// EwlPage

namespace EnterpriseWebLibrary.Website.TestPages;

partial class ResponsiveTableDemo {
	protected override string getResourceName() => "Responsive Table";

	protected override PageContent getContent() =>
		new UiPageContent().Add(
			ResponsiveDataTable
				.Create(
					style: EwfTableStyle.StandardExceptLayout,
					caption: "Caption",
					subCaption: "Sub caption",
					allowExportToExcel: true,
					fields: new EwfTableField( size: 1.ToPercentage() ).Append( new EwfTableField( size: 2.ToPercentage() ) )
						.Concat( Enumerable.Repeat( new EwfTableField( size: 3.ToPercentage() ), 3 ) )
						.Materialize(),
					headItems: EwfTableItem.Create(
							"First Column".ToCell(),
							"Second Column".ToCell(),
							"Third Column".ToCell(),
							"Fourth Column".ToCell(),
							"Fifth Column".ToCell() )
						.ToCollection(),
					defaultItemLimit: DataRowLimit.Fifty )
				.AddData(
					Enumerable.Range( 1, 250 ),
					i => EwfTableItem.Create(
						EwfTableItemSetup.Create( activationBehavior: ElementActivationBehavior.CreateHyperlink( ActionControls.GetInfo() ) ),
						i.ToString().ToCell(),
						( i * 2 + Environment.NewLine + "extra stuff" ).ToCell(),
						"Lorem ipsum dolor sit amet".ToCell(),
						"Lorem ipsum dolor sit amet".ToCell(),
						"Lorem ipsum dolor sit amet".ToCell() ) ) );
}