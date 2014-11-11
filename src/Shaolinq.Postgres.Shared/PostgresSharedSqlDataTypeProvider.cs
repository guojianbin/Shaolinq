// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
﻿using Shaolinq.Persistence;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedSqlDataTypeProvider
		: DefaultSqlDataTypeProvider
	{
		private readonly SqlDataType blobSqlDataType;

		public bool NativeUuids { get; private set; }
		public bool NativeEnums { get; private set; }

		protected override SqlDataType GetBlobDataType()
		{
			return blobSqlDataType;
		}

		protected override SqlDataType GetEnumDataType(Type type)
		{
			if (!this.NativeEnums)
			{
				return base.GetEnumDataType(type);
			}

			return new PostgresSharedEnumSqlDataType(this.ConstraintDefaults, type);
		}

		public PostgresSharedSqlDataTypeProvider(ConstraintDefaults constraintDefaults, bool nativeUuids, bool nativeEnums)
			: base(constraintDefaults)
		{
			this.NativeUuids = nativeUuids;
			this.NativeEnums = nativeEnums;
			
			this.blobSqlDataType = new DefaultBlobSqlDataType(constraintDefaults, "BYTEA");

			DefineSqlDataType(typeof(bool), "BOOLEAN", "GetBoolean");
			DefineSqlDataType(typeof(short), "SMALLINT", "GetInt16");
			DefineSqlDataType(typeof(int), "INTEGER", "GetInt32");

			DefineSqlDataType(typeof(ushort), "SMALLINT", "GetUInt16");
			DefineSqlDataType(typeof(uint), "INTEGER", "GetUInt32");
			DefineSqlDataType(typeof(ulong), "BIGINT", "GetUInt64");

			DefineSqlDataType(typeof(double), "DOUBLE PRECISION", "GetDouble");
			DefineSqlDataType(typeof(byte), "SMALLINT", "GetByte");
			DefineSqlDataType(typeof(sbyte), "SMALLINT", "GetByte");
			DefineSqlDataType(typeof(decimal), "NUMERIC(60, 30)", "GetDecimal");

			DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaults, "TIMESTAMP", false));
			DefineSqlDataType(new UniversalTimeNormalisingDateTimeSqlDateType(this.ConstraintDefaults, "TIMESTAMP", true));

			DefineSqlDataType(new PostgresSharedTimespanSqlDataType(this.ConstraintDefaults, typeof(TimeSpan)));
			DefineSqlDataType(new PostgresSharedTimespanSqlDataType(this.ConstraintDefaults, typeof(TimeSpan?)));

			if (nativeUuids)
			{
				DefineSqlDataType(new PostgresSharedUuidSqlDataType(this.ConstraintDefaults, typeof(Guid)));
				DefineSqlDataType(new PostgresSharedUuidSqlDataType(this.ConstraintDefaults, typeof(Guid?)));
			}
		}
	}
}
