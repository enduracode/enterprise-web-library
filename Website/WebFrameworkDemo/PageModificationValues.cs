namespace EnterpriseWebLibrary.Website.WebFrameworkDemo;

// EwlPage
partial class PageModificationValues {
	protected override string getResourceName() => "Page-Modification Values";

	protected override PageContent getContent() {
		var pmv = new PageModificationValue<bool>();
		return new UiPageContent(
			contentFootActions: new ButtonSetup( "Post Back", behavior: new PostBackBehavior( postBack: PostBack.CreateIntermediate( null ) ) ) ).Add(
			new FlowCheckbox(
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
						nestedContentAlwaysDisplayed: true ) ).ToFormItem()
				.ToComponentCollection() );
	}
}