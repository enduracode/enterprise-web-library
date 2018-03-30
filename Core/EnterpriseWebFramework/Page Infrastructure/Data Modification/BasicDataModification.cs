using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class BasicDataModification: DataModification, ValidationList {
		private readonly List<EwfValidation> validations = new List<EwfValidation>();
		private readonly List<Action> modificationMethods = new List<Action>();

		void ValidationList.AddValidation( EwfValidation validation ) {
			validations.Add( validation );
		}

		public void AddModificationMethod( Action modificationMethod ) {
			modificationMethods.Add( modificationMethod );
		}

		public void AddModificationMethods( IEnumerable<Action> modificationMethods ) {
			this.modificationMethods.AddRange( modificationMethods );
		}

		internal bool Execute(
			bool skipIfNoChanges, bool formValuesChanged, Action<EwfValidation, IEnumerable<string>> validationErrorHandler, bool performValidationOnly = false,
			Tuple<Action, Action> actionMethodAndPostModificationMethod = null ) {
			var validationNeeded = validations.Any() && ( !skipIfNoChanges || formValuesChanged );
			if( validationNeeded ) {
				var errorsOccurred = false;
				foreach( var validation in validations ) {
					var validator = new Validator();
					validation.Method( validator );
					if( validator.ErrorsOccurred )
						errorsOccurred = true;
					validationErrorHandler( validation, validator.ErrorMessages );
				}
				if( errorsOccurred )
					throw new DataModificationException();
			}

			var skipModification = !modificationMethods.Any() || ( skipIfNoChanges && !formValuesChanged );
			if( performValidationOnly || ( skipModification && actionMethodAndPostModificationMethod == null ) )
				return validationNeeded;

			DataAccessState.Current.DisableCache();
			try {
				if( !skipModification )
					foreach( var method in modificationMethods )
						method();
				actionMethodAndPostModificationMethod?.Item1();
				DataAccessState.Current.ResetCache();
				AppRequestState.Instance.PreExecuteCommitTimeValidationMethodsForAllOpenConnections();
				actionMethodAndPostModificationMethod?.Item2();
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