using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Humanizer;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A component capable of displaying chart data. Currently implemented with Chart.js.
	/// </summary>
	public sealed class Chart: FlowComponent {
		private static readonly ElementClass elementClass = new ElementClass( "ewfChart" );

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
				new CssElement( "Chart", "div.{0}".FormatWith( elementClass.ClassName ) ).ToCollection();
		}

		private readonly IReadOnlyCollection<FlowComponent> children;

		/// <summary>
		/// Creates a chart displaying a supported <see cref="ChartType"/> with the given data. Includes a chart and a table, and allows exporting the data to
		/// Excel.
		/// </summary>
		/// <param name="setup">The setup object for the chart.</param>
		/// <param name="dataSet">The data set.</param>
		/// <param name="color">The color to use for the data set.</param>
		public Chart( ChartSetup setup, ChartDataSet dataSet, Color? color = null ): this( setup, dataSet.ToCollection(), colors: color?.ToCollection() ) {}

		/// <summary>
		/// Creates a chart displaying a supported <see cref="ChartType"/> with the given data. Includes a chart and a table, and allows exporting the data to
		/// Excel. Assuming <paramref name="dataSets"/> has multiple elements, draws multiple sets of Y values on the same chart.
		/// </summary>
		/// <param name="setup">The setup object for the chart.</param>
		/// <param name="dataSets">The data sets.</param>
		/// <param name="colors">The colors to use for the data sets. Pass null for default colors. If you specify your own colors, the number of colors does not
		/// need to match the number of data sets. If you pass fewer colors than data sets, the chart will use random colors for the remaining data sets.</param>
		public Chart( ChartSetup setup, [ NotNull ] IReadOnlyCollection<ChartDataSet> dataSets, IEnumerable<Color> colors = null ) {
			var rand = new Random();
			colors = ( colors ?? getDefaultColors() ).Take( dataSets.Count )
				.Pad( dataSets.Count, () => Color.FromArgb( rand.Next( 256 ), rand.Next( 256 ), rand.Next( 256 ) ) );

			string chartType;
			string toRgbaString( Color color, string opacity ) => "rgba( {0}, {1}, {2}, {3} )".FormatWith( color.R, color.G, color.B, opacity );
			Func<ChartDataSet, Color, JObject> datasetSelector;
			JObject options;
			switch( setup.ChartType ) {
				case ChartType.Line:
					chartType = "line";
					datasetSelector = ( set, color ) => new JObject(
						new JProperty( "label", set.Label ),
						new JProperty( "data", new JArray( set.Values.TakeLast( setup.MaxXValues ) ) ),
						new JProperty( "backgroundColor", toRgbaString( color, "0.5" ) ),
						new JProperty( "borderColor", toRgbaString( color, "1" ) ) );
					options = new JObject(
						new JProperty( "aspectRatio", 2 ),
						new JProperty( "legend", new JObject( new JProperty( "display", dataSets.Count > 1 ) ) ),
						new JProperty(
							"scales",
							new JObject(
								new JProperty( "yAxes", new JArray( new JObject( new JProperty( "ticks", new JObject( new JProperty( "beginAtZero", true ) ) ) ) ) ) ) ) );
					break;
				case ChartType.Bar:
				case ChartType.StackedBar:
					chartType = "bar";
					datasetSelector = ( set, color ) => new JObject(
						new JProperty( "label", set.Label ).ToCollection()
							.Append( new JProperty( "data", new JArray( set.Values.TakeLast( setup.MaxXValues ) ) ) )
							.Append( new JProperty( "backgroundColor", toRgbaString( color, "1" ) ) )
							.Concat(
								setup.ChartType == ChartType.StackedBar ? new JProperty( "stack", set.StackedGroupName ).ToCollection() : Enumerable.Empty<JProperty>() ) );
					options = new JObject(
						new JProperty( "aspectRatio", 2 ),
						new JProperty( "legend", new JObject( new JProperty( "display", dataSets.Count > 1 ) ) ),
						new JProperty(
							"scales",
							new JObject(
								new JProperty( "xAxes", new JArray( new JObject( new JProperty( "stacked", setup.ChartType == ChartType.StackedBar ) ) ) ),
								new JProperty(
									"yAxes",
									new JArray(
										new JObject(
											new JProperty( "stacked", setup.ChartType == ChartType.StackedBar ),
											new JProperty( "ticks", new JObject( new JProperty( "beginAtZero", true ) ) ) ) ) ) ) ) );
					break;
				default:
					throw new UnexpectedValueException( setup.ChartType );
			}

			var canvas = new GenericFlowContainer(
				new ElementComponent(
					context => new ElementData(
						() => {
							var jsInitStatement = "new Chart( '{0}', {{ type: '{1}', data: {2}, options: {3} }} );".FormatWith(
								context.Id,
								chartType,
								new JObject(
									new JProperty( "labels", new JArray( setup.Labels.TakeLast( setup.MaxXValues ) ) ),
									new JProperty( "datasets", new JArray( dataSets.Zip( colors, ( set, color ) => datasetSelector( set, color ) ) ) ) ).ToString(
									Formatting.None ),
								options.ToString( Formatting.None ) );

							return new ElementLocalData(
								"canvas",
								focusDependentData: new ElementFocusDependentData( includeIdAttribute: true, jsInitStatements: jsInitStatement ) );
						} ) ).ToCollection() );

			var table = ColumnPrimaryTable.Create( postBackIdBase: setup.PostBackIdBase, allowExportToExcel: true, firstDataFieldIndex: 1 )
				.AddItems(
					EwfTableItem.Create( setup.XAxisTitle.ToCollection().Concat( setup.Labels ).Select( i => i.ToCell() ).Materialize() )
						.ToCollection()
						.Concat(
							from dataSet in dataSets
							select EwfTableItem.Create( dataSet.Label.ToCell().Concat( from i in dataSet.Values select i.ToString().ToCell() ).Materialize() ) )
						.Materialize() );

			children = new GenericFlowContainer( canvas.Append<FlowComponent>( table ).Materialize(), classes: elementClass ).ToCollection();
		}

		private IEnumerable<Color> getDefaultColors() {
			yield return Color.FromArgb( 120, 160, 195 );
			yield return Color.FromArgb( 255, 182, 149 );
			yield return Color.FromArgb( 170, 225, 149 );
			yield return Color.FromArgb( 255, 230, 149 );
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
	}
}