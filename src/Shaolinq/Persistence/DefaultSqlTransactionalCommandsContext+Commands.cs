﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using Shaolinq.Logging;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence
{
	public partial class DefaultSqlTransactionalCommandsContext
	{
		#region ExecuteReader
		[RewriteAsync]
		public override IDataReader ExecuteReader(string sql, IReadOnlyList<TypedValue> parameters)
		{
			using (var command = this.CreateCommand())
			{
				foreach (var value in parameters)
				{
					this.AddParameter(command, value.Type, value.Value);
				}

				command.CommandText = sql;

				Logger.Debug(() => this.FormatCommand(command));

				try
				{
					return command.ExecuteReaderEx();
				}
				catch (Exception e)
				{
					var decoratedException = LogAndDecorateException(e, command);

					if (decoratedException != null)
					{
						throw decoratedException;
					}

					throw;
				}
			}
		}
		#endregion

		#region Update
		
		[RewriteAsync]
		public override void Update(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);

			foreach (var dataAccessObject in dataAccessObjects)
			{
				var objectState = dataAccessObject.GetAdvanced().ObjectState;

				if ((objectState & (ObjectState.Changed | ObjectState.ServerSidePropertiesHydrated)) == 0)
				{
					continue;
				}

				using (var command = this.BuildUpdateCommand(typeDescriptor, dataAccessObject))
				{

					if (command == null)
					{
						Logger.ErrorFormat("Object is reported as changed but GetChangedProperties returns an empty list ({0})", dataAccessObject);

						continue;
					}

					Logger.Debug(() => this.FormatCommand(command));

					int result;

					try
					{
						result = command.ExecuteNonQueryEx();
					}
					catch (Exception e)
					{
						var decoratedException = LogAndDecorateException(e, command);

						if (decoratedException != null)
						{
							throw decoratedException;
						}

						throw;
					}

					if (result == 0)
					{
						throw new MissingDataAccessObjectException(dataAccessObject, null, command.CommandText);
					}

					dataAccessObject.ToObjectInternal().ResetModified();
				}
			}
		}
		#endregion

		#region Insert

		[RewriteAsync]
		public override InsertResults Insert(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			var listToFixup = new List<DataAccessObject>();
			var listToRetry = new List<DataAccessObject>();

			foreach (var dataAccessObject in dataAccessObjects)
			{
				var objectState = dataAccessObject.GetAdvanced().ObjectState;

				switch (objectState & ObjectState.NewChanged)
				{
				case ObjectState.Unchanged:
					continue;
				case ObjectState.New:
				case ObjectState.NewChanged:
					break;
				case ObjectState.Changed:
					throw new NotSupportedException("Changed state not supported");
				}

				var primaryKeyIsComplete = (objectState & ObjectState.PrimaryKeyReferencesNewObjectWithServerSideProperties) == 0;
				var deferrableOrNotReferencingNewObject = (this.SqlDatabaseContext.SqlDialect.SupportsCapability(SqlCapability.Deferrability) || ((objectState & ObjectState.ReferencesNewObject) == 0));

				var objectReadyToBeCommited = primaryKeyIsComplete && deferrableOrNotReferencingNewObject;

				if (objectReadyToBeCommited)
				{
					var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);

					using (var command = this.BuildInsertCommand(typeDescriptor, dataAccessObject))
					{
						Logger.Debug(() => this.FormatCommand(command));

						try
						{
							var reader = command.ExecuteReaderEx();

							using (reader)
							{
								if (dataAccessObject.GetAdvanced().DefinesAnyDirectPropertiesGeneratedOnTheServerSide)
								{
									var dataAccessObjectInternal = dataAccessObject.ToObjectInternal();

									var result = reader.ReadEx();

									if (result)
									{
										this.ApplyPropertiesGeneratedOnServerSide(dataAccessObject, reader);
										dataAccessObjectInternal.MarkServerSidePropertiesAsApplied();
									}

									reader.Close();

									if (dataAccessObjectInternal.ComputeServerGeneratedIdDependentComputedTextProperties())
									{
										this.Update(dataAccessObject.GetType(), new[] { dataAccessObject });
									}
								}
							}
						}
						catch (Exception e)
						{
							var decoratedException = LogAndDecorateException(e, command);

							if (decoratedException != null)
							{
								throw decoratedException;
							}

							throw;
						}

						if ((objectState & ObjectState.ReferencesNewObjectWithServerSideProperties) == ObjectState.ReferencesNewObjectWithServerSideProperties)
						{
							listToFixup.Add(dataAccessObject);
						}
						else
						{
							dataAccessObject.ToObjectInternal().ResetModified();
						}
					}
				}
				else
				{
					listToRetry.Add(dataAccessObject);
				}
			}

			return new InsertResults(listToFixup, listToRetry);
		}
		#endregion

		#region DeleteExpression

		[RewriteAsync]
        public override void Delete(SqlDeleteExpression deleteExpression)
		{
			var formatResult = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(deleteExpression, SqlQueryFormatterOptions.Default);

			using (var command = this.CreateCommand())
			{
				command.CommandText = formatResult.CommandText;

				foreach (var value in formatResult.ParameterValues)
				{
					this.AddParameter(command, value.Type, value.Value);
				}

				Logger.Debug(() => this.FormatCommand(command));

				try
				{
					var count = command.ExecuteNonQueryEx();
				}
				catch (Exception e)
				{
					var decoratedException = LogAndDecorateException(e, command);

					if (decoratedException != null)
					{
						throw decoratedException;
					}

					throw;
				}
			}
		}

		#endregion

		#region DeleteObjects

		[RewriteAsync]
		public override void Delete(Type type, IEnumerable<DataAccessObject> dataAccessObjects)
		{
			var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);
			var parameter = Expression.Parameter(typeDescriptor.Type, "value");

			Expression body = null;

			foreach (var dataAccessObject in dataAccessObjects)
			{
				var currentExpression = Expression.Equal(parameter, Expression.Constant(dataAccessObject));

				if (body == null)
				{
					body = currentExpression;
				}
				else
				{
					body = Expression.OrElse(body, currentExpression);
				}
			}

			if (body == null)
			{
				return;
			}
            
			var condition = Expression.Lambda(body, parameter);
		    var expression = (Expression)Expression.Call(Expression.Constant(this.DataAccessModel), MethodInfoFastRef.DataAccessModelGetDataAccessObjectsMethod.MakeGenericMethod(typeDescriptor.Type));

		    expression = Expression.Call(MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(typeDescriptor.Type), expression, Expression.Quote(condition));
		    expression = Expression.Call(MethodInfoFastRef.QueryableExtensionsDeleteMethod.MakeGenericMethod(typeDescriptor.Type), expression);

			var provider = new SqlQueryProvider(this.DataAccessModel, this.SqlDatabaseContext);

		    ((ISqlQueryProvider)provider).ExecuteEx<int>(expression);
		}

		#endregion
	}
}
