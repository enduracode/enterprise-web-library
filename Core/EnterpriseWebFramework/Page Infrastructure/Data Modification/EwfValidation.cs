using System;
using System.Collections.Generic;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A validation.
	/// </summary>
	// We renamed this from just Validation because it conflicted with the Validation namespace in the System.Collections.Immutable package.
	public class EwfValidation {
		private static Func<Func<bool>> validationPredicateGetter;
		private static Func<IReadOnlyCollection<DataModification>> dataModificationGetter;
		private static Func<HashSet<DataModification>> otherElementDataModificationGetter;
		private static Action validationCreationReporter;

		internal static void Init(
			Func<Func<bool>> validationPredicateGetter, Func<IReadOnlyCollection<DataModification>> dataModificationGetter,
			Func<HashSet<DataModification>> otherElementDataModificationGetter, Action validationCreationReporter ) {
			EwfValidation.validationPredicateGetter = validationPredicateGetter;
			EwfValidation.dataModificationGetter = dataModificationGetter;
			EwfValidation.otherElementDataModificationGetter = otherElementDataModificationGetter;
			EwfValidation.validationCreationReporter = validationCreationReporter;
		}

		private readonly Action<Validator> method;

		/// <summary>
		/// Creates a validation with the specified method and adds it to the current data modifications.
		/// </summary>
		/// <param name="method">The method that will be called by the data modification(s) to which this validation is added. Within the method, do not add
		/// modification methods to outside lists; this adds confusion and commonly leads to modification methods being skipped or executing in the wrong order.
		/// </param>
		public EwfValidation( Action<Validator> method ) {
			var dataModifications = dataModificationGetter();
			if( otherElementDataModificationGetter().Overlaps( dataModifications ) )
				throw new ApplicationException( "One or more of the data modifications contain validations from other page elements." );

			var predicate = validationPredicateGetter();
			this.method = validator => {
				if( predicate() )
					method( validator );
			};

			foreach( var i in dataModifications )
				( (ValidationList)i ).AddValidation( this );
			validationCreationReporter();
		}

		/// <summary>
		/// This overload exists to support form controls that still require a post-back-value dictionary. It will be removed when EnduraCode goal 588 is complete.
		/// </summary>
		public EwfValidation( Action<PostBackValueDictionary, Validator> method )
			: this( validator => method( AppRequestState.Instance.EwfPageRequestState.PostBackValues, validator ) ) {}

		internal Action<Validator> Method => method;
	}
}