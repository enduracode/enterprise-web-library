// EwlPage

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo;

partial class FormItemListDemo {
	protected override string getResourceName() => "Form Item List";

	protected override PageContent getContent() =>
		new UiPageContent( omitContentBox: true ).Add( getSection( "Stack", FormItemList.CreateStack( generalSetup: getSetup() ).AddItems( getFormItems() ) ) )
			.Add( getSection( "Wrapping", FormItemList.CreateWrapping( setup: getSetup() ).AddItems( getFormItems() ) ) )
			.Add( getSection( "Responsive grid", FormItemList.CreateResponsiveGrid( generalSetup: getSetup() ).AddItems( getFormItems() ) ) )
			.Add( getSection( "Fixed grid", FormItemList.CreateFixedGrid( 6, generalSetup: getSetup() ).AddItems( getFormItems() ) ) );

	private Section getSection( string heading, FormItemList content ) => new( heading, content.ToCollection(), style: SectionStyle.Box );

	private FormItemListSetup getSetup() => new( buttonSetup: new ButtonSetup( "Submit" ) );

	private IReadOnlyCollection<FormItem> getFormItems() {
		var boxId = new ModalBoxId();
		return new TextControl( "", true, setup: TextControlSetup.Create( widthOverride: 45.ToEm() ) ).ToFormItem(
				setup: new FormItemSetup( columnSpan: 2 ),
				label: "Model number ".ToComponents()
					.Append(
						new EwfButton(
							new StandardButtonStyle( "(popup)", buttonSize: ButtonSize.ShrinkWrap ),
							behavior: new OpenModalBehavior( boxId, etherealChildren: new ModalBox( boxId, true, "More information...".ToComponents() ).ToCollection() ) ) )
					.Materialize() )
			.Append( "$150".ToComponents().ToFormItem( label: "Normal price".ToComponents() ) )
			.Append(
				new TextControl(
					"",
					true,
					maxLength: 10,
					validationMethod: ( value, validator ) => {
						if( value.Length > 0 )
							validator.NoteErrorAndAddMessage( "The price is wrong. Also, this error message is far too long." );
					} ).ToFormItem( label: "Actual price".ToComponents() ) )
			.Append( new NumberControl( null, true ).ToFormItem( label: "Quantity".ToComponents() ) )
			.Append( new TextControl( "", true, setup: TextControlSetup.Create( numberOfRows: 3 ) ).ToFormItem( label: "Inventory".ToComponents() ) )
			.Append( new Checkbox( false, label: "Tax exempt".ToComponents() ).ToFormItem() )
			.Append( new NumberControl( null, true ).ToFormItem( label: "Quantity shipped".ToComponents() ) )
			.Materialize();
	}
}