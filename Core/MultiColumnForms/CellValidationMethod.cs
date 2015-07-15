using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.InputValidation;
using System;

namespace EnterpriseWebLibrary.MultiColumnForms {

	/// <summary>
	/// A method used to validate form values in a single cell on a multiple-column form.
	/// </summary>
	public delegate void CellValidationMethod<TColumnMod, TColumnField>( TColumnMod mod, TColumnField field, string subject );

}