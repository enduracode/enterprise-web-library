using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A data set for a chart.
	/// </summary>
	public class ChartDataSet {
		internal readonly string Label;
		internal readonly string StackedGroupName;
		internal readonly IEnumerable<double> Values;

		/// <summary>
		/// Constructor for double values.
		/// </summary>
		/// <param name="label">The data set label. Do not pass null.</param>
		/// <param name="values">The Y values.</param>
		/// <param name="stackedGroupName">Only applies to <see cref="ChartType.StackedBar"/> and <see cref="ChartType.HorizontalStackedBar"/>. The name of the
		/// group to which this data set belongs. Each group will be a separate stack. Do not pass null.</param>
		public ChartDataSet( string label, IEnumerable<double> values, string stackedGroupName = "" ) {
			Label = label;
			StackedGroupName = stackedGroupName;
			Values = values;
		}

		/// <summary>
		/// Constructor for int values.
		/// </summary>
		/// <param name="label">The data set label. Do not pass null.</param>
		/// <param name="values">The Y values.</param>
		/// <param name="stackedGroupName">Only applies to <see cref="ChartType.StackedBar"/> and <see cref="ChartType.HorizontalStackedBar"/>. The name of the
		/// group to which this data set belongs. Each group will be a separate stack. Do not pass null.</param>
		public ChartDataSet( string label, IEnumerable<int> values, string stackedGroupName = "" ): this(
			label,
			values.Select( i => (double)i ),
			stackedGroupName: stackedGroupName ) {}
	}
}