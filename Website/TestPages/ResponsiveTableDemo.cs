// EwlPage

namespace EnterpriseWebLibrary.Website.TestPages;

partial class ResponsiveTableDemo {
	protected override string getResourceName() => "Responsive Table";

	protected override PageContent getContent() =>
		new UiPageContent().Add(
			ResponsiveTable
				.Create(
					caption: "Caption",
					subCaption: "Sub caption",
					allowExportToExcel: true,
					fields: new EwfTableField( size: 1.ToPercentage() ).Append( new EwfTableField( size: 2.ToPercentage() ) )
						.Append( new EwfTableField( size: 3.ToPercentage() ) )
						.Append( new EwfTableField( classes: ElementClasses.LongColumn, size: 3.ToPercentage() ) )
						.Append( new EwfTableField( size: 3.ToPercentage() ) )
						.Materialize(),
					headItems: EwfTableItem.Create(
							"First column".ToCell(),
							"Second column".ToCell(),
							"Third column".ToCell(),
							"Long column".ToCell(),
							"Fifth column".ToCell() )
						.ToCollection(),
					defaultItemLimit: DataRowLimit.Fifty )
				.AddData(
					Enumerable.Range( 1, 250 ),
					i => EwfTableItem.Create(
						EwfTableItemSetup.Create( activationBehavior: ElementActivationBehavior.CreateHyperlink( ActionControls.GetInfo() ) ),
						i.ToString().ToCell(),
						( i * 2 + Environment.NewLine + "extra stuff" ).ToCell(),
						"Lorem ipsum dolor sit amet".ToCell(),
						i < 3
							? "Lorem ipsum dolor sit amet".ToCell()
							: "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam ultricies nibh ipsum, sit amet fermentum arcu feugiat non. Etiam eget libero non erat gravida blandit."
								.ToCell(),
						"Lorem ipsum dolor ".ToComponents().Append( new LineBreakOpportunity() ).Concat( "sit amet".ToComponents() ).Materialize().ToCell() ) ) );
}