using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
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
		/// Executes the specified method with the specified data modifications being used for any validations that are created, and with the default action being
		/// available to form controls and buttons.
		/// </summary>
		/// <param name="dataModifications"></param>
		/// <param name="method"></param>
		/// <param name="defaultActionOverride">Pass null to use the post-back corresponding to the first of the data modifications.</param>
		public static void ExecuteWithDataModificationsAndDefaultAction(
			IEnumerable<DataModification> dataModifications, Action method, NonPostBackFormAction defaultActionOverride = null ) {
			IReadOnlyCollection<DataModification> dmCollection = dataModifications.ToImmutableArray();
			if( dmCollection.Count == 0 )
				throw new ApplicationException( "There must be at least one data modification." );
			dataModificationAsserter( dmCollection );

			Current.stack.Push( Tuple.Create( defaultActionOverride, new Stack<Func<bool>>(), dmCollection ) );
			try {
				method();
			}
			finally {
				Current.stack.Pop();
			}
		}

		/// <summary>
		/// Executes the specified method with the specified data modifications being used for any validations that are created, and with the default action being
		/// available to form controls and buttons.
		/// </summary>
		/// <param name="dataModifications"></param>
		/// <param name="method"></param>
		/// <param name="defaultActionOverride">Pass null to use the post-back corresponding to the first of the data modifications.</param>
		public static T ExecuteWithDataModificationsAndDefaultAction<T>(
			IEnumerable<DataModification> dataModifications, Func<T> method, NonPostBackFormAction defaultActionOverride = null ) {
			IReadOnlyCollection<DataModification> dmCollection = dataModifications.ToImmutableArray();
			if( dmCollection.Count == 0 )
				throw new ApplicationException( "There must be at least one data modification." );
			dataModificationAsserter( dmCollection );

			Current.stack.Push( Tuple.Create( defaultActionOverride, new Stack<Func<bool>>(), dmCollection ) );
			try {
				return method();
			}
			finally {
				Current.stack.Pop();
			}
		}

		/// <summary>
		/// Executes the specified method with the specified predicate being used for any validations that are created.
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
		/// Executes the specified method with the specified predicate being used for any validations that are created.
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

		private readonly Stack<Tuple<NonPostBackFormAction, Stack<Func<bool>>, IReadOnlyCollection<DataModification>>> stack =
			new Stack<Tuple<NonPostBackFormAction, Stack<Func<bool>>, IReadOnlyCollection<DataModification>>>();

		/// <summary>
		/// EwfPage and private use only.
		/// </summary>
		internal readonly HashSet<DataModification> DataModificationsWithValidationsFromOtherElements = new HashSet<DataModification>();

		private readonly HashSet<DataModification> dataModificationsWithValidations = new HashSet<DataModification>();

		internal FormState() {}

		/// <summary>
		/// Gets the current default action.
		/// </summary>
		public FormAction DefaultAction => DefaultActionOverride ?? (FormAction)new PostBackFormAction( PostBack );

		/// <summary>
		/// Gets the post-back corresponding to the first of the current data modifications.
		/// </summary>
		public PostBack PostBack => postBackSelector( DataModifications.First() );

		/// <summary>
		/// Gets the current default-action override.
		/// </summary>
		public NonPostBackFormAction DefaultActionOverride => stack.Peek().Item1;

		/// <summary>
		/// EwfPage use only.
		/// </summary>
		internal Func<bool> ValidationPredicate {
			get {
				var predicates = validationPredicateStack.Reverse().ToImmutableArray();
				return () => {
					foreach( var predicate in predicates ) {
						if( !predicate() )
							return false;
					}
					return true;
				};
			}
		}

		private Stack<Func<bool>> validationPredicateStack => stack.Peek().Item2;

		/// <summary>
		/// Gets the current data modifications.
		/// </summary>
		public IReadOnlyCollection<DataModification> DataModifications => stack.Peek().Item3;

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
}