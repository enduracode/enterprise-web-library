using EnterpriseWebLibrary.DataAccess;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal class BasicDataModification: DataModification, ValidationList {
	private static Action slowExecutionNotifier;

	internal static void Init( Action slowExecutionNotifier ) {
		BasicDataModification.slowExecutionNotifier = slowExecutionNotifier;
	}

	private readonly bool isSlow;
	private readonly List<EwfValidation> validations = new();
	private Action modificationMethod;

	internal BasicDataModification( bool isSlow ) {
		this.isSlow = isSlow;
	}

	void ValidationList.AddValidation( EwfValidation validation ) {
		validations.Add( validation );
	}

	/// <summary>
	/// Adds the modification method, which only executes if all validations succeed. This can only be called once.
	/// </summary>
	public void AddModificationMethod( Action modificationMethod ) {
		if( this.modificationMethod != null )
			throw new ApplicationException( "The modification method was already added." );
		this.modificationMethod = modificationMethod;
	}

	internal bool Execute(
		bool skipIfNoChanges, bool changesExist, Action<EwfValidation, IEnumerable<string>> validationErrorHandler, bool performValidationOnly = false,
		Tuple<Action, Action> actionMethodAndPostModificationMethod = null ) {
		var validationNeeded = validations.Any() && ( !skipIfNoChanges || changesExist );
		if( validationNeeded ) {
			if( isSlow )
				slowExecutionNotifier();

			var errorsOccurred = false;
			foreach( var validation in validations ) {
				var validator = new Validator();
				validation.Method( validator );
				if( validator.ErrorsOccurred ) {
					errorsOccurred = true;
					if( !validator.ErrorMessages.Any() )
						throw new ApplicationException( "Validation errors occurred but there are no messages." );
				}
				validationErrorHandler( validation, validator.ErrorMessages );
			}
			if( errorsOccurred )
				throw new DataModificationException();
		}

		var skipModification = modificationMethod == null || ( skipIfNoChanges && !changesExist );
		if( performValidationOnly || ( skipModification && actionMethodAndPostModificationMethod == null ) )
			return validationNeeded;

		if( isSlow )
			slowExecutionNotifier();

		DataAccessState.Current.DisableCache();
		try {
			if( !skipModification )
				modificationMethod();
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