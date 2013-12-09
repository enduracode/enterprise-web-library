using System;
using System.Collections.Generic;
using System.Linq;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
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

		internal bool Execute( bool skipIfNoChanges, bool formValuesChanged, Action<Validation, IEnumerable<string>> validationErrorHandler,
		                       bool performValidationOnly = false, Action additionalMethod = null ) {
			var skipModification = ( !validations.Any() && ( performValidationOnly || !modificationMethods.Any() ) ) || ( skipIfNoChanges && !formValuesChanged );
			var skipAdditionalMethod = performValidationOnly || additionalMethod == null;
			if( skipModification && skipAdditionalMethod )
				return false;

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
				throw new EwfException( Translation.PleaseCorrectTheErrorsShownBelow.ToSingleElementArray().Concat( topValidator.ErrorMessages ).ToArray() );

			DataAccessState.Current.DisableCache();
			try {
				foreach( var method in modificationMethods )
					method();
				if( additionalMethod != null )
					additionalMethod();
			}
			catch( Exception e ) {
				if( e.GetChain().OfType<EwfException>().FirstOrDefault() != null )
					AppRequestState.Instance.RollbackDatabaseTransactions();
				throw;
			}
			finally {
				DataAccessState.Current.ResetCache();
			}

			return true;
		}
	}
}