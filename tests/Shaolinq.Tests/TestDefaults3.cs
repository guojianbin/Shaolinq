﻿using System.Linq;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	[TestFixture("Sqlite")]
	[TestFixture("Sqlite:DataAccessScope")]
	[TestFixture("SqlServer")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	[Ignore("Need to review")]
	public class TestDefaults3
		: BaseTests<TestDataAccessModel>
	{
		public TestDefaults3(string providerName)
			: base(providerName, alwaysSubmitDefaultValues: false, valueTypesAutoImplicitDefault: true, includeImplicitDefaultsInSchema: true)
		{
		}

		[Test]
		public void Test_ValueRequiredField_AlwaysNeeded()
		{
			long id;

			using (var scope = NewTransactionScope())
			{
				var obj = this.model.DefaultsTestObjects.Create();

				obj.IntValueWithValueRequired = 1;
				obj.NullableIntValueWithValueRequired = null;

				scope.Flush();

				id = obj.Id;

				scope.Complete();
			}

			using (var scope = NewTransactionScope())
			{
				this.model.DefaultsTestObjects.Single(c => c.Id == id);

				scope.Complete();
			}
		}

		[Test]
		public void Test_ImplicitDefault_Overridden_By_ValueRequired()
		{
			Assert.Throws<MissingPropertyValueException>
			(() =>
			{
				using (var scope = NewTransactionScope())
				{
					var obj = this.model.DefaultsTestObjects.Create();
					
					obj.IntValueWithValueRequired = 1;
					obj.NullableIntValueWithValueRequired = 2;

					scope.Flush();

					scope.Complete();
				}
			});
		}
	}
}
