// EwlPage
// OptionalParameter: bool customerIsBusiness

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo;

partial class DataUpdateModification {
	protected override string getResourceName() => "Data-Update Modification";

	protected override PageContent getContent() =>
		new UiPageContent().Add(
				FormItemList.CreateStack()
					.AddItems(
						new DataValue<string>().ToTextControl( false, setup: TextControlSetup.CreateReadOnly(), value: "John Doe" )
							.ToFormItem( label: "Customer name".ToComponents() )
							.Append(
								parametersModification.GetCustomerIsBusinessRadioListFormItem(
									RadioListSetup.Create( SelectList.GetTrueFalseItems( "Business", "Individual" ).Reverse() ),
									label: "Customer type".ToComponents() ) )
							.Materialize() ) )
			.Add( getSendSampleSection() );

	private FlowComponent getSendSampleSection() {
		var package = new DataValue<string>();
		return FormState.ExecuteWithDataModificationsAndDefaultAction(
			PostBack.CreateFull(
					id: "sample",
					modificationMethod: () => {
						var customerType = CustomerIsBusiness ? "business" : "individual";
						AddStatusMessage( StatusMessageType.Info, $"{package.Value} sent to {customerType}." );
					} )
				.ToCollection(),
			() => new Section(
				"Send a sample",
				FormItemList.CreateWrapping( setup: new FormItemListSetup( buttonSetup: new ButtonSetup( "Send" ) ) )
					.AddItem(
						package.ToRadioList(
								RadioListSetup.Create( new[] { "Single", "5-pack", "10-pack" }.Select( i => SelectListItem.Create( i, i ) ) ),
								value: CustomerIsBusiness ? "" : "Single" )
							.ToFormItem() )
					.ToCollection() ) );
	}
}