// EwlPage

namespace EnterpriseWebLibrary.Website.TestPages;

partial class FormItemListDemo {
	protected override string getResourceName() => "Form Item List";

	protected override PageContent getContent() =>
		new UiPageContent( omitContentBox: true ).Add( getSection( "Stack", FormItemList.CreateStack( generalSetup: getSetup(), items: getFormItems() ) ) )
			.Add( getSection( "Wrapping", FormItemList.CreateWrapping( setup: getSetup(), items: getFormItems() ) ) )
			.Add( getSection( "Responsive grid", FormItemList.CreateResponsiveGrid( generalSetup: getSetup(), items: getFormItems() ) ) )
			.Add( getSection( "Fixed grid", FormItemList.CreateFixedGrid( 6, generalSetup: getSetup(), items: getFormItems() ) ) );

	private Section getSection( string heading, FormItemList content ) => new( heading, content.ToCollection(), style: SectionStyle.Box );

	private FormItemListSetup getSetup() => new( buttonSetup: new ButtonSetup( "Submit" ) );

	private IReadOnlyCollection<FormItem> getFormItems() {
		var boxId = new ModalBoxId();
		return new TextControl( "", true ).ToFormItem(
				setup: new FormItemSetup( columnSpan: 2 ),
				label: "Model number ".ToComponents()
					.Append(
						new EwfButton(
							new StandardButtonStyle( "(popup)", buttonSize: ButtonSize.ShrinkWrap ),
							behavior: new OpenModalBehavior( boxId, etherealChildren: new ModalBox( boxId, true, "More information...".ToComponents() ).ToCollection() ) ) )
					.Materialize() )
			.Append( "".ToComponents().ToFormItem( label: "Normal price".ToComponents() ) )
			.Append( new TextControl( "", true ).ToFormItem( label: "Actual price".ToComponents() ) )
			.Append( new TextControl( "", true ).ToFormItem( label: "Quantity".ToComponents() ) )
			.Append( "".ToComponents().ToFormItem( label: "Inventory".ToComponents() ) )
			.Append( "".ToComponents().ToFormItem( label: "Bill Number".ToComponents() ) )
			.Materialize();
	}
}