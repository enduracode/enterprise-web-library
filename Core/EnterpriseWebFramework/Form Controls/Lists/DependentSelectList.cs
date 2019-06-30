using System;
using System.Collections.Generic;

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
	public class DependentSelectList<ItemIdType, ParentItemIdType>: FlowComponent {
		private readonly Func<ParentItemIdType> parentItemIdGetter;
		private readonly PageModificationValue<ParentItemIdType> parentItemIdPmv;
		private readonly List<FlowComponent> children = new List<FlowComponent>();

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
				() => selectListGetter()
					.ToFormItem( setup: new FormItemSetup( displaySetup: parentItemIdPmv.ToCondition( parentItemId.ToCollection() ).ToDisplaySetup() ) )
					.ToComponent() );
			children.Add( selectList );
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
	}
}