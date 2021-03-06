// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Platform.Reflection;
using Platform.Validation;

namespace Shaolinq.Persistence
{
	public class DefaultStringSqlDataType
		: SqlDataType
	{
		public DefaultStringSqlDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration)
			: base(constraintDefaultsConfiguration, typeof(string))
		{
		}

		protected DefaultStringSqlDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type type)
			: base(constraintDefaultsConfiguration, type)
		{
		}
		
		protected virtual string CreateFixedTypeName(int maximumLength)
		{
			return $"CHAR({maximumLength})";
		}

		protected virtual string CreateVariableName(int maximumLength)
		{
			return $"VARCHAR({maximumLength})";
		}

		protected virtual string CreateTextName()
		{
			return "TEXT";
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor, ConstraintDefaultsConfiguration constraintDefaults)
		{
			var sizeConstraintAttribute = propertyDescriptor?.PropertyInfo.GetFirstCustomAttribute<SizeConstraintAttribute>(true);
			
			if (sizeConstraintAttribute != null || constraintDefaults?.StringSizeFlexibility != null)
			{
				switch ((constraintDefaults?.StringSizeFlexibility ?? sizeConstraintAttribute.SizeFlexibility))
				{
				case SizeFlexibility.Fixed:
					return this.CreateFixedTypeName(sizeConstraintAttribute.MaximumLength);
				case SizeFlexibility.Variable:
					return this.CreateVariableName(sizeConstraintAttribute.MaximumLength);
				case SizeFlexibility.LargeVariable:
					return this.CreateTextName();
				default:
					throw new NotSupportedException("SizeFlexibility: " + sizeConstraintAttribute.SizeFlexibility);
				}
			}
			else
			{
				if (propertyDescriptor != null && (propertyDescriptor.IsPrimaryKey || propertyDescriptor.HasUniqueAttribute || propertyDescriptor.IndexAttributes.Count > 0))
				{
					return this.CreateVariableName(this.constraintDefaultsConfiguration.IndexedStringMaximumLength);
				}
				else
				{
					return this.CreateVariableName((constraintDefaults ?? this.constraintDefaultsConfiguration).StringMaximumLength);
				}
			}
		}

		public override Expression GetReadExpression(Expression dataReader, int ordinal)
		{
			return Expression.Condition
			(
				Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
				Expression.Convert(Expression.Constant(null), this.SupportedType),
				Expression.Call(dataReader, DataRecordMethods.GetStringMethod, Expression.Constant(ordinal))
			);
		}
	}
}
