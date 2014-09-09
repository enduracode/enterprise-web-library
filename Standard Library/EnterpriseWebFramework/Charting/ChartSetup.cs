using System.Collections.Generic;
using JetBrains.Annotations;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a chart.
	/// </summary>
	public class ChartSetup {
		internal readonly string PostBackIdBase;
		internal readonly ChartType ChartType;

		[ NotNull ]
		internal readonly string XAxisTitle;

		[ NotNull ]
		internal readonly IEnumerable<string> Labels;

		internal readonly int MaxXValues;
		internal readonly string ExportFileName;

		/// <summary>
		/// Creates a chart setup object.
		/// </summary>
		/// <param name="chartType"></param>
		/// <param name="xAxisTitle">The title of the X axis. Do not pass null.</param>
		/// <param name="labels">The labels for the X axis. There must be exactly as many elements as there are in each series.</param>
		/// <param name="exportFileName">Used to create a meaningful file name when exporting the data.</param>
		/// <param name="postBackIdBase">Do not pass null.</param>
		/// <param name="maxXValues">The number of values to display on the x axis. This menas only the last <paramref name="maxXValues"/> values are displayed.
		/// </param>
		public ChartSetup(
			ChartType chartType, string xAxisTitle, IEnumerable<string> labels, string exportFileName, string postBackIdBase = "", int maxXValues = 16 ) {
			PostBackIdBase = PostBack.GetCompositeId( "ewfChart", postBackIdBase );
			ChartType = chartType;
			XAxisTitle = xAxisTitle;
			Labels = labels;
			MaxXValues = maxXValues;
			ExportFileName = exportFileName;
		}
	}
}