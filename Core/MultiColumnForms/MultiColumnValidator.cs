using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.InputValidation;
using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.MultiColumnForms {

	/// <summary>
	/// Makes it easier to validate form values on pages with multiple columns containing identical fields.
	/// </summary>
	public class MultiColumnValidator<TColumnMod> {
		private TColumnMod[] mods;
		private string[] subjects;

		/// <summary>
		/// Creates a multi-column validator. The mods array should contain one modification object for each column you would like to validate. The subjects array should contain
		/// the error message subject for each column. Since you may not want to validate all columns, the subjects array may contain more elements than the mods array.
		/// </summary>
		public MultiColumnValidator( TColumnMod[] mods, string[] subjects ) {
			this.mods = mods;
			this.subjects = subjects;
		}

		/// <summary>
		/// Validates form values in multiple cells using the specified fields array and cell validation method. The fields array must contain at least as many elements as the mods
		/// array passed to the constructor. If a check box is passed and it is unchecked, the first field will be used for all cell validations.
		/// </summary>
		public void ValidateRow<TColumnField>( TColumnField[] fields, CellValidationMethod<TColumnMod, TColumnField> cellValidationMethod, CheckBox fieldValuesDifferent ) {
			for( int i = 0; i < mods.Length; i += 1 )
				if( fieldValuesDifferent == null || fieldValuesDifferent.Checked )
					cellValidationMethod( mods[ i ], fields[ i ], subjects[ i ] );
				else
					cellValidationMethod( mods[ i ], fields[ 0 ], subjects[ i ] );
		}

	}

}