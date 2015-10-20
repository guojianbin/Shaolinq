﻿using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ExpressionVisitor = Platform.Linq.ExpressionVisitor;

namespace Shaolinq.Parser
{
	public class ReferencedPropertiesGatherer
		: ExpressionVisitor
	{
		private readonly Expression target;
		private readonly List<PropertyInfo> results = new List<PropertyInfo>();

		public ReferencedPropertiesGatherer(Expression target)
		{
			this.target = target;
		}

		public static List<PropertyInfo> Gather(Expression expression, Expression target)
		{
			var gatherer = new ReferencedPropertiesGatherer(target);

			gatherer.Visit(expression);

			return gatherer.results;
		}

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Object == this.target)
            {
                var attributes = methodCallExpression.Method.GetCustomAttributes(typeof(DependsOnPropertyAttribute), true);

                foreach (DependsOnPropertyAttribute attribute in attributes)
                {
                    var property = this.target.Type.GetProperty(attribute.PropertyName);

                    this.results.Add(property);
                }
            }

            return base.VisitMethodCall(methodCallExpression);
        }

        protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			if (memberExpression.Expression == this.target)
			{
			    var info = memberExpression.Member as PropertyInfo;

                if (info != null)
				{
					results.Add(info);
				}
			}

		    return base.VisitMemberAccess(memberExpression);
		}
	}
}