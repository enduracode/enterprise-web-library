using Humanizer;

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo;

// EwlPage
partial class PageModificationValues {
	protected override string getResourceName() => "Page-Modification Values";

	protected override PageContent getContent() {
		var items = Enumerable.Range( 1, 3 ).Materialize();
		var selectedItemPmv = new PageModificationValue<int?>();
		return new UiPageContent(
				contentFootActions: new ButtonSetup( "Post Back", behavior: new PostBackBehavior( postBack: PostBack.CreateIntermediate( null ) ) ) )
			.Add( FormItemList.CreateFixedGrid( 1 ).AddItem( getCheckbox() ).AddItem( getDropDown( items, selectedItemPmv ) ) )
			.Add(
				FormItemList.CreateFixedGrid(
						1,
						generalSetup: new FormItemListSetup( displaySetup: selectedItemPmv.ToCondition( items.Select( i => (int?)i ) ).ToDisplaySetup() ) )
					.AddItem( "Conditional content".ToFormItem() ) );
	}

	private FormItem getCheckbox() {
		var pmv = new PageModificationValue<bool>();
		return new FlowCheckbox(
			false,
			"Test".ToComponents(),
			setup: FlowCheckboxSetup.Create(
				pageModificationValue: pmv,
				nestedContentGetter: () =>
					"Value: ".ToComponents()
						.Append(
							pmv.ToGenericPhrasingContainer(
								v => $"{( v ? "true" : "false" )} (server side)",
								valueExpression => $"{valueExpression}.toString() + ' (client side)'" ) )
						.Materialize(),
				nestedContentAlwaysDisplayed: true ) ).ToFormItem();
	}

	private FormItem getDropDown( IReadOnlyCollection<int> items, PageModificationValue<int?> selectedItemPmv ) =>
		SelectList.CreateDropDown(
				DropDownSetup.Create( items.Select( i => SelectListItem.Create( (int?)i, i.ToWords().Capitalize() ) ), itemIdPageModificationValue: selectedItemPmv ),
				null )
			.ToFormItem();
}