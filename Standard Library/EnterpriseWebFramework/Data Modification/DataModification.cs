using System;
using System.Collections.Generic;
using System.Linq;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// A collection of validation methods and a collection of modification methods.
	/// </summary>
	public class DataModification: ValidationList {
		private readonly List<EnterpriseWebFramework.Validation> validations = new List<EnterpriseWebFramework.Validation>();
		private readonly List<EnterpriseWebFramework.Validation> topValidations = new List<EnterpriseWebFramework.Validation>();
		private readonly List<DbMethod> modificationMethods = new List<DbMethod>();

		/// <summary>
		/// Creates a data modification.
		/// </summary>
		public DataModification( Action<Validator> firstValidationMethod = null, DbMethod firstModificationMethod = null ) {
			if( firstValidationMethod != null )
				AddValidationMethod( firstValidationMethod );
			if( firstModificationMethod != null )
				AddModificationMethod( firstModificationMethod );
		}

		internal void AddValidation( EnterpriseWebFramework.Validation validation ) {
			validations.Add( validation );
		}

		/// <summary>
		/// Adds all validations from the specified basic validation list.
		/// </summary>
		public void AddValidations( BasicValidationList validationList ) {
			validations.AddRange( validationList.Validations );
		}

		/// <summary>
		/// Adds a validation method whose errors are displayed at the top of the window.
		/// </summary>
		// NOTE: Rename to AddTopValidationMethod. Also pass PostBackValues to validationMethod.
		public void AddValidationMethod( Action<Validator> validationMethod ) {
			var validation = new EnterpriseWebFramework.Validation( ( pbv, validator ) => validationMethod( validator ), this );
			topValidations.Add( validation );
		}

		/// <summary>
		/// Adds a modification method. These only execute if all validation methods succeed.
		/// </summary>
		public void AddModificationMethod( DbMethod modificationMethod ) {
			modificationMethods.Add( modificationMethod );
		}

		/// <summary>
		/// Adds a list of modification methods. These only execute if all validation methods succeed.
		/// </summary>
		public void AddModificationMethods( IEnumerable<DbMethod> modificationMethods ) {
			this.modificationMethods.AddRange( modificationMethods );
		}

		internal bool ContainsAnyValidationsOrModifications() {
			return validations.Any() || modificationMethods.Any();
		}

		internal void ValidateFormValues( Validator topValidator, Action<EnterpriseWebFramework.Validation, IEnumerable<string>> errorHandler ) {
			foreach( var validation in validations ) {
				if( topValidations.Contains( validation ) )
					validation.Method( AppRequestState.Instance.EwfPageRequestState.PostBackValues, topValidator );
				else {
					var validator = new Validator();
					validation.Method( AppRequestState.Instance.EwfPageRequestState.PostBackValues, validator );
					if( validator.ErrorsOccurred )
						topValidator.NoteError();
					errorHandler( validation, validator.ErrorMessages );
				}
			}
		}

		internal void ModifyData( DBConnection cn ) {
			foreach( var method in modificationMethods )
				method( cn );
		}
	}
}