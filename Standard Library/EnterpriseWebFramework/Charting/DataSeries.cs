using System.Collections.Generic;
using System.Linq;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A data series for a chart.
	/// </summary>
	public class DataSeries {
		public readonly string Title;
		public readonly IEnumerable<double> Values;

		/// <summary>
		/// Constructor for double values.
		/// </summary>
		/// <param name="title">The label for the Y values.</param>
		/// <param name="values">The Y values.</param>
		public DataSeries( string title, IEnumerable<double> values ) {
			Title = title;
			Values = values;
		}

		/// <summary>
		/// Constructor for int values.
		/// </summary>
		/// <param name="title">The label for the Y values.</param>
		/// <param name="values">The Y values.</param>
		public DataSeries( string title, IEnumerable<int> values ): this( title, values.Select( i => (double)i ) ) {}
	}
}