using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A data set for a chart.
	/// </summary>
	public class ChartDataSet {
		internal readonly string Label;
		internal readonly IEnumerable<double> Values;

		/// <summary>
		/// Constructor for double values.
		/// </summary>
		/// <param name="label">The data set label. Do not pass null.</param>
		/// <param name="values">The Y values.</param>
		public ChartDataSet( string label, IEnumerable<double> values ) {
			Label = label;
			Values = values;
		}

		/// <summary>
		/// Constructor for int values.
		/// </summary>
		/// <param name="label">The data set label. Do not pass null.</param>
		/// <param name="values">The Y values.</param>
		public ChartDataSet( string label, IEnumerable<int> values ): this( label, values.Select( i => (double)i ) ) {}
	}
}