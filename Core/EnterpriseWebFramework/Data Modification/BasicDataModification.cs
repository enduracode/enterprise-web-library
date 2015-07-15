using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class BasicDataModification: DataModification, ValidationListInternal {
		private readonly List<Validation> validations = new List<Validation>();
		private readonly List<Validation> topValidations = new List<Validation>();
		private readonly List<Action> modificationMethods = new List<Action>();

		void ValidationListInternal.AddValidation( Validation validation ) {
			validations.Add( validation );
		}

		public void AddValidations( BasicValidationList validationList ) {
			validations.AddRange( validationList.Validations );
		}

		public void AddTopValidationMethod( Action<PostBackValueDictionary, Validator> validationMethod ) {
			var validation = new Validation( validationMethod, this );
			topValidations.Add( validation );
		}

		public void AddModificationMethod( Action modificationMethod ) {
			modificationMethods.Add( modificationMethod );
		}

		public void AddModificationMethods( IEnumerable<Action> modificationMethods ) {
			this.modificationMethods.AddRange( modificationMethods );
		}

		internal bool Execute(
			bool skipIfNoChanges, bool formValuesChanged, Action<Validation, IEnumerable<string>> validationErrorHandler, bool performValidationOnly = false,
			Tuple<Action, Action> actionMethodAndPostModificationMethod = null ) {
			var validationNeeded = validations.Any() && ( !skipIfNoChanges || formValuesChanged );
			if( validationNeeded ) {
				var topValidator = new Validator();
				foreach( var validation in validations ) {
					if( topValidations.Contains( validation ) )
						validation.Method( AppRequestState.Instance.EwfPageRequestState.PostBackValues, topValidator );
					else {
						var validator = new Validator();
						validation.Method( AppRequestState.Instance.EwfPageRequestState.PostBackValues, validator );
						if( validator.ErrorsOccurred )
							topValidator.NoteError();
						validationErrorHandler( validation, validator.ErrorMessages );
					}
				}
				if( topValidator.ErrorsOccurred )
					throw new DataModificationException( Translation.PleaseCorrectTheErrorsShownBelow.ToSingleElementArray().Concat( topValidator.ErrorMessages ).ToArray() );
			}

			var skipModification = !modificationMethods.Any() || ( skipIfNoChanges && !formValuesChanged );
			if( performValidationOnly || ( skipModification && actionMethodAndPostModificationMethod == null ) )
				return validationNeeded;

			DataAccessState.Current.DisableCache();
			try {
				if( !skipModification ) {
					foreach( var method in modificationMethods )
						method();
				}
				if( actionMethodAndPostModificationMethod != null )
					actionMethodAndPostModificationMethod.Item1();
				DataAccessState.Current.ResetCache();
				AppRequestState.Instance.PreExecuteCommitTimeValidationMethodsForAllOpenConnections();
				if( actionMethodAndPostModificationMethod != null )
					actionMethodAndPostModificationMethod.Item2();
			}
			catch {
				AppRequestState.Instance.RollbackDatabaseTransactions();
				DataAccessState.Current.ResetCache();
				throw;
			}

			return true;
		}
	}
}