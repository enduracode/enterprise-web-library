using System;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A select list whose visible options depend upon the selected value in another select list.
	/// </summary>
	public static class DependentSelectList {
		public static DependentSelectList<ItemIdType, ParentItemIdType> Create<ItemIdType, ParentItemIdType>(
			Func<ParentItemIdType> parentItemIdGetter, PageModificationValue<ParentItemIdType> parentItemIdPmv ) =>
			new DependentSelectList<ItemIdType, ParentItemIdType>( parentItemIdGetter, parentItemIdPmv );
	}


	/// <summary>
	/// A select list whose visible options depend upon the selected value in another select list.
	/// </summary>
	public class DependentSelectList<ItemIdType, ParentItemIdType>: Control, INamingContainer {
		private readonly Func<ParentItemIdType> parentItemIdGetter;
		private readonly PageModificationValue<ParentItemIdType> parentItemIdPmv;

		internal DependentSelectList( Func<ParentItemIdType> parentItemIdGetter, PageModificationValue<ParentItemIdType> parentItemIdPmv ) {
			this.parentItemIdGetter = parentItemIdGetter;
			this.parentItemIdPmv = parentItemIdPmv;
		}

		/// <summary>
		/// Adds a select list, which will only be visible when the specified parent value is selected.
		/// </summary>
		public void AddSelectList( ParentItemIdType parentItemId, Func<SelectList<ItemIdType>> selectListGetter ) {
			var selectList = FormState.ExecuteWithValidationPredicate(
				() => EwlStatics.AreEqual( parentItemId, parentItemIdGetter() ),
				() => selectListGetter().ToFormItem().ToControl() );

			Controls.Add( selectList );

			parentItemIdPmv.ToCondition( parentItemId.ToCollection() ).AddDisplayLink( selectList.ToCollection() );
		}
	}
}