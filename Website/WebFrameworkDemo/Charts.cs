// EwlPage

namespace EnterpriseWebLibrary.Website.TestPages;

partial class Charts {
	protected override string getResourceName() => "Chart";

	protected override PageContent getContent() {
		var content = new UiPageContent( omitContentBox: true );

		content.Add(
			getChart(
				"Line",
				new Chart(
					new ChartSetup( ChartType.Line, 4, new[] { "Three", "One", "Two", "Four", "Five" }, postBackIdBase: "1" ),
					new ChartDataSet( "Value", new[] { 3, 1, 2, 4, 5 } ) ) ) );

		var random = new Random();
		var floatData = Enumerable.Range( 0, 20 ).Select( i => random.NextDouble() * 100 ).ToArray();

		content.Add(
			getChart(
				"Line of floating point numbers",
				new Chart(
					new ChartSetup( ChartType.Line, 4, floatData.Select( ( f, i ) => "" + i ), postBackIdBase: "2" ),
					new ChartDataSet( "Value", floatData ) ) ) );

		var floatData1 = Enumerable.Range( 0, 20 ).Select( i => random.NextDouble() * 100 ).ToArray();
		var floatData2 = Enumerable.Range( 0, 20 ).Select( i => random.NextDouble() * 100 ).ToArray();

		content.Add(
			getChart(
				"Two lines of floating point numbers",
				new Chart(
					new ChartSetup( ChartType.Line, 4, floatData1.Select( ( f, i ) => "" + i ), postBackIdBase: "3" ),
					new[] { new ChartDataSet( "First value", floatData1 ), new ChartDataSet( "Second value", floatData2 ) } ) ) );

		var floatData3 = Enumerable.Range( 0, 10 ).Select( i => random.NextDouble() * 50 ).ToArray();

		content.Add(
			getChart(
				"Bar",
				new Chart(
					new ChartSetup( ChartType.Bar, 4, floatData3.Select( ( f, i ) => "" + i ), postBackIdBase: "4" ),
					new ChartDataSet( "Value", floatData3 ) ) ) );


		var floatData4 = Enumerable.Range( 0, 20 ).Select( i => random.NextDouble() * 100 ).ToArray();
		var floatData5 = Enumerable.Range( 0, 20 ).Select( i => random.NextDouble() * 100 ).ToArray();

		content.Add(
			getChart(
				"Two bars of floating point numbers",
				new Chart(
					new ChartSetup( ChartType.Bar, 4, floatData4.Select( ( f, i ) => "" + i ), postBackIdBase: "5" ),
					new[] { new ChartDataSet( "First value", floatData4 ), new ChartDataSet( "Second value", floatData5 ) } ) ) );

		return content;
	}

	private FlowComponent getChart( string title, Chart chart ) => new Section( title, chart.ToCollection(), style: SectionStyle.Box );
}