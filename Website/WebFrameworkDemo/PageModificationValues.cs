// EwlPage

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo;

partial class PageModificationValues {
	protected override string getResourceName() => "Page-Modification Values";

	protected override PageContent getContent() {
		var pmv = new PageModificationValue<bool>();
		return new UiPageContent(
				contentFootActions: new ButtonSetup( "Post Back", behavior: new PostBackBehavior( postBack: PostBack.CreateIntermediate( null ) ) ).ToCollection() )
			.Add( new Checkbox( false, "Test".ToComponents(), setup: CheckboxSetup.Create( pageModificationValue: pmv ) ).ToFormItem().ToComponentCollection() )
			.Add(
				new Paragraph(
					"Value: ".ToComponents()
						.Concat(
							pmv.ToGenericPhrasingContainer(
								v => $"{( v ? "true" : "false" )} (server side)",
								valueExpression => $"{valueExpression}.toString() + ' (client side)'" ) )
						.Materialize() ) );
	}
}