using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class IdentifiedComponentData<ChildType> where ChildType: PageComponent {
		internal readonly bool IsIdContainer;
		internal readonly IEnumerable<UpdateRegionLinker> UpdateRegionLinkers;
		internal readonly IEnumerable<EwfValidation> Validations;
		internal readonly Func<ModificationErrorDictionary, IEnumerable<ChildType>> ChildGetter;

		internal IdentifiedComponentData(
			bool isIdContainer, IEnumerable<UpdateRegionLinker> updateRegionLinkers, IEnumerable<EwfValidation> validations,
			Func<ModificationErrorDictionary, IEnumerable<ChildType>> childGetter ) {
			IsIdContainer = isIdContainer;
			UpdateRegionLinkers = updateRegionLinkers;
			Validations = validations;
			ChildGetter = childGetter;
		}
	}
}