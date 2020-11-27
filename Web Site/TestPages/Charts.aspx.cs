using System;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using Tewl.Tools;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class Charts: EwfPage {
		partial class Info {
			public override string ResourceName => "Chart";
		}

		protected override void loadData() {
			EwfUiStatics.OmitContentBox();

			ph.AddControlsReturnThis(
				getChart(
						"Line",
						new Chart(
							new ChartSetup( ChartType.Line, 4, new[] { "Three", "One", "Two", "Four", "Five" }, postBackIdBase: "1" ),
							new ChartDataSet( "Value", new[] { 3, 1, 2, 4, 5 } ) ) )
					.ToCollection()
					.GetControls() );

			var random = new Random();
			var floatData = Enumerable.Range( 0, 20 ).Select( i => random.NextDouble() * 100 ).ToArray();

			ph.AddControlsReturnThis(
				getChart(
						"Line of floating point numbers",
						new Chart(
							new ChartSetup( ChartType.Line, 4, floatData.Select( ( f, i ) => "" + i ), postBackIdBase: "2" ),
							new ChartDataSet( "Value", floatData ) ) )
					.ToCollection()
					.GetControls() );

			var floatData1 = Enumerable.Range( 0, 20 ).Select( i => random.NextDouble() * 100 ).ToArray();
			var floatData2 = Enumerable.Range( 0, 20 ).Select( i => random.NextDouble() * 100 ).ToArray();

			ph.AddControlsReturnThis(
				getChart(
						"Two lines of floating point numbers",
						new Chart(
							new ChartSetup( ChartType.Line, 4, floatData1.Select( ( f, i ) => "" + i ), postBackIdBase: "3" ),
							new[] { new ChartDataSet( "First value", floatData1 ), new ChartDataSet( "Second value", floatData2 ) } ) )
					.ToCollection()
					.GetControls() );

			var floatData3 = Enumerable.Range( 0, 10 ).Select( i => random.NextDouble() * 50 ).ToArray();

			ph.AddControlsReturnThis(
				getChart(
						"Bar",
						new Chart(
							new ChartSetup( ChartType.Bar, 4, floatData3.Select( ( f, i ) => "" + i ), postBackIdBase: "4" ),
							new ChartDataSet( "Value", floatData3 ) ) )
					.ToCollection()
					.GetControls() );


			var floatData4 = Enumerable.Range( 0, 20 ).Select( i => random.NextDouble() * 100 ).ToArray();
			var floatData5 = Enumerable.Range( 0, 20 ).Select( i => random.NextDouble() * 100 ).ToArray();

			ph.AddControlsReturnThis(
				getChart(
						"Two bars of floating point numbers",
						new Chart(
							new ChartSetup( ChartType.Bar, 4, floatData4.Select( ( f, i ) => "" + i ), postBackIdBase: "5" ),
							new[] { new ChartDataSet( "First value", floatData4 ), new ChartDataSet( "Second value", floatData5 ) } ) )
					.ToCollection()
					.GetControls() );
		}

		private FlowComponent getChart( string title, Chart chart ) => new Section( title, chart.ToCollection(), style: SectionStyle.Box );
	}
}