// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlProjectionExpression
		: SqlBaseExpression
	{
		public bool IsDefaultIfEmpty { get; }
		public bool IsElementTableProjection { get; }
		public SqlSelectExpression Select { get; }
		public Expression Projector { get; }
		public LambdaExpression Aggregator { get; }
		public SelectFirstType SelectFirstType { get; }
		public Expression DefaultValueExpression { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Projection;

		public SqlProjectionExpression(SqlSelectExpression select, Expression projector, LambdaExpression aggregator)
			: this(select, projector, aggregator, false)
		{
		}

		public SqlProjectionExpression(SqlSelectExpression select, Expression projector, LambdaExpression aggregator, bool isElementTableProjection)
			: this(select, projector, aggregator, isElementTableProjection, SelectFirstType.None, null, false)
		{
		}

		public SqlProjectionExpression(SqlSelectExpression select, Expression projector, LambdaExpression aggregator, bool isElementTableProjection, SelectFirstType selectFirstType, Expression defaultValueExpression, bool isDefaultIfEmpty)
			: base(selectFirstType == SelectFirstType.None ? select.Type : select.Type.GetGenericArguments()[0])
		{
			this.Select = select;
			this.Projector = projector;
			this.Aggregator = aggregator;
			this.SelectFirstType = selectFirstType;
			this.DefaultValueExpression = defaultValueExpression;
			this.IsElementTableProjection = isElementTableProjection;
			this.IsDefaultIfEmpty = isDefaultIfEmpty;
		}

		public SqlProjectionExpression ToDefaultIfEmpty(Expression defaultValueExpression)
		{
			return new SqlProjectionExpression(this.Select, this.Projector, this.Aggregator, this.IsElementTableProjection, this.SelectFirstType, defaultValueExpression, true);
		}
	}
}
