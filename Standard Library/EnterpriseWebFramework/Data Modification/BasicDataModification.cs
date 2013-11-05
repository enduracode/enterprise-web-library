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

		internal void Execute( bool skipIfNoChanges, bool formValuesChanged, Action<Validation, IEnumerable<string>> validationErrorHandler,
		                       Action additionalMethod = null ) {
			if( additionalMethod == null && ( ( !validations.Any() && !modificationMethods.Any() ) || ( skipIfNoChanges && !formValuesChanged ) ) )
				return;

			DataAccessState.Current.DisableCache();
			try {
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

				foreach( var method in modificationMethods )
					method();

				if( additionalMethod != null )
					additionalMethod();
			}
			finally {
				DataAccessState.Current.ResetCache();
			}
		}
	}
}