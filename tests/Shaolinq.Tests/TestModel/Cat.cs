﻿// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public class Cat
		: DataAccessObject<long>
	{
		[PersistedMember]
		[Index(IndexName = "Index")]
		public virtual string Name { get; set; }

		[PersistedMember]
		[ComputedMember("Id + 100000000", "Id = value - 100000000")]
		public virtual long? MutatedId { get; set; }

		[Index("CompositeIndexOverObjectAndNonObject", CompositeOrder = 2)]
		[Index(IndexName = "Index", SortOrder = SortOrder.Descending)]
		[PersistedMember, DefaultValue(9)]
		public virtual int LivesRemaining { get; set; }

		[Index("CompositeIndexOverObjectAndNonObject", CompositeOrder = 1)]
		[PersistedMember]
		public virtual Dog Companion { get; set; }

		[BackReference("ParentCatFoo")]
		[ForeignObjectConstraint(OnDeleteAction = ForeignObjectAction.Restrict, OnUpdateAction = ForeignObjectAction.Restrict)]
		[Index("CompositeIndexOverObjectAndNonObject", CompositeOrder = 3)]
		public virtual Cat Parent { get; set; }
		
		[RelatedDataAccessObjects]
		public virtual RelatedDataAccessObjects<Cat> Kittens { get; }

		[BackReference]
		public virtual Student Student { get; set; }
	}
}
