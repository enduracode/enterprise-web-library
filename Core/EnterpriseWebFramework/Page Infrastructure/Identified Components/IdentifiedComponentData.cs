﻿using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class IdentifiedComponentData<ChildType> where ChildType: PageComponent {
		internal readonly string Id;
		internal readonly IReadOnlyCollection<UpdateRegionLinker> UpdateRegionLinkers;
		internal readonly ErrorSourceSet ErrorSources;
		internal readonly Func<ModificationErrorDictionary, IEnumerable<ChildType>> ChildGetter;

		/// <summary>
		/// Creates an identified-component-data object.
		/// </summary>
		/// <param name="id">The component ID. Pass a nonempty string to specify an ID and make the component an ID container. Pass the empty string for an
		/// ID container only. Pass null for no ID and no container. A component with a specified ID cannot be used more than once in the same ID container.</param>
		/// <param name="updateRegionLinkers"></param>
		/// <param name="errorSources"></param>
		/// <param name="childGetter"></param>
		internal IdentifiedComponentData(
			string id, IReadOnlyCollection<UpdateRegionLinker> updateRegionLinkers, ErrorSourceSet errorSources,
			Func<ModificationErrorDictionary, IEnumerable<ChildType>> childGetter ) {
			Id = id;
			UpdateRegionLinkers = updateRegionLinkers;
			ErrorSources = errorSources;
			ChildGetter = childGetter;
		}
	}
}