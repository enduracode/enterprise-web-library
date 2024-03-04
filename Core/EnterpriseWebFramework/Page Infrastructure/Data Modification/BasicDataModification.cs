using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.EnterpriseWebFramework.PageInfrastructure;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal class BasicDataModification: DataModification, ValidationList {
	private static Action slowExecutionNotifier = null!;
	private static Action<EwfValidation, IEnumerable<string>> validationErrorHandler = null!;
	private static Action<IReadOnlyCollection<TrustedHtmlString>> modificationErrorHandler = null!;

	internal static void Init(
		Action slowExecutionNotifier, Action<EwfValidation, IEnumerable<string>> validationErrorHandler,
		Action<IReadOnlyCollection<TrustedHtmlString>> modificationErrorHandler ) {
		BasicDataModification.slowExecutionNotifier = slowExecutionNotifier;
		BasicDataModification.validationErrorHandler = validationErrorHandler;
		BasicDataModification.modificationErrorHandler = modificationErrorHandler;
	}

	private readonly bool isSlow;
	private readonly List<EwfValidation> validations = new();
	private Action? modificationMethod;

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
		if( this.modificationMethod is not null )
			throw new ApplicationException( "The modification method was already added." );
		this.modificationMethod = modificationMethod;
	}

	/// <summary>
	/// Returns whether anything executed.
	/// </summary>
	internal bool Execute(
		bool skipIfNoChanges, bool changesExist, bool performValidationOnly = false, Tuple<Action, Action>? actionMethodAndPostModificationMethod = null ) {
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
						throw new Exception( "Validation errors occurred but there are no messages." );
				}
				validationErrorHandler( validation, validator.ErrorMessages );
			}
			if( errorsOccurred )
				return true;
		}

		var skipModification = modificationMethod is null || ( skipIfNoChanges && !changesExist );
		if( performValidationOnly || ( skipModification && actionMethodAndPostModificationMethod == null ) )
			return validationNeeded;

		if( isSlow )
			slowExecutionNotifier();

		var modificationResponseCookieIndex = CookieStatics.ResponseCookies.Count;

		AutomaticDatabaseConnectionManager.Current.EnableModifications();
		DataAccessState.Current.DisableCache();

		try {
			if( !skipModification )
				modificationMethod!();
			actionMethodAndPostModificationMethod?.Item1();
			DataAccessState.Current.ResetCache();
			AutomaticDatabaseConnectionManager.Current.PreExecuteCommitTimeValidationMethods();
			actionMethodAndPostModificationMethod?.Item2();
		}
		catch( Exception e ) {
			AutomaticDatabaseConnectionManager.Current.RollbackModifications();
			DataAccessState.Current.ResetCache();

			CookieStatics.RemoveResponseCookies( modificationResponseCookieIndex );
			RequestStateStatics.RefreshRequestState();

			var dmException = e.GetChain().OfType<DataModificationException>().FirstOrDefault();
			if( dmException is null )
				throw;

			if( dmException.ModificationMethod is not null )
				AutomaticDatabaseConnectionManager.Current.ExecuteWithModificationsEnabled( dmException.ModificationMethod );
			modificationErrorHandler( dmException.HtmlMessages );
			return true;
		}

		AutomaticDatabaseConnectionManager.Current.CommitModifications();

		return true;
	}
}