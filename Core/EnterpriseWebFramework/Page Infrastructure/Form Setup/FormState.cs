using System.Collections.Immutable;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

[ PublicAPI ]
public class FormState {
	private static Func<FormState> stateGetter = null!;
	private static Action<IReadOnlyCollection<DataModificationAction>> dataModificationAsserter = null!;
	private static Func<DataModificationAction, PostBack> postBackSelector = null!;

	internal static void Init(
		Func<FormState> formStateGetter, Action<IReadOnlyCollection<DataModificationAction>> dataModificationAsserter,
		Func<DataModificationAction, PostBack> postBackSelector ) {
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
	/// <param name="defaultActionOverride">The default action. Pass null to use the post-back corresponding to the first of the data modification actions.
	/// </param>
	/// <param name="formControlDefaultActionOverride">The form-control-specific default action. Pass null to use the same action for both form controls and
	/// buttons.</param>
	public static void ExecuteWithActions(
		DataModificationActionsParameter dataModificationActions, Action method, NonPostBackFormAction? defaultActionOverride = null,
		SpecifiedValue<NonPostBackFormAction?>? formControlDefaultActionOverride = null ) {
		if( dataModificationActions.Collection.Value.Count == 0 )
			throw new ApplicationException( "There must be at least one data modification action." );
		dataModificationAsserter( dataModificationActions.Collection.Value );

		Current.stack.Push( ( defaultActionOverride, formControlDefaultActionOverride, new Stack<Func<bool>>(), dataModificationActions ) );
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
	/// <param name="defaultActionOverride">The default action. Pass null to use the post-back corresponding to the first of the data modification actions.
	/// </param>
	/// <param name="formControlDefaultActionOverride">The form-control-specific default action. Pass null to use the same action for both form controls and
	/// buttons.</param>
	public static T ExecuteWithActions<T>(
		DataModificationActionsParameter dataModificationActions, Func<T> method, NonPostBackFormAction? defaultActionOverride = null,
		SpecifiedValue<NonPostBackFormAction?>? formControlDefaultActionOverride = null ) {
		if( dataModificationActions.Collection.Value.Count == 0 )
			throw new ApplicationException( "There must be at least one data modification action." );
		dataModificationAsserter( dataModificationActions.Collection.Value );

		Current.stack.Push( ( defaultActionOverride, formControlDefaultActionOverride, new Stack<Func<bool>>(), dataModificationActions ) );
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

	private readonly Stack<( NonPostBackFormAction? actionOverride, SpecifiedValue<NonPostBackFormAction?>? formControlActionOverride, Stack<Func<bool>>
		validationPredicateStack, DataModificationActionsParameter dataModificationActions )> stack = new();

	private readonly HashSet<DataModificationAction> dataModificationsWithValidationsFromOtherElements = new();
	private readonly HashSet<DataModificationAction> dataModificationsWithValidations = new();

	internal FormState() {}

	/// <summary>
	/// Gets the current default action.
	/// </summary>
	public FormAction DefaultAction => actionOverride ?? (FormAction)new PostBackFormAction( PostBack );

	/// <summary>
	/// Gets the current form-control-specific default action. Returns null for no action.
	/// </summary>
	public FormAction? FormControlDefaultAction => formControlActionOverride is null ? DefaultAction : formControlActionOverride.Value;

	/// <summary>
	/// Gets the post-back corresponding to the first of the current data modification actions.
	/// </summary>
	public PostBack PostBack => postBackSelector( DataModificationActions.Collection.Value.First() );

	private NonPostBackFormAction? actionOverride => stack.Peek().actionOverride;
	private SpecifiedValue<NonPostBackFormAction?>? formControlActionOverride => stack.Peek().formControlActionOverride;

	/// <summary>
	/// PageBase use only.
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
	/// Gets the current data-modification actions.
	/// </summary>
	public DataModificationActionsParameter DataModificationActions => stack.Peek().dataModificationActions;

	/// <summary>
	/// PageBase use only.
	/// </summary>
	internal void AddValidationToDataModificationActions( EwfValidation validation ) {
		var dataModificationActions = DataModificationActions.Collection.Value;
		if( dataModificationsWithValidationsFromOtherElements.Overlaps( dataModificationActions ) )
			throw new Exception( "One or more of the data modification actions contain validations from other page elements." );

		foreach( var i in dataModificationActions )
			( (ValidationList)i ).AddValidation( validation );
		dataModificationsWithValidations.UnionWith( dataModificationActions );
	}

	/// <summary>
	/// PageTree use only.
	/// </summary>
	internal void SetForNextElement() {
		dataModificationsWithValidationsFromOtherElements.UnionWith( dataModificationsWithValidations );
		dataModificationsWithValidations.Clear();
	}
}