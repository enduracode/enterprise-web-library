#nullable disable
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

[ PublicAPI ]
public class FormState {
	private static Func<FormState> stateGetter;
	private static Action<IReadOnlyCollection<DataModification>> dataModificationAsserter;
	private static Func<DataModification, PostBack> postBackSelector;

	internal static void Init(
		Func<FormState> formStateGetter, Action<IReadOnlyCollection<DataModification>> dataModificationAsserter,
		Func<DataModification, PostBack> postBackSelector ) {
		stateGetter = formStateGetter;
		FormState.dataModificationAsserter = dataModificationAsserter;
		FormState.postBackSelector = postBackSelector;
	}

	/// <summary>
	/// Gets the current form state. This will throw an exception when called from the worker threads used by parallel programming tools such as PLINQ and the
	/// Task Parallel Library, since we want to avoid the race conditions that would result from multiple threads creating validations.
	/// </summary>
	public static FormState Current {
		get {
			var state = stateGetter();
			if( state == null )
				throw new ApplicationException( "No form state exists at this time." );
			return state;
		}
	}

	/// <summary>
	/// Executes a method with the specified data-modification actions used for any validations that are created, and with a default action available to form
	/// controls and buttons.
	/// </summary>
	/// <param name="dataModificationActions"></param>
	/// <param name="method"></param>
	/// <param name="defaultActionOverride">The default action. Pass null to use the post-back corresponding to the first of the data modifications.</param>
	/// <param name="formControlDefaultActionOverride">The form-control-specific default action. Pass null to use the same action for both form controls and
	/// buttons.</param>
	public static void ExecuteWithActions(
		DataModificationsParameter dataModificationActions, Action method, NonPostBackFormAction defaultActionOverride = null,
		SpecifiedValue<NonPostBackFormAction> formControlDefaultActionOverride = null ) {
		IReadOnlyCollection<DataModification> dmCollection = dataModificationActions.GetCollection();
		if( dmCollection.Count == 0 )
			throw new ApplicationException( "There must be at least one data modification." );
		dataModificationAsserter( dmCollection );

		Current.stack.Push( ( defaultActionOverride, formControlDefaultActionOverride, new Stack<Func<bool>>(), dmCollection ) );
		try {
			method();
		}
		finally {
			Current.stack.Pop();
		}
	}

	/// <summary>
	/// Executes a method with the specified data-modification actions used for any validations that are created, and with a default action available to form
	/// controls and buttons.
	/// </summary>
	/// <param name="dataModificationActions"></param>
	/// <param name="method"></param>
	/// <param name="defaultActionOverride">The default action. Pass null to use the post-back corresponding to the first of the data modifications.</param>
	/// <param name="formControlDefaultActionOverride">The form-control-specific default action. Pass null to use the same action for both form controls and
	/// buttons.</param>
	public static T ExecuteWithActions<T>(
		DataModificationsParameter dataModificationActions, Func<T> method, NonPostBackFormAction defaultActionOverride = null,
		SpecifiedValue<NonPostBackFormAction> formControlDefaultActionOverride = null ) {
		IReadOnlyCollection<DataModification> dmCollection = dataModificationActions.GetCollection();
		if( dmCollection.Count == 0 )
			throw new ApplicationException( "There must be at least one data modification." );
		dataModificationAsserter( dmCollection );

		Current.stack.Push( ( defaultActionOverride, formControlDefaultActionOverride, new Stack<Func<bool>>(), dmCollection ) );
		try {
			return method();
		}
		finally {
			Current.stack.Pop();
		}
	}

	/// <summary>
	/// Executes a method with the specified predicate used for any validations that are created.
	/// </summary>
	public static void ExecuteWithValidationPredicate( Func<bool> validationPredicate, Action method ) {
		Current.validationPredicateStack.Push( validationPredicate );
		try {
			method();
		}
		finally {
			Current.validationPredicateStack.Pop();
		}
	}

	/// <summary>
	/// Executes a method with the specified predicate used for any validations that are created.
	/// </summary>
	public static T ExecuteWithValidationPredicate<T>( Func<bool> validationPredicate, Func<T> method ) {
		Current.validationPredicateStack.Push( validationPredicate );
		try {
			return method();
		}
		finally {
			Current.validationPredicateStack.Pop();
		}
	}

	private readonly Stack<( NonPostBackFormAction actionOverride, SpecifiedValue<NonPostBackFormAction> formControlActionOverride, Stack<Func<bool>>
		validationPredicateStack, IReadOnlyCollection<DataModification> dataModifications )> stack = new();

	/// <summary>
	/// EwfPage and private use only.
	/// </summary>
	internal readonly HashSet<DataModification> DataModificationsWithValidationsFromOtherElements = new();

	private readonly HashSet<DataModification> dataModificationsWithValidations = new();

	internal FormState() {}

	/// <summary>
	/// Gets the current default action.
	/// </summary>
	public FormAction DefaultAction => actionOverride ?? (FormAction)new PostBackFormAction( PostBack );

	/// <summary>
	/// Gets the current form-control-specific default action. Returns null for no action.
	/// </summary>
	public FormAction FormControlDefaultAction => formControlActionOverride != null ? formControlActionOverride.Value : DefaultAction;

	/// <summary>
	/// Gets the post-back corresponding to the first of the current data modifications.
	/// </summary>
	public PostBack PostBack => postBackSelector( DataModifications.First() );

	private NonPostBackFormAction actionOverride => stack.Peek().actionOverride;
	private SpecifiedValue<NonPostBackFormAction> formControlActionOverride => stack.Peek().formControlActionOverride;

	/// <summary>
	/// EwfPage use only.
	/// </summary>
	internal Func<bool> ValidationPredicate {
		get {
			var predicates = validationPredicateStack.Reverse().ToImmutableArray();
			return () => {
				foreach( var predicate in predicates )
					if( !predicate() )
						return false;
				return true;
			};
		}
	}

	private Stack<Func<bool>> validationPredicateStack => stack.Peek().validationPredicateStack;

	/// <summary>
	/// Gets the current data modifications.
	/// </summary>
	public IReadOnlyCollection<DataModification> DataModifications => stack.Peek().dataModifications;

	/// <summary>
	/// EwfPage use only.
	/// </summary>
	internal void ReportValidationCreated() {
		dataModificationsWithValidations.UnionWith( DataModifications );
	}

	/// <summary>
	/// EwfPage use only.
	/// </summary>
	internal void SetForNextElement() {
		DataModificationsWithValidationsFromOtherElements.UnionWith( dataModificationsWithValidations );
		dataModificationsWithValidations.Clear();
	}
}