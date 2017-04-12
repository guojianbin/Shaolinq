﻿using System;

namespace Shaolinq.Persistence.Linq.Expressions
{
	[Flags]
	public enum ConstraintType
	{
		ForeignKey = 1,
		PrimaryKey = 2,
		Unique = 4,
		AutoIncrement = 8,
		NotNull = 16,
		DefaultValue = 32,
		PrimaryKeyAutoIncrement = PrimaryKey | AutoIncrement,
		ForeignKeyNotNull = ForeignKey | NotNull,
		ForeignKeyUnique = ForeignKey | Unique,
		ForeignKeyUniqueNotNull = ForeignKey | Unique | NotNull
	}
}