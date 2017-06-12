﻿// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Reflection;

namespace Shaolinq.Postgres
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface)]
	public class RewriteAsyncAttribute
		: Attribute
	{
		public bool ContinueOnCapturedContext { get; set; }
		public MethodAttributes MethodAttributes { get; set; }

		public RewriteAsyncAttribute(MethodAttributes methodAttributes = default(MethodAttributes))
		{
			this.MethodAttributes = methodAttributes;
		}
	}
}
