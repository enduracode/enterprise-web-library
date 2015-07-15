using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.InputValidation;
using System;

namespace EnterpriseWebLibrary.MultiColumnForms {

	/// <summary>
	/// A method used to load data into a single cell on a multiple column form.
	/// </summary>
	public delegate void CellLoadMethod<TColumnData, TColumnField>( TColumnData data, TColumnField field );

}