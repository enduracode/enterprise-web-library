using System;
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
		/// <param name="updateRegionLinkers">A collection of update-region linkers. Do not pass null. Since an identified component (or any component) can appear
		/// multiple times on a page, the update regions of each instance are scoped to the instance. This prevents an instance from “reaching out” of itself and
		/// selecting update regions inside other instances of the same component on the page.</param>
		/// <param name="errorSources"></param>
		/// <param name="childGetter">If the child components depend on the modification error dictionary, do not pass null for the component ID. Having an ID
		/// container is important so that when the components differ before and after a transfer, other parts of the page such as form controls do not get
		/// affected.</param>
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