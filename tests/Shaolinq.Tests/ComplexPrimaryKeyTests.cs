﻿// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Platform;
using Shaolinq.Persistence.Linq;
using Shaolinq.Tests.ComplexPrimaryKeyModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Sqlite")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	[TestFixture("Sqlite:DataAccessScope")]
	[TestFixture("SqlServer:DataAccessScope")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	public class ComplexPrimaryKeyTests
		: BaseTests<ComplexPrimaryKeyDataAccessModel>
	{
		private long shopId;
		
		public class SelectorTesterClass<T>
			where T : DataAccessObject<Guid>, IAddressed
		{
			private readonly Func<IQueryable<T>> queryable;

			public SelectorTesterClass(Func<IQueryable<T>> queryable)
			{
				this.queryable = queryable;
			}

			public IQueryable<T> Query1()
			{
				// Roslyn turns this into Expression.Member(c, methodof(IAddressed.Address))
				// Instead of Expression.Member(Expression.Convert(c, typeof(IAddressed)), methodof(T.Address))

				return this.queryable().Include(c => c.Address);
			}

			public IQueryable<T> Query2()
			{
				// Roslyn turns this into Expression.Member(Expression.Convert(c, typeof(IAddressed)), methodof(IAddressed.Address))
				// Instead of Expression.Member(c, methodof(T.Address))

				return (this.queryable()).Where(c => c.Address.Number == 0);
			}

			public IQueryable<T> Query3()
			{
				// Roslyn turns this into Expression.Member(Expression.Convert(c, typeof(IAddressed)), methodof(IAddressed.Address))
				// Instead of Expression.Member(c, methodof(T.Address))

				return (this.queryable()).Select(c => c.IncludeDirect(d => d.Address));
			}
		}

		public class SelectorTesterClass2<T>
			where T : IAddressed
		{
			private readonly Func<IQueryable<T>> queryable;

			public SelectorTesterClass2(Func<IQueryable<T>> queryable)
			{
				this.queryable = queryable;
			}

			public IQueryable<T> Query()
			{
				// Roslyn turns this into Expression.Member(c, methodof(IAddressed.Address))
				// Instead of Expression.Member(c, methodof(T.Address))

				return (this.queryable()).Where(c => c.Address.Number == 0);
			}
		}

		[Test]
		public void Test_Expression_Tree_Selector_With_And_Interface_Parameter_And_Generics1()
		{
			var tester = new SelectorTesterClass<Mall>(() => this.model.Malls);

			tester.Query1();

			var s = tester.Query1().ToString();

			Assert.IsTrue(s.Contains("JOIN"));
			Assert.IsFalse(s.Contains("ObjectReference"));
		}

		[Test]
		public void Test_Expression_Tree_Selector_With_And_Interface_Parameter_And_Generics2a()
		{
			var tester = new SelectorTesterClass<Mall>(() => this.model.Malls);

			var s = tester.Query2().ToString();

			Console.WriteLine(s);

			Assert.IsTrue(s.Contains("JOIN"));
			Assert.IsFalse(s.Contains("ObjectReference"));
		}

		[Test]
		public void Test_Expression_Tree_Selector_With_And_Interface_Parameter_And_Generics2b()
		{
			var tester = new SelectorTesterClass2<Mall>(() => this.model.Malls);

			var s = tester.Query().ToString();

			Console.WriteLine(s);

			Assert.IsTrue(s.Contains("JOIN"));
			Assert.IsFalse(s.Contains("ObjectReference"));
		}

		[Test]
		public void Test_Expression_Tree_Selector_With_And_Interface_Parameter_And_Generics3()
		{

			var tester = new SelectorTesterClass<Mall>(() => this.model.Malls);

			tester.Query1();

			var s = tester.Query3().ToString();

			Assert.IsTrue(s.Contains("JOIN"));
			Assert.IsFalse(s.Contains("ObjectReference"));
		}
		
		public ComplexPrimaryKeyTests(string providerName)
			: base(providerName)
		{
		}

		[OneTimeSetUp]
		public void SetUpFixture()
		{
			using (var scope = this.NewTransactionScope())
			{
				var region = this.model.Regions.Create();
				region.Name = "Washington";
				region.Diameter = 2000;

				this.model.Flush();

				var mall = this.model.Malls.Create();

				this.model.Flush();

				var shop = mall.Shops.Create();

				mall.Name = "Seattle City";

				var address = this.model.Addresses.Create();
				shop.Address = address;
				shop.Address.Street = "Madison Street";
				
				shop.Address.Region = region;
				shop.Name = "Microsoft Store";

				var center = this.model.Coordinates.Create();
				center.Label = "Center of Washington";

				shop.Address.Region.Center = center;

				this.model.Flush();

				this.shopId = shop.Id;
				
				region = this.model.Regions.Create();
				shop.SecondAddress = this.model.Addresses.Create();
				shop.SecondAddress.Street = "Jefferson Avenue";
				shop.SecondAddress.Region = region;
				shop.SecondAddress.Region.Name = "Washington";
				shop.SecondAddress.Region.Diameter = 100;

				var sisterMall = this.model.Malls.Create();
				sisterMall.Name = "Mall of Oregan";

				sisterMall.Address = this.model.Addresses.Create();
				sisterMall.Address.Region = this.model.Regions.Create();
				sisterMall.Address.Number = 1600;
				sisterMall.Address.Street = "Oregan Street";
				sisterMall.Address.Region.Name = "Wickfield";
				sisterMall.Address.Region.Center = this.model.Coordinates.Create();
				sisterMall.Address.Region.Center.Longitude = 600.5;
				sisterMall.Address.Region.Center.Magnitude = 10;

				mall.SisterMall = sisterMall;
				mall.SisterMall2 = sisterMall;
				

				shop = sisterMall.Shops.Create();
				shop.Name = "Sister Mall Store 1";
				address = this.model.Addresses.Create();
				shop.Address = address;
				shop.Address.Region = region;

				shop = sisterMall.Shops.Create();
				shop.Name = "Sister Mall Store 2";
				address = this.model.Addresses.Create();
				shop.Address = address;
				shop.Address.Region = region;

				shop = sisterMall.Shops3.Create();
				shop.Name = "Sister Mall Store B";
				address = this.model.Addresses.Create();
				shop.Address = address;
				shop.Address.Region = region;

				scope.Flush();
				
				scope.Complete();
			}
		}
		
		private IQueryable<Address> GetAllAddresses(bool v)
		{
			return this.model.Addresses.Where(c => c.Number != 0);
		}

		private IQueryable<Region> GetAllRegions(bool v)
		{
			return this.model.Regions.Where(c => c.Diameter > 0);
		}

		[Test]
		public void Test_Join_With_Implicit_Join_On_Join_Condition1()
		{
			var malls = this.model.Malls.Where(c => c.Name != null);

			var query = from mall in malls
				join r in this.GetAllRegions(false) on mall.Address.Region2 equals r
				select new { mall, r };

			var result = query.ToList();
		}
		
		[Test]
		public void Test_Join_With_Implicit_Join_On_Join_Condition2()
		{
			var malls = this.model.Malls.Where(c => c.Name != null);

			var query = from mall in malls
						join r in this.GetAllRegions(false) on mall.Address.Region.Id equals r.Id
						select new { mall, r };

			var result = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implicit_Join_On_Join_Condition3()
		{
			var malls = this.model.Malls.Where(c => c.Name != null);

			var query = from mall in malls
						join r in this.GetAllRegions(false) on mall.Address.Region equals r
						select new { mall, r };

			var result = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implicit_Join_On_Join_Condition4()
		{
			var malls = this.model.Malls.Where(c => c.Name != null);

			var query = from mall in malls
						join r in this.GetAllRegions(false) on mall.Address.Region2 equals r
						select new { mall, r };

			var result = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implicit_Join_On_Join_Condition5()
		{
			var malls = this.model.Malls.Where(c => c.Name != null);

			var query = from mall in malls
						join r in this.GetAllRegions(false) on mall.Address.Region2.Id equals r.Id
						select new { mall, r };

			var result = query.ToList();
		}

		[Test]
		public void Test_SelectMany1()
		{
			var malls = this.model.Malls.Where(c => c.Name != null);
			
			var query = from mall in malls
				from address in this.model.Addresses.Where(c => c.Number != 0)
				where mall.Address.Region == address.Region
				select new { mall, address };

			query.ToList();
		}

		[Test]
		public void Test_SelectMany2()
		{
			var malls = this.model.Malls;

			var query = from mall in malls
						from address in this.GetAllAddresses(false)
						where mall.Address.Region == address.Region
						select new { mall, address };

			query.ToList();
		}

		[Test]
		public void Test_SelectMany3()
		{
			var malls = this.model.Malls;

			var query = malls
				.Select(c => new object[] {  c.Address })
				.AsEnumerable()
				.SelectMany(c => c)
				.ToList();
		}

		[Test]
		public void Test_Implicit_Join_Compare_ObjectReferences()
		{
			var malls = this.model.Malls;
			var coordinate = this.model.Coordinates.GetReference(0);

			var query = from mall in malls
						from region in this.model.Regions
						where mall.Address.Region2.Center == coordinate
						select new { mall, region };

			query.ToList();
		}

		[Test]
		public void Test_SuperMall_With_Mall_PrimaryKey()
		{
			Guid mallId;
			long region0, region1, region2;
			long address0, address1, address2;
			
			using (var scope = this.NewTransactionScope())
			{
				var mall = this.model.Malls.Create();
				var superMall = this.model.SuperMalls.Create();

				mall.Address = this.model.Addresses.Create();
				mall.Address.Region = this.model.Regions.Create();
				mall.Address.Region.Name = "!RegionName0";

				superMall.Id = mall;

				superMall.Address1 = this.model.Addresses.Create();
				superMall.Address1.Region = this.model.Regions.Create();
				superMall.Address1.Region.Name = "!RegionName1";

				superMall.Address2 = this.model.Addresses.Create();
				superMall.Address2.Region = this.model.Regions.Create();
				superMall.Address2.Region.Name = "!RegionName2";

				scope.Flush();

				mallId = mall.Id;
				address0 = mall.Address.Id;
				address1 = superMall.Address1.Id;
				address2 = superMall.Address2.Id;

				region0 = mall.Address.Region.Id;
				region1 = superMall.Address1.Region.Id;
				region2 = superMall.Address2.Region.Id;

				scope.Complete();
			}

			using (var scope = this.NewTransactionScope())
			{
				var address = this.model.Addresses.GetReference(new { Id = address0, Region = new { Id = region0, Name = "!RegionName0" } });

				address.Inflate();

				var mall = this.model.Malls.GetReference(new { Id = mallId, Address = new { Id = address0, Region = new { Id = region0, Name = "!RegionName0" } } });

				mall.Inflate();

				var key =
					new
					{
						Id = new { Id = mallId, Address = new { Id = address0, Region = new { Id = region0, Name = "!RegionName0" } } },
						Address1 = new { Id = address1, Region = new { Id = region1, Name = "!RegionName1" } },
						Address2 = new { Id = address2, Region = new { Id = region2, Name = "!RegionName2" } }
					};

				var reference = this.model.SuperMalls.GetReference(key);
				reference.Inflate();

				//var superMall = this.model.SuperMalls.Single(c => c == reference);
			}	
		}

		[Test]
		public void Test_Set_NullableDate()
		{
			using (var scope = this.NewTransactionScope())
			{
				var shop = this.model.Shops.FirstOrDefault(c => c.Name == "Microsoft Store");

				shop.CloseDate = DateTime.Now;

				scope.Complete();
			}

			using (var scope = this.NewTransactionScope())
			{
				var shop = this.model.Shops.FirstOrDefault(c => c.Name == "Microsoft Store");

				Assert.IsNotNull(shop.CloseDate);

				shop.CloseDate = null;

				scope.Complete();
			}

			using (var scope = this.NewTransactionScope())
			{
				var shop = this.model.Shops.FirstOrDefault(c => c.Name == "Microsoft Store");

				Assert.IsNull(shop.CloseDate);
			}
		}

		[Test]
		public void Test_Complex_Explicit_Joins()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses on shop.Address equals address1
				join
				address2 in this.model.Addresses on shop.SecondAddress equals address2
				select
				new
				{
					shop,
					region1 = address1.Region,
					region2 = address2.Region,
				};

			var results = query.ToList();

			var query2 =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses on shop.Address equals address1
				join
				address2 in this.model.Addresses on shop.SecondAddress equals address2
				select
				new
				{
					shop,
					region1 = address1.Region,
					region2 = address2.Region,
				};


			var result2 = query2.ToList();
		}

		[Test]
		public void Test_Explicit_Left_Join_Empty_Right()
		{
			var query =
				from shop in this.model.Shops
				join address_ in this.model.Addresses on shop.ThirdAddress equals address_ into g
				from address in g.DefaultIfEmpty()
				select
				new
				{
					address
				};

			var first = query.First();

			Assert.IsNull(first.address);
		}

		[Test]
		public void Test_Explicit_Left_Join_Empty_Right2()
		{
			var query =
				from shop in this.model.Shops
				join address in this.model.Addresses on shop.ThirdAddress equals address
				select
				new
				{
					address
				};

			var first = query.FirstOrDefault();

			Assert.IsNull(first);
		}

		[Test]
		public void Test_Complex_Explicit_Joins_With_Explicit_Include_In_Select()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses on shop.Address equals address1
				join
				address2 in this.model.Addresses on shop.SecondAddress equals address2
				join
				//address3 in this.model.Addresses on shop.ThirdAddress equals address3 
				_address3 in this.model.Addresses on shop.ThirdAddress equals _address3 into g
				from address3 in g.DefaultIfEmpty()
				select
				new
				{
					shop,
					region1 = address1.Region,
					region2 = address2.Region,
					region3a = address3.Region,
					region3b = address3.Region2,
				};

			var first = query.First();

			Assert.IsFalse(first.region1.IsDeflatedReference());
			Assert.IsFalse(first.region2.IsDeflatedReference());
			Assert.AreEqual(2000, first.region1.Diameter);
			Assert.AreEqual(100, first.region2.Diameter);
			Assert.IsNull(first.region3a);
		}

		[Test]
		public void Test_Complex_Explicit_Joins_With_Back_Reference_On_Condition()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses on shop.Address equals address1
				join
				address2 in this.model.Addresses on address1 equals address2
				select
				new
				{
					address1,
					address2
				};

			using (var scope = this.NewTransactionScope())
			{
				var first = query.First();

				Assert.AreSame(first.address1, first.address2);
			}
		}

		[Test]
		public void Test_Complex_Explicit_Joins_Same_Objects()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses on shop.Address equals address1
				join
				address2 in this.model.Addresses on shop.Address equals address2
				select
				new
				{
					shop,
					address1,
					address2
				};

			var first = query.First();

			// TODO: Assert.AreSame(first.address1, first.address2);
			Assert.AreEqual(first.address1, first.address2);
		}

		[Test]
		public void Test_Complex_Explicit_Joins_With_Include_In_Queryables()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses on shop.Address equals address1
				join
				address2 in this.model.Addresses.Include(c => c.Region) on shop.SecondAddress equals address2
				join
				_address3 in this.model.Addresses on shop.ThirdAddress equals _address3  into g
				from address3 in g.DefaultIfEmpty()
				join
				address4 in this.model.Addresses.Include(c => c.Region) on shop.SecondAddress equals address4
				join
				address5 in this.model.Addresses.Include(c => c.Region) on address4 equals address5
				select
				new
				{
					shop,
					address1 = address1.IncludeDirect(c => c.Region.Center),
					address2,
					address3 = address3.IncludeDirect(c => c.Region),
					address4,
					address5
				};

			using (var scope = this.NewTransactionScope())
			{
				var first = query.First();

				Assert.AreSame(first.address4, first.address5);

				Assert.IsFalse(first.address1.Region.IsDeflatedReference());
				Assert.IsFalse(first.address1.Region.Center.IsDeflatedReference());
				Assert.IsFalse(first.address2.Region.IsDeflatedReference());
				Assert.IsFalse(first.address4.Region.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.address1.Street);
				Assert.AreEqual("Jefferson Avenue", first.address2.Street);
				Assert.AreEqual("Jefferson Avenue", first.address4.Street);
				Assert.AreEqual(2000, first.address1.Region.Diameter);
				Assert.AreEqual(100, first.address2.Region.Diameter);
				Assert.AreEqual(100, first.address4.Region.Diameter);
				Assert.IsNull(first.address3);
			}
		}

		[Test]
		public void Test_Complex_Explicit_Joins_Without_Include_In_Queryables()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses on shop.Address equals address1
				join
				address2 in this.model.Addresses on shop.SecondAddress equals address2
				join
				address3_ in this.model.Addresses on shop.ThirdAddress equals address3_ into g
				from address3 in g.DefaultIfEmpty()
				select
				new
				{
					shop,
					address1,
					address2,
					address3
				};

			var first = query.First();

			Assert.IsTrue(first.address1.Region.IsDeflatedReference());
			Assert.IsTrue(first.address2.Region.IsDeflatedReference());
			Assert.AreEqual("Madison Street", first.address1.Street);
			Assert.AreEqual("Jefferson Avenue", first.address2.Street);
			Assert.AreEqual(2000, first.address1.Region.Diameter);
			Assert.AreEqual(100, first.address2.Region.Diameter);
			Assert.IsFalse(first.address1.Region.IsDeflatedReference());
			Assert.IsFalse(first.address2.Region.IsDeflatedReference());
			Assert.IsNull(first.address3);
		}

		[Test]
		public void Test_Join_With_Implict_Join_With_Related_Object_In_Projection()
		{
			var query =
				from
					shop in this.model.Shops
				join
					address1 in this.model.Addresses on shop.Address equals address1
				select new
				{
					address = shop.Address
				};

			var results = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implict_Join_With_Related_Object_In_Projection2()
		{
			var query =
				from
					shop in this.model.Shops
				join
					address1 in this.model.Addresses on shop.Address equals address1
				select new
				{
					shop.Address,
					shop.SecondAddress,
					address1.Region
				};

			var results = query.ToList();
		}

		[Test]
		public void Test_Eplicit_Join_With_Implicit_Reference_To_Related_PrimaryKey_In_Projection()
		{
			var query =
				(from
					shop in this.model.Shops
				 join
					 address1 in this.model.Addresses on shop.Address equals address1
				 select new
				 {
					 address1.Region.Name,
					 shop.Address.Id
				 });

			var results = query.ToList();
		}

		[Test]
		public void Test_Explicit_Join_With_Implicit_Reference_To_Related_NonPrimaryKey_In_Projection1()
		{
			var query =
				(from
					shop in this.model.Shops
					join
						address1 in this.model.Addresses on shop.Address equals address1
					select new
					{
						address1.Region.Diameter
					});

			var results = query.ToList();
		}

		[Test]
		public void Test_Eplicit_Join_Project_Into_AnonymousType()
		{
			var query =
				(from
					shop in this.model.Shops
					join
						address1 in this.model.Addresses on shop.Address equals address1
					select new
					{
						A = shop,
						B = address1
					}).Select(c => new
					{
						c.B.Region.Diameter
					});

			var results = query.ToList();
		}

		[Test]
		public void Test_Eplicit_Join_Project_Into_Pair()
		{
			var query =
				(from
					shop in this.model.Shops
				 join
					 address1 in this.model.Addresses on shop.Address equals address1
				 select new Tuple<Shop, Address>(shop, address1)).Select(c => new
				 {
					 c.Item2.Region.Diameter
				 });

			var results = query.ToList();
		}

		[Test]
		public void Test_Eplicit_Join_With_Implicit_Reference_To_Related_NonPrimaryKey_In_Projection2()
		{
			var query =
				(from
					shop in this.model.Shops
				 join
					 address1 in this.model.Addresses on shop.Address equals address1
				 select new
				 {
					 address1.Region.Diameter,
					 shop.Address.Street
				 });

			var results = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implict_Join_With_Related_Object_In_Projection3a()
		{
			var query =
				(from
					shop in this.model.Shops
					join
						address1 in this.model.Addresses on shop.Address equals address1
					select new
					{
						address1.Region.Range,
						shop.Address.Street
					});

			var results = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implict_Join_With_Related_Object_In_Projection3b()
		{
			var query =
				(from
					shop in this.model.Shops
				 join
					 address1 in this.model.Addresses on shop.Address equals address1
				 select new
				 {
					 shop,
					 address1
				 }).Select(c => new
				 {
					 c.shop.Address.Region.Center,
					 c.address1.Region,
					 c.address1.Region2
				 });

			var results = query.ToList();
		}

		[Test]
		public void Test_Select_Then_Select_With_Multiple_Implicit_Joins()
		{
			var query = (from address in this.model.Addresses
						 select new
						 {
							 address1 = address
						 }).Select
				(c => new
				{
					c.address1.Region,
					c.address1.Region2
				});

			var results = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implict_Join_With_Related_Object_In_Projection4()
		{
			var query =
				(from
					shop in this.model.Shops
				 join
					 address1 in this.model.Addresses on shop.Address equals address1
				 select new
				 {
					 shop,
					 address1
				 }).Select(c => new
				 {
					 c.shop.Address,
					 c.address1 
				 }).Select(c => new { c.Address, c.address1.Region });

			var results = query.ToList();
		}

		[Test]
		public void Test_Join_With_Implict_Join_With_Related_Object_Two_Deep_In_Projection()
		{
			var query =
				from
					shop in this.model.Shops
				join
					address1 in this.model.Addresses on shop.Address equals address1
				select new
				{
					region = shop.Address.Region
				};

			var results = query.ToList();
		}

		[Test]
		public void Test_Complex_Explicit_And_Implicit_Joins_With_OrderBy()
		{
			var query =
				from
					shop in this.model.Shops
				join
					address1 in this.model.Addresses on shop.Address equals address1
				join
					address2 in this.model.Addresses on shop.SecondAddress equals address2
				orderby shop.Name
				select
					new
					{
						shop,
						region1 = address1.Region,
						address1 = shop.Address
					};

			var results = query.ToList();
		}


		[Test]
		public void Test_Complex_Explicit_With_Includes()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses.Include(c => c.Region) on shop.Address equals address1
				join
				address2 in this.model.Addresses.Include(c => c.Region) on shop.SecondAddress equals address2
				select
				new
				{
					shop,
					region1 = address1.Region,
					region2 = address2.Region2,
				};

			var results = query.ToList();
		}

		[Test, Ignore("Crazy query eh?")]
		public void Test_Explicit_Join_With_Implicit_Join_In_Equality_Properties()
		{
			var query =
				from
				shop in this.model.Shops
				join
					address1 in this.model.Addresses on shop.Address.Region.Diameter equals address1.Region.Diameter
				select
					new
					{
						shop
					};

			var results = query.ToList();
		}

		[Test]
		public void Test_Explicit_Useless_Join_And_Project_Requiring_Implicit_Join()
		{
			var query =
				from
				shop in this.model.Shops
				join
					address1 in this.model.Addresses on shop.Address equals address1
				select
					new
					{
						street = shop.Address.Street
					};

			var results = query.ToList();
		}

		[Test]
		public void Test_Explicit_Useless_Join_And_Project_Requiring_Implicit_Join_Manual_Reselect()
		{
			var query =
				(from
					shop in this.model.Shops
					join
						address1 in this.model.Addresses on shop.Address equals address1
					select
						new
						{
							shop,
							address1
						}
					).Select(c =>
								 new
								 {
									 street = c.shop.Address.Street,
									 diameter = c.shop.Address.Region.Diameter
								 });

			var results = query.ToList();
		}

		[Test]
		public void Test_Complex_Explicit_With_Implicit_Join_In_Projection()
		{
			var query =
				(from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses on shop.Address equals address1
				join
				address2 in this.model.Addresses on shop.SecondAddress equals address2
				select
				new
				{
					shop, address1, address2
				})
				.Select(c => 
				new
				{
					c.shop,
					diameter1 = c.address1.Region.Diameter,
					range1 = c.shop.Address.Region.Range
				});

			var results = query.ToList();
		}

		[Test]
		public void Test_Complex_Explicit_And_Implicit_Joins_With_Include_And_OrderBy()
		{
			var query =
				from
				shop in this.model.Shops
				join
				address1 in this.model.Addresses.Include(c => c.Region) on shop.Address equals address1
				join
				address2 in this.model.Addresses.Include(c => c.Region) on shop.SecondAddress equals address2
				orderby shop.Name
				select
				new
				{
					shop,
					region1 = address1.Region,
					region2 = address2.Region2,
				};

			var results = query.ToList();
		}

		[Test]
		public void Test_Explicit_Complex1()
		{
			using (var scope = this.NewTransactionScope())
			{
				var objs = from toy in this.model.Toys.Where(c => c.Missing != null).OrderBy(c => c.Name)
						   join child in this.model.Children.Where(c => c.Nickname != null) on toy.Owner equals child
					where !child.Good
					select new
					{
						child,
						toy
					};

				var list = objs.ToList();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Explicit_Complex2()
		{
			using (var scope = this.NewTransactionScope())
			{
				var objs = from toy in this.model.GetDataAccessObjects<Toy>().Where(c => c.Missing != null).OrderBy(c => c.Name)
						   join child in this.model.GetDataAccessObjects<Child>().Where(c => c.Nickname != null) on toy.Owner equals child
						   where child.Good
						   select new
						   {
							   child,
							   toy
						   };

				var list = objs.ToList();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Explicit_Join_Select_Then_GroupBy()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					(from
						shop in this.model.Shops
					 join address in this.model.Addresses on shop.Address equals address
					 select new
					 {
						 shop,
						 address
					 }).GroupBy(c => c.address.Street, c => c.shop)
						.Select(c => new { c.Key, count = c.Count() });


				var all = query.ToList();
			}
		}

		[Test]
		public void Test_Explicit_Join_On_GroupBy()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					(from
						shop in this.model.Shops
						join address in this.model.Addresses on shop.Address equals address
						select new
						{
							shop,
							address
						}
						).GroupBy(c => c.address.Street, c => c.shop)
						.Select(c => new { c.Key, count = c.Count()});


				var all = query.ToList();
			}
		}

		[Test]
		public void Test_Implicit_Join_On_GroupBy1()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					from
						shop in this.model.Shops
					group shop by shop.Address.Street
					into g
					select
						g.Key;

				var first = query.First();
			}
		}

		[Test]
		public void Test_Implicit_Join_On_GroupBy2()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops.GroupBy(c => c.Address.Street, c => new { Number = 1, Key = c.Address });

				var first = query.First();
			}
		}

		[Test]
		public void Test_Implicit_Join_On_OrderBy()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					(from
						shop in this.model.Shops
					orderby shop.Address.Street
					select
						shop).GroupBy(c => c.Address.Street);

				var first = query.First();
			}
		}

		[Test]
		public void Test_Implicit_Join_On_OrderBy_Project_Related_Property()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					from
						shop in this.model.Shops
					orderby shop.Address.Street
					select
						shop.Address;

				var first = query.First();
			}
		}


		[Test]
		public void Test_Implicit_Join_On_OrderBy_Project_Simple()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					from
						shop in this.model.Shops
					orderby shop.Address.Street
					select
						shop.Name;

				var first = query.First();
			}
		}

		[Test]
		public void Test_Implicit_Where_Join_Not_Primary_Key1()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					from
						shop in this.model.Shops
					where
						shop.Address.Street == "Madison Street"
					select
						shop;

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_And_Has_Property_With_Null_Value()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address.Street == "Madison Street");

				var first = query.First();

				Assert.IsNull(first.ThirdAddress);
			}
		}

		[Test]
		public void Test_Include_Property_With_Null_Value()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address.Street == "Madison Street")
					.Include(c => c.ThirdAddress.Region.Center);

				var first = query.First();

				Assert.IsNull(first.ThirdAddress);
			}
		}

		[Test]
		public void Test_Implicit_Join_In_Where_Then_Select_Single_Property()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address.Street == "Madison Street")
					.Select(c => c.Id);

				var first = query.First();
			}
		}

		[Test]
		public void Test_Implicit_Join_In_Where_Then_Project()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address.Street == "Madison Street")
					.Select(c => c);

				var first = query.First();
			}
		}

		[Test]
		public void Test_Implicit_Join_In_Where_With_Back_Reference_Then_Project1()
		{
			var results = this.model.Shops.Where(c => c.Mall.Address.Id == 0).ToList();
		}

		[Test]
		public void Test_Implicit_Join_In_Where_With_Back_Reference_Then_Project2()
		{
			var results = this.model.Shops.Where(c => c.Mall.Id == Guid.Empty).ToList();
		}

		[Test]
		public void Test_Implicit_Join_In_Where_With_Back_Reference_Then_Project3()
		{
			var results = this.model.Shops.Include(c => c.Mall).Where(c => c.Mall.Address.Id == 0).ToList();
		}

		[Test]
		public void Test_Implicit_Join_In_Where_With_Back_Reference_Then_Project4()
		{
			var results = this.model.Shops.Select(c => c.Mall.Address.Id == 0).ToList();
		}

		[Test]
		public void Test_Implicit_Join_In_Where_With_Back_Reference_Then_Project4b()
		{
			var results = this.model.Shops.Include(c => c.Mall).Select(c => c.Mall.Address.Id == 0).ToList();
		}

		[Test]
		public void Test_Implicit_Join_In_Where_With_Back_Reference_Then_Project5()
		{
			var results = this.model.Shops.Select(c => c.Mall.Address.Street == null).ToList();
		}

		[Test]
		public void Test_Twin_Implicit_Join_In_Where_Then_Project()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Name != "" && c.OpeningDate > new DateTime())
					.Where(c => c.Address.Street == "Madison Street"
						&& c.SecondAddress.Number == 0
						&& c.ThirdAddress.Number == 0
						&& c.ThirdAddress.Region.Diameter >= 0)
					.Select(c => c);

				var values = query.ToList();
			}
		}

		[Test]
		public void Test_Implicit_Join_From_Projection()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Select(c => new { shop = c, region = c.Address.Region, region2 = c.SecondAddress.Region })
					.Where(c => c.shop.Name != null && c.region2.Diameter >= 0 && c.region.Diameter >= 0);

				var values = query.ToList();
			}
		}

		[Test]
		public void Test_Implicit_Join_In_Where_Then_Project_Anonymous()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address.Street == "Madison Street")
					.Select(c => new { c });

				var first = query.First();
			}
		}


		[Test]
		public void Test_Include_With_One_Select()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops.Where(c => c.Address.Street == "Madison Street")
					.Select(c => c.IncludeDirect(d => d.Address).IncludeDirect(d => d.SecondAddress));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Where_With_Same_Property_Sub_Property_Before()
		{
			using (var scope = this.NewTransactionScope())
			{
#pragma warning disable CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
				var query = this.model.Shops
					.Where(c => c.SecondAddress.Number != null)
					.Include(c => c.SecondAddress);
#pragma warning restore CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Where_With_Same_Property_Before()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.SecondAddress != null)
					.Include(c => c.SecondAddress);

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Where_With_Same_Property_Afterwards()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Include(c => c.SecondAddress)
					.Where(c => c.SecondAddress != null);

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Where_With_Parent_Value_Afterwards()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Include(c => c.SecondAddress)
					.Where(c => c != null && c.Id == this.shopId);

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Where_With_Different_Object_Property_Afterwards()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address != null && c.Id == this.shopId)
					.Include(c => c.SecondAddress);

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Where_Complex_Key_Is_Not_Null()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address != null);

				var first = query.First();

				Assert.IsNotNull(first.Address);
				Assert.IsTrue(first.Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Where_Complex_Key_Is_Null()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address == null);

				var first = query.FirstOrDefault();

				Assert.IsNull(first);
			}
		}

		[Test]
		public void Test_Include_With_Where_With_Different_Property_Afterwards()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Include(c => c.SecondAddress)
					.Where(c => c.Address != null);

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}
		
		[Test]
		public void Test_Include_With_Two_Different_Selects()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Where(c => c.Address.Region.Name == "Washington")
					.Select(c => c.IncludeDirect(d => d.Address))
					.Select(c => c.IncludeDirect(d => d.SecondAddress));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Two_Different_QuerableIncludes()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address.Region.Name == "Washington")
					.Where(c => c.Id == this.shopId)
					.Include(c => c.Address)
					.Include(c => c.SecondAddress);

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_With_Two_Different_QuerableIncludes_Same_Property()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops.Where(c => c.Address.Region.Name == "Washington")
					.Select(c => c.IncludeDirect(d => d.Address))
					.Select(c => c.IncludeDirect(d => d.Mall))
					.Select(c => c.IncludeDirect(d => d.Address));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_Sample_Property_Twice()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops.Where(c => c.Address.Region.Name == "Washington")
					.Include(c => c.Address)
					.Include(c => c.Address);

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Implicit_Where_Join_Multiple_Depths_Not_Primary_Key()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					from
						shop in this.model.Shops
					where
						shop.Address.Region.Center.Label == "Center of Washington"
					select
						shop;

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Related_Object()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					from
						shop in this.model.Shops
					select
						shop.Address;

				var first = query.First();
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject_Two_Levels()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = from
					shop in this.model.Shops
					where shop.Id == this.shopId
					select shop.IncludeDirect(c => c.Address.Region);

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
				Assert.AreEqual("Washington", first.Address.Region.Name);
			}
		}

		[Test]
		public void Test_Select_Two_Implicit_Joins_At_Nested_Levels()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Select(c => new { c.Address, c.Address.Region });

				var first = query.First();
			}
		}

		[Test]
		public void Test_Select_Two_Implicit_Joins_At_Same_Level()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Select(c => new
					{
						c.Address,
						c.Mall
					});

				var first = query.First();
			}
		}

		[Test]
		public void Test_Select_Implicit_Join_On_RelatedObject_And_Other_Related_Object_Of_Same_Type1()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address.Street == "Madison Street" && c.SecondAddress.Street == "Jefferson Avenue")
					.Select(c => c.IncludeDirect(d => d.Address));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsTrue(first.SecondAddress.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Implicit_Join_On_RelatedObject_And_Other_Related_Object_Of_Same_Type2()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address.Street == "Madison Street" )
					.Where(c => c.SecondAddress.Street == "Jefferson Avenue")
					.Select(c => c.IncludeDirect(d => d.Address));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsTrue(first.SecondAddress.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Implicit_Join_On_RelatedObject_And_Other_Related_Object_Of_Same_Type3()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Address.Street == "Madison Street")
					.Where(c => c.SecondAddress.Street == "Jefferson Avenue")
					.Select(c => c.IncludeDirect(d => d.Address).IncludeDirect(d => d.SecondAddress));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.SecondAddress.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
				Assert.AreEqual("Jefferson Avenue", first.SecondAddress.Street);
			}
		}

		[Test]
		public void Test_Select_Include_And_Include_RelatedObjects()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(c => c.IncludeDirect(d => d.Address).IncludeDirect(d => d.Mall));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Mall.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_Off_Join()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = (from  mall  in this.model.Malls
							join shop in this.model.Shops on mall equals shop.Mall
							select new {  mall, shop }).Include(c => c.shop.Address);

				var first = query.First();

				Assert.IsFalse(first.shop.IsDeflatedReference());
				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_Select_Anon()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Malls.Include(c => c.Address).Select(c => new { c.Address.Number }).ToList();
			}
		}

		[Test]
		public void Test_NP1_Query()
		{
			using (var scope = this.NewTransactionScope())
			{
				var mall = this.model.Malls.First();

				this.model.Shops.Where(c => c.Mall == mall).ToList();
			}
		}

		[Test]
		public void Test_Select_Include_And_Include_RelatedObjects2()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(c => c.IncludeDirect(d => d.Address).IncludeDirect(d => d.Address.Region));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_And_Include_RelatedObjects3()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(c => c.IncludeDirect(d => DataAccessObjectInternalHelpers.IncludeDirect(d.Address, e => e.Region)));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_And_Include_RelatedObjects_Via_Pair1()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(c => new Tuple<string, Shop>("hi",  c ))
					.Select(c => c.Item2.IncludeDirect(e => e.Address.Region));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_And_Include_RelatedObjects_Via_Pair2()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(c => new Tuple<string, Shop>("hi", c ))
					.Select(c => c.Item2.IncludeDirect(d => DataAccessObjectInternalHelpers.IncludeDirect(d.Address, e => e.Region)));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject1()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(c => c.IncludeDirect(d => d.Address));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.AreEqual("Madison Street", first.Address.Street);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2a1()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						shop
					}).Include(c => c.shop.Address);

				var first = query.First();

				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
				Assert.IsTrue(first.shop.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2a3()
		{
			var y = new
			{
				address = this.model.Addresses.GetReference(
				new
				{
					Id = 1,
					Region = this.model.Regions.GetReference(new { Id = 1, Name = "" })
				})
			};

			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						x = new
						{
							y = new
							{
								shop
							}
						}
					}).Include(c => c.x.y.shop.Address.Region);

				var first = query.First();

				Assert.IsFalse(first.x.y.shop.Address.IsDeflatedReference());
				Assert.IsFalse(first.x.y.shop.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2a2()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						x = new
						{
							shop
						}
					}).Include(c => c.x.shop.Address);

				var first = query.First();

				Assert.IsFalse(first.x.shop.Address.IsDeflatedReference());
				Assert.IsTrue(first.x.shop.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2b()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						shop
					}).Select(c => c.IncludeDirect(d => d.shop.Address));

				var first = query.First();

				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
				Assert.IsTrue(first.shop.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2c()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						shop
					}).Where(c => c.shop.Address.Street == "Madison Street");

				var first = query.First();

				Assert.IsTrue(first.shop.Address.IsDeflatedReference());
				Assert.IsTrue(first.shop.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2d()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						shop
					}).Select(c => new { c, c.shop, c.shop.Address });

				var first = query.First();

				Assert.IsFalse(first.c.shop.Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject2e()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						shop
					}).Select(c => new { c.shop, c.shop.Address});

				var first = query.First();
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject3a()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(c => c).Select(c => c.IncludeDirect(d => d.Address.Region));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject3b()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						shopp = shop
					}).Select(c => c.shopp.IncludeDirect(d => d.Address.Region));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject3c()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Shops
					.Where(c => c.Id == this.shopId)
					.Select(shop => new
					{
						x = new
						{
							shopp = shop
						}
					}).Select(c => c.x.shopp.IncludeDirect(d => d.Address.Region));

				var first = query.First();

				Assert.IsFalse(first.Address.IsDeflatedReference());
				Assert.IsFalse(first.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Select_Project_Related_Object_And_Include1()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					(from
						shop in this.model.Shops
						select new
						{
							shop1 = new
							{
								 shop2 = shop
							}
						}
					).Select(c => new { shop = c.shop1.shop2, address = c.shop1.shop2.Address.IncludeDirect(d => d.Region) });


				var first = query.First();
				Assert.IsNotNull(first.shop);
				Assert.IsNotNull(first.address);
				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
				Assert.IsFalse(first.shop.Address.Region.IsDeflatedReference());
				Assert.AreEqual(first.shop.Address, first.address);
			}
		}

		[Test]
		public void Test_Select_Project_Related_Object_And_Include2()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					(from
						shop in this.model.Shops
					 select new
					 {
						 shop = new
						 {
							 shop = shop
						 }
					 }
					).Select(c => new { shop = c.shop.shop.IncludeDirect(d => d.Address.Region), address = c.shop.shop.Address });


				var first = query.First();
				Assert.IsNotNull(first.shop);
				Assert.IsNotNull(first.address);
				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
				Assert.IsFalse(first.shop.Address.Region.IsDeflatedReference());
				Assert.AreEqual(first.shop.Address, first.address);
			}
		}

		[Test]
		public void Test_Select_Project_Related_Object_And_Include3()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					(from
						shop in this.model.Shops
					 select new
					 {
						 shop = new
						 {
							 shop = shop.IncludeDirect(c => c.Address.Region)
						 }
					 }
					).Select(c => new { shop = c.shop.shop, address = c.shop.shop.Address });


				var first = query.First();
				Assert.IsNotNull(first.shop);
				Assert.IsNotNull(first.address);
				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
				Assert.IsFalse(first.shop.Address.Region.IsDeflatedReference());
				Assert.AreEqual(first.shop.Address, first.address);
			}
		}

		[Test]
		public void Test_Select_Project_Related_Object_And_Include4()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					(from
						shop in this.model.Shops
					 select new
					 {
						 shop = new
						 {
							 shop = shop.IncludeDirect(c => c.Address)
						 }
					 }
					).Select(c => new { shop = c.shop.shop, address = c.shop.shop.Address });


				var first = query.First();
				Assert.IsNotNull(first.shop);
				Assert.IsNotNull(first.address);
				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
				Assert.IsTrue(first.shop.Address.Region.IsDeflatedReference());
				Assert.AreEqual(first.shop.Address, first.address);
			}
		}

		[Test]
		public void Test_Select_Project_Related_Object_And_Include5()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					(from
						shop in this.model.Shops
					 select new
					 {
						 shop = new
						 {
							 shop = shop
						 }
					 }
					).Select(c => new { shop = c.shop.shop, address = c.shop.shop.Address });


				var first = query.First();
				Assert.IsNotNull(first.shop);
				Assert.IsNotNull(first.address);
				Assert.IsFalse(first.shop.Address.IsDeflatedReference());
				Assert.AreEqual(first.shop.Address, first.address);
			}
		}

		[Test]
		public void Test_Select_Include_Self()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					(from
						shop in this.model.Shops
					 select new
					 {
						 shop = new
						 {
							 shop
						 }
					 }
					).Select(c => new { shop = c.shop.shop.IncludeDirect(d => d) });

				var first = query.First();
				Assert.IsNotNull(first.shop);
			}
		}

		[Test]
		public void Test_Select_Include_RelatedObject_Nested_Anonymous()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					(from
						shop in this.model.Shops
					 select new
					 {
						 container = new
						 {
							 shop
						 }
					 }
					).Select(c => new { shop = c.container.shop.IncludeDirect(d => d.Address)});

				var first = query.First();
				Assert.IsNotNull(first.shop);
			}
		}

		[Test]
		public void Test_Implicit_Where_Join_Multiple_Depths_Primary_Key()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query =
					from
						shop in this.model.Shops
					where
						shop.Address.Region.Name == "Washington"
					select
						shop;

				var first = query.First();

				Assert.IsTrue(first.Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Implicit_Join_With_Complex_Primary_Key()
		{
			using (var scope = this.NewTransactionScope())
			{
				var mall = this.model.Malls.Create();
				var shop = mall.Shops.Create();

				mall.Name = "Westfield";
				
				shop.Address = this.model.Addresses.Create();
				shop.Address.Region = this.model.Regions.Create();
				shop.Address.Region.Name = "City of London";
				shop.Name = "Apple Store";

				scope.Complete();
			}

			using (var scope = this.NewTransactionScope())
			{
				Assert.IsNotNull(this.model.Malls.First(c => c.Name == "Westfield"));
				Assert.IsNotNull(this.model.Shops.FirstOrDefault(c => c.Name == "Apple Store"));
				Assert.IsNotNull(this.model.Shops.First(c => c.Mall.Name == "Westfield"));
				
				var query =
					from
						shop in this.model.Shops
					where shop.Mall.Name.StartsWith("Westfield")
					select shop;

				Assert.IsNotEmpty(query.ToList());
				
				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Complex_Primary_Key()
		{
			long shopId;
			long addressId;
			long regionId;
			
			using (var scope = this.NewTransactionScope())
			{
				var shop = this.model.Shops.Create();
				shop.Address = this.model.Addresses.Create();
				shop.Address.Region = this.model.Regions.Create();
				shop.Address.Region.Name = "City of London";

				shop.Name = "Apple Store";

				scope.Flush();

				shopId = shop.Id;
				addressId = shop.Address.Id;
				regionId = shop.Address.Region.Id;

				scope.Complete();
			}

			using (var scope = this.NewTransactionScope())
			{
				var shop = this.model.Shops.FirstOrDefault(c => c.Id == shopId);

				Assert.IsNotNull(shop);
				Assert.IsNotNull(shop.Address);
				Assert.IsNotNull(shop.Address.Region);
				Assert.AreEqual(addressId, shop.Address.Id);
				Assert.AreEqual(regionId, shop.Address.Region.Id);

				scope.Complete();
			}

			using (var scope = this.NewTransactionScope())
			{
				var shop = this.model.Shops.FirstOrDefault(c => c.Address.Region.Id == regionId);

				Assert.IsNotNull(shop);
				Assert.IsNotNull(shop.Address);
				Assert.IsNotNull(shop.Address.Region);
				Assert.AreEqual(addressId, shop.Address.Id);
				Assert.AreEqual(regionId, shop.Address.Region.Id);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_Any_On_RelatedObjects()
		{
			using (var scope = this.NewTransactionScope())
			{
				var malls = this.model
					.Malls
					.Where(c => c.Shops.Any(d => d.Name != ""))
					.ToList();
			}
		}

		[Test]
		public void Test_Include_Collection1()
		{
			using (var scope = this.NewTransactionScope())
			{
				var malls = this.model
					.Malls
					.Include(c => c.Shops.IncludedItems().Address)
					.OrderBy(c => c.Name)
					.ToList();
			}
		}

		[Test]
		public void Test_Include_Collection2()
		{
			using (var scope = this.NewTransactionScope())
			{
				var malls = this.model
					.Malls
					.Include(c => c.SisterMall.Shops.IncludedItems().Address).ToList();

				var mall = malls.First(c => c.Name.Contains("Seattle City"));
				Assert.IsNull(mall.Address);
				Assert.IsTrue(mall.SisterMall.Address.IsDeflatedReference());
				var shops = mall.SisterMall.Shops.ToList();
				Assert.IsFalse(shops[0].Address.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include_Collection3()
		{
			using (var scope = this.NewTransactionScope())
			{
				var mall1 = this.model
					.Malls
					.Include(c => c.SisterMall.Shops.Include(d => d.Address.Region))
					.First(c => c.Name.Contains("Seattle City"));

				var mall2 = this.model
					.Malls
					.Include(c => c.SisterMall.Shops.Include(d => d.Address.Region))
					.First(c => c.Name.Contains("Seattle City"));

				var shops1 = mall1.SisterMall.Shops.ToList();
				var shops2 = mall2.SisterMall.Shops.ToList();

				Assert.AreEqual(2, shops1.Count);
				Assert.AreEqual(2, shops2.Count);

				foreach (var shop in shops1)
				{
					Assert.IsFalse(shop.Address.IsDeflatedReference());
					Assert.IsFalse(shop.Address.Region.IsDeflatedReference());
				}

				foreach (var shop in shops2)
				{
					Assert.IsFalse(shop.Address.IsDeflatedReference());
					Assert.IsFalse(shop.Address.Region.IsDeflatedReference());
				}
			}
		}

		[Test]
		public void Test_Include_Collection4a()
		{
			using (var scope = this.NewTransactionScope())
			{
				var malls = this.model
					.Malls
					.Include(d => d.SisterMall.Address.Region).ToList();

				var mall = malls.First(c => c.Name.Contains("Seattle City"));
				Assert.IsFalse(mall.SisterMall.Address.IsDeflatedReference());
				Assert.IsFalse(mall.SisterMall.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Implicit_Include_Using_Interface()
		{
			var result = Test_Implicit_Include_Using_Interface_Private(this.model.GetDataAccessObjects<Mall>()).ToList();

			Assert.IsNotEmpty(result);
		}

		public IQueryable<T> Test_Implicit_Include_Using_Interface_Private<T>(IQueryable<T> queryable)
			where T: DataAccessObject, INamed
		{
			return queryable.Where(c => c.Name == "Seattle City");
		}
		
		[Test]
		public void Test_Include1()
		{
			var malls = this.model
				.Malls
				.Include(c => c.SisterMall.Shops)
				.Include(c => c.SisterMall.Shops2)
				.Include(c => c.SisterMall.Shops3)
				.Where(c => c.Name == "Seattle City")
				.ToList();

			var hashSet = new HashSet<Mall>(malls);

			Assert.AreEqual(hashSet.Count, malls.Count);

			malls = this.model
				.Malls
				.Include(c => c.SisterMall.Shops)
				.Include(c => c.SisterMall.Shops2)
				.Include(c => c.SisterMall.Shops3)
				.Where(c => c.Name == "Seattle City")
				.ToList();

			hashSet = new HashSet<Mall>(malls);

			Assert.AreEqual(hashSet.Count, malls.Count);
		}
		
		[Test]
		public void Test_Include2()
		{
			using (var scope = this.NewTransactionScope())
			{
				var malls = this.model
					.Malls
					.Include(d => d.SisterMall.Address.IncludeDirect(c => c.Region)).ToList();

				var mall = malls.First(c => c.Name.Contains("Seattle City"));
				Assert.IsFalse(mall.SisterMall.Address.IsDeflatedReference());
				Assert.IsFalse(mall.SisterMall.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include3()
		{
			using (var scope = this.NewTransactionScope())
			{
				var mall = this.model
					.Malls
					.Where(c => c.Name == "Seattle City")
					.Include(d => d.SisterMall.Address.Region)
					.First(c => c.Address == null);

				Assert.IsNull(mall.Address);
				Assert.AreEqual("Seattle City", mall.Name);
				Assert.IsNotNull(mall.SisterMall);
				Assert.IsFalse(mall.SisterMall.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_Include4()
		{
			using (var scope = this.NewTransactionScope())
			{
				var mall = this.model
					.Malls
					.Include(d => d.Address.Region)
					.First(c => c.Address.Region != null);

				Assert.IsFalse(mall.Address.Region.IsDeflatedReference());
			}
		}

		[Test]
		public void Test_OrderBy_Complex_Object()
		{
			using (var scope = this.NewTransactionScope())
			{
				var malls = this.model.Shops.OrderBy(c => c).ToList();
			}
		}

		[Test]
		public void Test_OrderBy_DaoProperty_With_Collection_Include1()
		{
			using (var scope = this.NewTransactionScope())
			{
				var malls = this.model.Malls
					.Include(c => c.Shops.IncludedItems().Address.Region)
					.Include(c => c.Shops2)
					.Include(c => c.Address)
					.ToList();

				var mall = malls.First();
				var count = mall.Shops.Items().Count;

				Assert.That(mall.Shops.HasItems);
				Assert.That(mall.Shops2.HasItems);
				Assert.That(mall.Shops.Items().Count, Is.GreaterThan(0));

				Assert.That(mall.Shops.Count(), Is.GreaterThan(0));

				var malls2 = this.model.Malls
					.Include(c => c.Shops.IncludedItems().Address.Region)
					.Include(c => c.Shops)
					.Include(c => c.Address)
					.ToList();

				Assert.IsFalse(mall.Shops.Items().First().Address.IsDeflatedReference());
				Assert.IsFalse(mall.Shops.Items().First().Address.Region.IsDeflatedReference());

				var mall2 = malls2.First();

				Assert.AreEqual(count, mall2.Shops.Items().Count);
				Assert.AreSame(mall, mall2);

				var x = mall.Shops2.ToList();

				Assert.That(mall.Shops2.Count(), Is.EqualTo(0));

				scope.Flush();

				Assert.That(mall.Shops.HasItems);
				Assert.That(mall.Shops2.HasItems);

				Assert.That(mall.Shops.Count(), Is.GreaterThan(0));
				Assert.That(mall.Shops2.Count(), Is.EqualTo(0));
			}
		}

		[Test]
		public void Test_Include_Without_Scope()
		{
			var malls = this.model.Malls
				.Include(c => c.Shops)
				.Include(c => c.Shops2.IncludedItems().Address.Region)
				.Include(c => c.Address)
				.ToList();

			var mall = malls.First();
			var count = mall.Shops2.Items().Count;

			Assert.That(mall.Shops.HasItems);
			Assert.That(mall.Shops2.HasItems);
			Assert.That(mall.Shops.Items().Count, Is.GreaterThan(0));

			Assert.That(mall.Shops.Count(), Is.GreaterThan(0));

			var malls2 = this.model.Malls
				.Include(c => c.Shops)
				.Include(c => c.Shops2.IncludedItems().Address.Region)
				.Include(c => c.Address)
				.ToList();

			var mall2 = malls2.First();

			Assert.AreEqual(count, mall2.Shops2.Items().Count);
			Assert.AreNotSame(mall, mall2);
			Assert.AreEqual(mall, mall2);

			var x = mall.Shops2.ToList();

			Assert.That(mall.Shops2.Count(), Is.EqualTo(0));

			Assert.That(mall.Shops.HasItems);
			Assert.That(mall.Shops2.HasItems);

			Assert.That(mall.Shops.Count(), Is.GreaterThan(0));
			Assert.That(mall.Shops2.Count(), Is.EqualTo(0));
		}

		[Test]
		public void Test_OrderBy_DaoProperty_With_Collection_CustomProject1()
		{
			using (var scope = this.NewTransactionScope())
			{
				var results = this.model.Malls
					.Where(c => c.Name == "Seattle City")
					.Include(c => c.Shops)
					.Include(c => c.Shops2.IncludedItems().Address.Region)
					.Include(c => c.Address)
					.Select(c => new { mall = c, shops = c.Shops })
					.ToList();

				var x = results[0].shops.Items().Count;

				Assert.That(results[0].shops.Items().Count, Is.GreaterThan(0));

				results = this.model.Malls
					.Where(c => c.Name == "Seattle City")
					.Include(c => c.Shops)
					.Include(c => c.Shops2.IncludedItems().Address.Region)
					.Include(c => c.Address)
					.Select(c => new { mall = c, shops = c.Shops })
					.ToList();
				
				Assert.That(results[0].shops.Items().Count, Is.EqualTo(x));
			}
		}

		[Test]
		public void Test_OrderBy_DaoProperty_With_Collection_CustomProject2()
		{
			using (var scope = this.NewTransactionScope())
			{
				var results = this.model.Malls
					.Where(c => c.Name == "Seattle City")
					.Include(c => c.Shops)
					.Include(c => c.Shops2.IncludedItems().Address.Region)
					.Include(c => c.Address)
					.Select(c => new { mall = c, shops = c.Shops2 })
					.ToList();

				Assert.That(results[0].shops.Items().Count, Is.EqualTo(0));

				results = this.model.Malls
					.Where(c => c.Name == "Seattle City")
					.Include(c => c.Shops)
					.Include(c => c.Shops2.IncludedItems().Address.Region)
					.Include(c => c.Address)
					.Select(c => new { mall = c, shops = c.Shops2 })
					.ToList();

				Assert.That(results[0].shops.Items().Count, Is.EqualTo(0));
			}
		}

		[Test]
		public void Test_OrderBy_DaoProperty_With_Collection_Include2()
		{
			using (var scope = this.NewTransactionScope())
			{
				var malls = this.model.Malls
					.Include(c => c.Shops)
					.Include(c => c.SisterMall.Shops)
					.Include(c => c.Shops2.IncludedItems().Address.Region)
					.Include(c => c.Address)
					.OrderBy(c => c.Name)
					.ToList();
			}
		}

		[Test]
		public void Test_OrderBy_DaoProperty_With_Collection_Include3()
		{
			using (var scope = this.NewTransactionScope())
			{
				var malls = this.model.Malls
					.OrderBy(c => c.Name)
					.Include(c => c.Shops)
					.Include(c => c.SisterMall.Shops)
					.Include(c => c.Shops2.OrderBy(d => d.CloseDate).IncludedItems().Address.Region)
					.Include(c => c.Address)
					.ToList();
			}
		}

		[Test]
		public void Test_OrderBy_DaoProperty_With_Collection_Include4()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Malls
					.Include(c => c.SisterMall.Shops)
					.Include(c => c.SisterMall2.Shops);

				var malls = query.ToList();

				foreach (var mall in malls.Where(c => c.SisterMall != null))
				{
					var s1 = mall.SisterMall.Shops.Items();

					Assert.AreEqual(2, s1.Count);

					var s3 = mall.SisterMall2.Shops.Items();

					Assert.AreEqual(2, s3.Count);
				}
			}
		}

		[Test]
		public void Test_OrderBy_DaoProperty_With_Collection_Include5()
		{
			using (var scope = this.NewTransactionScope())
			{
				var query = this.model.Malls
					.OrderByDescending(c => c.Name)
					.Where(c => c.Name != "")
					.Include(c => c.SisterMall.Shops)
					.Include(c => c.SisterMall.Shops2)
					.Include(c => c.SisterMall.Shops3)
					.Distinct();

				var malls = query.ToList();

				foreach (var mall in malls.Where(c => c.SisterMall != null))
				{
					var s1 = mall.SisterMall.Shops.Items();

					Assert.AreEqual(2, s1.Count);
					Assert.IsTrue(s1.All(c => !c.IsDeflatedReference()));

					var s2 = mall.SisterMall.Shops2.Items();
					Assert.AreEqual(0, s2.Count);

					var s3 = mall.SisterMall.Shops3.Items();
					Assert.AreEqual(1, s3.Count);
					Assert.IsTrue(s3.All(c => !c.IsDeflatedReference()));
					Assert.AreEqual("Sister Mall Store B", s3.Single().Name);
				}
			}
		}

		[Test]
		public void Test_OrderBy_DaoProperty_With_Collection_Include5_DAS()
		{
			using (var scope = new DataAccessScope())
			{
				var query = this.model.Malls
					.OrderByDescending(c => c.Name)
					.Where(c => c.Name != "")
					.Include(c => c.SisterMall.Shops)
					.Include(c => c.SisterMall.Shops2)
					.Include(c => c.SisterMall.Shops3)
					.Distinct();

				var malls = query.ToList();

				foreach (var mall in malls.Where(c => c.SisterMall != null))
				{
					var s1 = mall.SisterMall.Shops.Items();

					Assert.AreEqual(2, s1.Count);
					Assert.IsTrue(s1.All(c => !c.IsDeflatedReference()));

					var s2 = mall.SisterMall.Shops2.Items();
					Assert.AreEqual(0, s2.Count);

					var s3 = mall.SisterMall.Shops3.Items();
					Assert.AreEqual(1, s3.Count);
					Assert.IsTrue(s3.All(c => !c.IsDeflatedReference()));
					Assert.AreEqual("Sister Mall Store B", s3.Single().Name);
				}
			}
		}
		
		[Test]
		public void Test_OrderBy_DaoProperty_With_Collection_Include6_Async1()
		{
			var func = (Func<Task>)async delegate
			{
				var query = this.model.Malls
					.Include(c => c.Shops)
					.Include(c => c.SisterMall.IncludeDirect(d => d.Shops).IncludeDirect(d => d.Shops2));

				var malls = await query.ToListAsync();
				
				foreach (var mall in malls.Where(c => c.SisterMall != null))
				{
					Assert.AreEqual(1, mall.Shops.Items().Count);
					Assert.AreEqual(mall.Shops.Items()[0].Name, "Microsoft Store");

					var s1 = mall.SisterMall.Shops.Items();

					Assert.AreEqual(2, s1.Count);
					Assert.IsTrue(s1.All(c => !c.IsDeflatedReference()));
					Assert.IsTrue(s1.All(c => c.Name.StartsWith("Sister Mall Store")));

					var s2 = mall.SisterMall.Shops2.Items();

					Assert.AreEqual(0, s2.Count);
				}
			};

			func().GetAwaiter().GetResult();
		}

		[Test]
		public void Test_OrderBy_DaoProperty_With_Collection_Include6_Async2()
		{
			var func = (Func<Task>)async delegate
			{
				using (var scope = DataAccessScope.CreateReadCommitted())
				{
					var query = this.model.Malls
						.Include(c => c.Shops)
						.Include(c => c.SisterMall.IncludeDirect(d => d.Shops).IncludeDirect(d => d.Shops2));

					var malls = await query.ToListAsync();

					foreach (var mall in malls.Where(c => c.SisterMall != null))
					{
						Assert.AreEqual(1, mall.Shops.Items().Count);
						Assert.AreEqual(mall.Shops.Items()[0].Name, "Microsoft Store");

						var s1 = mall.SisterMall.Shops.Items();

						Assert.AreEqual(2, s1.Count);
						Assert.IsTrue(s1.All(c => !c.IsDeflatedReference()));
						Assert.IsTrue(s1.All(c => c.Name.StartsWith("Sister Mall Store")));

						var s2 = mall.SisterMall.Shops2.Items();

						Assert.AreEqual(0, s2.Count);
					}
				}
			};

			func().GetAwaiter().GetResult();
		}

		[Test]
		public void Test_OrderBy_DaoProperty_With_Collection_Include6()
		{
			var query = this.model.Malls
				.Include(c => c.Shops)
				.Include(c => c.SisterMall.IncludeDirect(d => d.Shops).IncludeDirect(d => d.Shops2));

			var malls = query.ToList();

			foreach (var mall in malls.Where(c => c.SisterMall != null))
			{
				Assert.AreEqual(1, mall.Shops.Items().Count);
				Assert.AreEqual(mall.Shops.Items()[0].Name, "Microsoft Store");

				var s1 = mall.SisterMall.Shops.Items();

				Assert.AreEqual(2, s1.Count);
				Assert.IsTrue(s1.All(c => !c.IsDeflatedReference()));
				Assert.IsTrue(s1.All(c => c.Name.StartsWith("Sister Mall Store")));

				var s2 = mall.SisterMall.Shops2.Items();

				Assert.AreEqual(0, s2.Count);
			}
		}

		[Test]
		public void Test_Join_With_Multiple_Conditions()
		{
			var x = from mall in this.model.Malls
				join shop in this.model.Shops
				on new { mall.TopShop.Id, mall.Address  } equals new { shop.Id, shop.Address } into shops
				from shop in shops.DefaultIfEmpty()
				select new 
				{
					mall.TopShop.Id,
					mall.Address
				};

			var y = x.ToList();
		}
	}
}
