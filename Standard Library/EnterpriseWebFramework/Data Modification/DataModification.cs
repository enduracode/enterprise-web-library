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
		private readonly List<Action> modificationMethods = new List<Action>();

		/// <summary>
		/// Creates a data modification.
		/// </summary>
		public DataModification( Action<PostBackValueDictionary, Validator> firstTopValidationMethod = null, Action firstModificationMethod = null ) {
			if( firstTopValidationMethod != null )
				AddTopValidationMethod( firstTopValidationMethod );
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
		public void AddTopValidationMethod( Action<PostBackValueDictionary, Validator> validationMethod ) {
			var validation = new EnterpriseWebFramework.Validation( validationMethod, this );
			topValidations.Add( validation );
		}

		/// <summary>
		/// Adds a modification method. These only execute if all validation methods succeed.
		/// </summary>
		public void AddModificationMethod( Action modificationMethod ) {
			modificationMethods.Add( modificationMethod );
		}

		/// <summary>
		/// Adds a list of modification methods. These only execute if all validation methods succeed.
		/// </summary>
		public void AddModificationMethods( IEnumerable<Action> modificationMethods ) {
			this.modificationMethods.AddRange( modificationMethods );
		}

		[ Obsolete( "Guaranteed through 30 September 2013. Please use the overload without the DBConnection parameter." ) ]
		public void AddModificationMethodCn( Action<DBConnection> modificationMethod ) {
			AddModificationMethod( () => modificationMethod( DataAccessState.Current.PrimaryDatabaseConnection ) );
		}

		[ Obsolete( "Guaranteed through 30 September 2013. Please use the overload without the DBConnection parameter." ) ]
		public void AddModificationMethodsCn( IEnumerable<Action<DBConnection>> modificationMethods ) {
			AddModificationMethods( from i in modificationMethods select new Action( () => i( DataAccessState.Current.PrimaryDatabaseConnection ) ) );
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
				method();
		}
	}
}