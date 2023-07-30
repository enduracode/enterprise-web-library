#nullable disable
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class TableSelectedItemData<ItemIdType> {
		internal IReadOnlyCollection<ButtonSetup> Buttons;
		internal EwfValidation Validation;

		internal IReadOnlyList<( IReadOnlyCollection<ButtonSetup> buttons, EwfValidation validation, IReadOnlyCollection<PhrasingComponent> checkboxes,
			List<ItemIdType> selectedIds )?> ItemGroupData;
	}
}