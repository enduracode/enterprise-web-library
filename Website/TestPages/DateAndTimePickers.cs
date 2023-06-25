// EwlPage

namespace EnterpriseWebLibrary.Website.TestPages;

partial class DateAndTimePickers {
	protected override string getResourceName() => "Date/Time Controls";

	protected override PageContent getContent() {
		var list = FormItemList.CreateStack();

		var datePmv = new PageModificationValue<string>();
		list.AddItem(
			new DateControl( null, true, setup: DateControlSetup.Create( pageModificationValue: datePmv ), validationMethod: ( _, _ ) => {} ).ToFormItem(
				label: "Date control".ToComponents()
					.Append( new LineBreak() )
					.Append(
						new SideComments(
							"Value: ".ToComponents().Concat( datePmv.ToGenericPhrasingContainer( v => v, valueExpression => valueExpression ) ).Materialize() ) )
					.Materialize() ) );

		list.AddFormItems(
			new TimeControl( null, true, validationMethod: ( _, _ ) => {} ).ToFormItem( label: "Time control".ToComponents() ),
			new TimeControl( null, true, minuteInterval: 30, validationMethod: ( _, _ ) => {} ).ToFormItem( label: "Drop-down time control".ToComponents() ),
			new DurationControl( null, true, validationMethod: ( _, _ ) => {} ).ToFormItem( label: "Duration control".ToComponents() ) );
		return new UiPageContent( isAutoDataUpdater: true ).Add( list );
	}
}