using System;
using System.Collections.Generic;
using System.Drawing;
using JetBrains.Annotations;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a chart.
	/// </summary>
	public class ChartSetup {
		public readonly int MaxXValues;

		[ NotNull ]
		public readonly string LabelsTitle;

		[ NotNull ]
		public readonly IEnumerable<string> Labels;

		[ NotNull ]
		public readonly IEnumerable<DataSeries> Values;

		[ CanBeNull ]
		public readonly Func<Color> NextColorSelector;

		/// <summary>
		/// The sum of all of the data for the chart.
		/// Assuming <paramref name="values"/> has multiple elements, draws multiple sets of Y values on the same chart.
		/// </summary>
		/// <param name="labelsTitle">The label for the X values.</param>
		/// <param name="labels">The labels for the X axis. There must be exactly as many elements as there are in each of the elements of <paramref name="values"/></param>.
		/// <param name="values">Y values.</param>
		/// <param name="nextColorSelector">When set, returns the next color used to be used for the current <see cref="DataSeries"/></param>.
		/// /// <param name="maxXValues">The amount of values to display on the x axis. This menas only the last <paramref name="maxXValues"/> values are displayed.</param>
		public ChartSetup( string labelsTitle, IEnumerable<string> labels, IEnumerable<DataSeries> values, Func<Color> nextColorSelector = null, int maxXValues = 16 ) {
			LabelsTitle = labelsTitle;
			Labels = labels;
			Values = values;
			NextColorSelector = nextColorSelector;
			MaxXValues = maxXValues;
		}

		/// <summary>
		/// The sum of all of the data for the chart.
		/// </summary>
		/// <param name="labelsTitle">The label for the X values.</param>
		/// <param name="labels">The labels for the X axis. There must be exactly as many elements as there are in <paramref name="values"/></param>.
		/// <param name="values">Y values.</param>
		/// <param name="color">The color to use to represent this data.</param>
		/// <param name="maxXValues">The amount of values to display on the x axis. This menas only the last <paramref name="maxXValues"/> values are displayed.</param>
		public ChartSetup( string labelsTitle, IEnumerable<string> labels, DataSeries values, Color? color = null, int maxXValues = 16 )
			: this( labelsTitle, labels, values.ToSingleElementArray(), color != null ? () => color.Value : (Func<Color>)null, maxXValues: maxXValues ) {}
	}
}