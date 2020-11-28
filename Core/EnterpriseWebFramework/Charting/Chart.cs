using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Humanizer;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tewl;
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
			var yAxisTicksCallbackProperty = setup.YAxisLabelFormatOptions != null
				                                 ? new JProperty(
					                                 "callback",
					                                 new JRaw(
						                                 "function( value, index, values ) {{ return new Intl.NumberFormat( '{0}', {1} ).format( value ); }}".FormatWith(
							                                 Cultures.EnglishUnitedStates.Name,
							                                 setup.YAxisLabelFormatOptions.ToString( Formatting.None ) ) ) ).ToCollection()
				                                 : Enumerable.Empty<JProperty>();
			JObject options;
			switch( setup.ChartType ) {
				case ChartType.Line:
					chartType = "line";
					datasetSelector = ( set, color ) => new JObject(
						new JProperty( "label", set.Label ),
						new JProperty( "data", new JArray( set.Values.TakeLast( setup.MaxXValues ) ) ),
						new JProperty( "pointBackgroundColor", toRgbaString( color, "1" ) ),
						new JProperty( "backgroundColor", toRgbaString( color, "0.25" ) ),
						new JProperty( "borderColor", toRgbaString( color, "1" ) ) );
					options = new JObject(
						new JProperty( "aspectRatio", setup.AspectRatio ),
						new JProperty( "legend", new JObject( new JProperty( "display", dataSets.Count > 1 ) ) ),
						new JProperty(
							"scales",
							new JObject(
								new JProperty(
									"xAxes",
									new JArray(
										new JObject(
											new JProperty(
												"scaleLabel",
												new JObject( new JProperty( "display", setup.XAxisTitle.Any() ), new JProperty( "labelString", setup.XAxisTitle ) ) ) ) ) ),
								new JProperty(
									"yAxes",
									new JArray(
										new JObject(
											new JProperty(
												"scaleLabel",
												new JObject( new JProperty( "display", setup.YAxisTitle.Any() ), new JProperty( "labelString", setup.YAxisTitle ) ) ),
											new JProperty(
												"ticks",
												new JObject( new JProperty( "beginAtZero", true ).ToCollection().Concat( yAxisTicksCallbackProperty ) ) ) ) ) ) ) ) );
					break;
				case ChartType.Bar:
				case ChartType.StackedBar:
				case ChartType.HorizontalBar:
				case ChartType.HorizontalStackedBar:
					var horizontal = setup.ChartType == ChartType.HorizontalBar || setup.ChartType == ChartType.HorizontalStackedBar;
					chartType = horizontal ? "horizontalBar" : "bar";

					var stacked = setup.ChartType == ChartType.StackedBar || setup.ChartType == ChartType.HorizontalStackedBar;
					datasetSelector = ( set, color ) => new JObject(
						new JProperty( "label", set.Label ).ToCollection()
							.Append( new JProperty( "data", new JArray( set.Values.TakeLast( setup.MaxXValues ) ) ) )
							.Append( new JProperty( "backgroundColor", toRgbaString( color, "1" ) ) )
							.Concat( stacked ? new JProperty( "stack", set.StackedGroupName ).ToCollection() : Enumerable.Empty<JProperty>() ) );

					var xAxis = new JObject(
						new JProperty( "stacked", stacked ),
						new JProperty(
							"scaleLabel",
							new JObject( new JProperty( "display", setup.XAxisTitle.Any() ), new JProperty( "labelString", setup.XAxisTitle ) ) ) );
					var yAxis = new JObject(
						new JProperty( "stacked", stacked ),
						new JProperty( "scaleLabel", new JObject( new JProperty( "display", setup.YAxisTitle.Any() ), new JProperty( "labelString", setup.YAxisTitle ) ) ),
						new JProperty( "ticks", new JObject( new JProperty( "beginAtZero", true ).ToCollection().Concat( yAxisTicksCallbackProperty ) ) ) );
					options = new JObject(
						new JProperty( "aspectRatio", setup.AspectRatio ),
						new JProperty( "legend", new JObject( new JProperty( "display", dataSets.Count > 1 ) ) ),
						new JProperty(
							"scales",
							new JObject(
								new JProperty( "xAxes", new JArray( horizontal ? yAxis : xAxis ) ),
								new JProperty( "yAxes", new JArray( horizontal ? xAxis : yAxis ) ) ) ) );

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

			var table = setup.OmitTable
				            ? Enumerable.Empty<FlowComponent>()
				            : new FlowCheckbox(
						            false,
						            "Show underlying data".ToComponents(),
						            setup: FlowCheckboxSetup.Create(
							            nestedContentGetter: () =>
								            ColumnPrimaryTable.Create( postBackIdBase: setup.PostBackIdBase, allowExportToExcel: true, firstDataFieldIndex: 1 )
									            .AddItems(
										            ( setup.XAxisTitle.Any() || setup.Labels.Any( i => i.Any() )
											              ? EwfTableItem.Create( setup.XAxisTitle.ToCollection().Concat( setup.Labels ).Select( i => i.ToCell() ).Materialize() )
												              .ToCollection()
											              : Enumerable.Empty<EwfTableItem>() ).Concat(
											            dataSets.Select(
												            dataSet => EwfTableItem.Create(
													            dataSet.Label.ToCell().Concat( from i in dataSet.Values select i.ToString().ToCell() ).Materialize() ) ) )
										            .Materialize() )
									            .ToCollection() ) ).ToFormItem()
					            .ToComponentCollection();

			children = new GenericFlowContainer( canvas.Concat( table ).Materialize(), classes: elementClass ).ToCollection();
		}

		private IEnumerable<Color> getDefaultColors() {
			yield return Color.FromArgb( 69, 110, 232 );
			yield return Color.FromArgb( 66, 129, 65 );
			yield return Color.FromArgb( 135, 84, 176 );
			yield return Color.FromArgb( 109, 123, 20 );
			yield return Color.FromArgb( 42, 107, 172 );
			yield return Color.FromArgb( 179, 89, 0 );
			yield return Color.FromArgb( 76, 125, 126 );
			yield return Color.FromArgb( 224, 46, 46 );
			yield return Color.FromArgb( 4, 129, 102 );
			yield return Color.FromArgb( 199, 80, 41 );
			yield return Color.FromArgb( 89, 96, 204 );
			yield return Color.FromArgb( 218, 43, 86 );
			yield return Color.FromArgb( 106, 74, 156 );
			yield return Color.FromArgb( 75, 101, 18 );
			yield return Color.FromArgb( 10, 127, 163 );
			yield return Color.FromArgb( 142, 113, 26 );
			yield return Color.FromArgb( 42, 108, 131 );
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
	}
}