using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class ValidationSetupState {
		private static Func<ValidationSetupState> stateGetter;
		private static Action<IReadOnlyCollection<DataModification>> dataModificationAsserter;

		internal static void Init( Func<ValidationSetupState> validationSetupStateGetter, Action<IReadOnlyCollection<DataModification>> dataModificationAsserter ) {
			stateGetter = validationSetupStateGetter;
			ValidationSetupState.dataModificationAsserter = dataModificationAsserter;
		}

		/// <summary>
		/// Gets the current validation-setup state. This will throw an exception when called from the worker threads used by parallel programming tools such as
		/// PLINQ and the Task Parallel Library, since we want to avoid the race conditions that would result from multiple threads creating validations.
		/// </summary>
		public static ValidationSetupState Current {
			get {
				var state = stateGetter();
				if( state == null )
					throw new ApplicationException( "No validation-setup state exists at this time." );
				return state;
			}
		}

		/// <summary>
		/// Executes the specified method with the specified data modifications being used for any validations that are created.
		/// </summary>
		public static void ExecuteWithDataModifications( IReadOnlyCollection<DataModification> dataModifications, Action method ) {
			if( dataModifications.Count == 0 )
				throw new ApplicationException( "There must be at least one data modification." );
			dataModificationAsserter( dataModifications );

			Current.stack.Push( Tuple.Create( new Stack<Func<bool>>(), dataModifications ) );
			try {
				method();
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

		private readonly Stack<Tuple<Stack<Func<bool>>, IReadOnlyCollection<DataModification>>> stack =
			new Stack<Tuple<Stack<Func<bool>>, IReadOnlyCollection<DataModification>>>();

		/// <summary>
		/// EwfValidation and private use only.
		/// </summary>
		internal readonly HashSet<DataModification> DataModificationsWithValidationsFromOtherElements = new HashSet<DataModification>();

		private readonly HashSet<DataModification> dataModificationsWithValidations = new HashSet<DataModification>();

		internal ValidationSetupState() {}

		/// <summary>
		/// EwfValidation use only.
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

		private Stack<Func<bool>> validationPredicateStack { get { return stack.Peek().Item1; } }

		/// <summary>
		/// Gets the current data modifications.
		/// </summary>
		public IReadOnlyCollection<DataModification> DataModifications { get { return stack.Peek().Item2; } }

		/// <summary>
		/// EwfValidation use only.
		/// </summary>
		internal void AddDataModificationsWithValidations( IReadOnlyCollection<DataModification> dataModifications ) {
			dataModificationsWithValidations.UnionWith( dataModifications );
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