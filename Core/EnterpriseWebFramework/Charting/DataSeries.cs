using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A data series for a chart.
	/// </summary>
	public class DataSeries {
		internal readonly string Name;
		internal readonly IEnumerable<double> Values;

		/// <summary>
		/// Constructor for double values.
		/// </summary>
		/// <param name="name">The series name. Do not pass null.</param>
		/// <param name="values">The Y values.</param>
		public DataSeries( string name, IEnumerable<double> values ) {
			Name = name;
			Values = values;
		}

		/// <summary>
		/// Constructor for int values.
		/// </summary>
		/// <param name="name">The series name. Do not pass null.</param>
		/// <param name="values">The Y values.</param>
		public DataSeries( string name, IEnumerable<int> values ): this( name, values.Select( i => (double)i ) ) {}
	}
}