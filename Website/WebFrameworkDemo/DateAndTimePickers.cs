﻿using NodaTime;
using NodaTime.Text;

// EwlPage

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo;

partial class DateAndTimePickers {
	protected override string getResourceName() => "Date/Time Controls";

	protected override PageContent getContent() {
		var list = FormItemList.CreateStack();

		var datePmv = new PageModificationValue<LocalDate?>();
		list.AddItem(
			new DateControl( null, true, setup: DateControlSetup.Create( pageModificationValue: datePmv ), validationMethod: ( _, _ ) => {} ).ToFormItem(
				label: "Date control".ToComponents()
					.Append( new LineBreak() )
					.Append(
						new SideComments(
							"Value: ".ToComponents()
								.Concat(
									datePmv.ToGenericPhrasingContainer( v => v.HasValue ? LocalDatePattern.Iso.Format( v.Value ) : "", valueExpression => valueExpression ) )
								.Materialize() ) )
					.Materialize() ) );

		list.AddItem( new TimeControl( null, true, validationMethod: ( _, _ ) => {} ).ToFormItem( label: "Time control".ToComponents() ) )
			.AddItem(
				new TimeControl( null, true, minuteInterval: 30, validationMethod: ( _, _ ) => {} ).ToFormItem( label: "Drop-down time control".ToComponents() ) )
			.AddItem( new DateAndTimeControl( null, true, validationMethod: ( _, _ ) => {} ).ToFormItem( label: "Date and time control".ToComponents() ) )
			.AddItem( new DurationControl( null, true, validationMethod: ( _, _ ) => {} ).ToFormItem( label: "Duration control".ToComponents() ) );
		return new UiPageContent( isAutoDataUpdater: true ).Add( list );
	}
}