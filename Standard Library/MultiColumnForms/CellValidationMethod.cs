using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.Validation;
using System;

namespace RedStapler.StandardLibrary.MultiColumnForms {

	/// <summary>
	/// A method used to validate form values in a single cell on a multiple-column form.
	/// </summary>
	public delegate void CellValidationMethod<TColumnMod, TColumnField>( TColumnMod mod, TColumnField field, string subject );

}