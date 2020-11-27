using System.Collections.Generic;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a chart.
	/// </summary>
	public class ChartSetup {
		internal readonly string PostBackIdBase;
		internal readonly ChartType ChartType;
		internal readonly double AspectRatio;

		[ NotNull ]
		internal readonly string XAxisTitle;

		[ NotNull ]
		internal readonly IEnumerable<string> Labels;

		internal readonly int MaxXValues;
		internal readonly string YAxisTitle;
		internal readonly bool OmitTable;

		/// <summary>
		/// Creates a chart setup object.
		/// </summary>
		/// <param name="chartType"></param>
		/// <param name="aspectRatio">The aspect ratio (width divided by height) of the chart canvas.</param>
		/// <param name="labels">The labels for the X axis. There must be exactly as many elements as there are in each data set.</param>
		/// <param name="postBackIdBase">Do not pass null.</param>
		/// <param name="xAxisTitle">The title of the X axis. Do not pass null.</param>
		/// <param name="maxXValues">The number of values to display on the x axis. This menas only the last <paramref name="maxXValues"/> values are displayed.
		/// </param>
		/// <param name="yAxisTitle">The title of the Y axis. Do not pass null.</param>
		/// <param name="omitTable">Pass true to omit the table containing the chart’s underlying data.</param>
		public ChartSetup(
			ChartType chartType, double aspectRatio, IEnumerable<string> labels, string postBackIdBase = "", string xAxisTitle = "", int maxXValues = 16,
			string yAxisTitle = "", bool omitTable = false ) {
			PostBackIdBase = PostBack.GetCompositeId( "ewfChart", postBackIdBase );
			ChartType = chartType;
			AspectRatio = aspectRatio;
			XAxisTitle = xAxisTitle;
			Labels = labels;
			MaxXValues = maxXValues;
			YAxisTitle = yAxisTitle;
			OmitTable = omitTable;
		}
	}
}