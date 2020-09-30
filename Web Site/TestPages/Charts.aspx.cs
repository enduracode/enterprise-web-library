using System;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using Tewl.Tools;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class Charts: EwfPage {
		partial class Info {
			public override string ResourceName => "Chart";
		}

		protected override void loadData() {
			ph.AddControlsReturnThis(
				new Chart(
						new ChartSetup(
							ChartType.Line,
							"This is the labels title",
							new[] { "Three", "One", "Two", "Four", "Five" },
							"The export name",
							postBackIdBase: "1" ),
						new DataSeries( "Title", new[] { 3, 1, 2, 4, 5 } ) ).ToCollection()
					.GetControls() );

			var random = new Random();
			var floatData = Enumerable.Range( 0, 20 ).Select( i => random.NextDouble() * 100 ).ToArray();

			ph.AddControlsReturnThis(
				new Chart(
						new ChartSetup( ChartType.Line, "Floating point numbers", floatData.Select( ( f, i ) => "" + i ), "The export name", postBackIdBase: "2" ),
						new DataSeries( "The value", floatData ) ).ToCollection()
					.GetControls() );

			var floatData1 = Enumerable.Range( 0, 20 ).Select( i => random.NextDouble() * 100 ).ToArray();
			var floatData2 = Enumerable.Range( 0, 20 ).Select( i => random.NextDouble() * 100 ).ToArray();

			ph.AddControlsReturnThis(
				new Chart(
						new ChartSetup(
							ChartType.Line,
							"Two lines of Floating point numbers",
							floatData1.Select( ( f, i ) => "" + i ),
							"The export name",
							postBackIdBase: "3" ),
						new[] { new DataSeries( "First values ", floatData1 ), new DataSeries( "Second values ", floatData2 ) } ).ToCollection()
					.GetControls() );

			var floatData3 = Enumerable.Range( 0, 10 ).Select( i => random.NextDouble() * 50 ).ToArray();

			ph.AddControlsReturnThis(
				new Chart(
						new ChartSetup( ChartType.Bar, "Bar graph", floatData3.Select( ( f, i ) => "" + i ), "The export name", postBackIdBase: "4" ),
						new DataSeries( "Values", floatData3 ) ).ToCollection()
					.GetControls() );


			var floatData4 = Enumerable.Range( 0, 20 ).Select( i => random.NextDouble() * 100 ).ToArray();
			var floatData5 = Enumerable.Range( 0, 20 ).Select( i => random.NextDouble() * 100 ).ToArray();

			ph.AddControlsReturnThis(
				new Chart(
						new ChartSetup(
							ChartType.Bar,
							"Two bars of Floating point numbers",
							floatData4.Select( ( f, i ) => "" + i ),
							"The export name",
							postBackIdBase: "5" ),
						new[] { new DataSeries( "First values ", floatData4 ), new DataSeries( "Second values ", floatData5 ) } ).ToCollection()
					.GetControls() );
		}
	}
}