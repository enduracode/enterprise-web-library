using System.Web.UI;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.MultiColumnForms {
	/// <summary>
	/// Makes it easier to load data on pages with multiple columns containing identical fields.
	/// </summary>
	public class MultiColumnLoader<TColumnData> {
		private readonly TColumnData[] data;

		/// <summary>
		/// Creates a multi-column loader. The data array should contain one data object for each column you would like to load.
		/// </summary>
		public MultiColumnLoader( TColumnData[] data ) {
			this.data = data;
		}

		/// <summary>
		/// Loads data into multiple cells using the specified fields array and cell load method. If a check box is passed, it will be unchecked if all elements in the fields array
		/// contain the same form data and checked otherwise.
		/// </summary>
		public void LoadRow<TColumnField>( TColumnField[] fields, CellLoadMethod<TColumnData, TColumnField> cellLoadMethod, CheckBox fieldValuesDifferent )
			where TColumnField: Control {
			for( var i = 0; i < data.Length; i += 1 )
				cellLoadMethod( data[ i ], fields[ i ] );
			if( fieldValuesDifferent != null ) {
				fieldValuesDifferent.Checked = false;

				// NOTE: Using a string for this is a poor implementation since each field value isn't properly delimited
				var fieldValues = GetFieldValues( fields[ 0 ] );

				for( var i = 1; i < data.Length; i += 1 ) {
					if( GetFieldValues( fields[ i ] ) != fieldValues ) {
						fieldValuesDifferent.Checked = true;
						break;
					}
				}
			}
		}

		private static string GetFieldValues( Control c ) {
			// NOTE: This implementation may not work.
			// NOTE: This method should be called later in the life cycle after all LoadData methods, when the control tree has been full defined.
			var fieldValues = string.Empty;
			//var valueAccessors = FormControlOps.GetValueAccessorsForFormControl( c );
			//if( valueAccessors == null )
			//  valueAccessors = FormControlOps.GetValueAccessorsForChildFormControls( c );

			//foreach( var valueAccessor in valueAccessors )
			//  fieldValues += ( "DELIM" + valueAccessor.Value );

			return fieldValues;
		}
	}
}