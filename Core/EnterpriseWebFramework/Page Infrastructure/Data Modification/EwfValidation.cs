﻿using System;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A validation.
	/// </summary>
	// We renamed this from just Validation because it conflicted with the Validation namespace in the System.Collections.Immutable package.
	public class EwfValidation {
		private readonly Action<Validator> method;

		/// <summary>
		/// Creates a validation with the specified method and adds it to the current data modifications.
		/// </summary>
		/// <param name="method">The method that will be called by the data modification(s) to which this validation is added. Within the method, do not add
		/// modification methods to outside lists; this adds confusion and commonly leads to modification methods being skipped or executing in the wrong order.
		/// </param>
		/// <param name="validationList">Do not use; this will be removed when EnduraCode goal 768 is complete.</param>
		public EwfValidation( Action<Validator> method, ValidationList validationList = null ) {
			if( validationList != null ) {
				this.method = method;
				( (ValidationListInternal)validationList ).AddValidation( this );
			}
			else {
				var setupState = ValidationSetupState.Current;
				var dataModifications = setupState.DataModifications;

				if( setupState.DataModificationsWithValidationsFromOtherElements.Overlaps( dataModifications ) )
					throw new ApplicationException( "One or more of the data modifications contain validations from other page elements." );

				var predicate = setupState.ValidationPredicate;
				this.method = validator => {
					if( predicate() )
						method( validator );
				};

				foreach( var i in dataModifications )
					( (ValidationListInternal)i ).AddValidation( this );
				setupState.AddDataModificationsWithValidations( dataModifications );
			}
		}

		/// <summary>
		/// This overload exists to support form controls that still require a post-back-value dictionary. It will be removed when EnduraCode goal 588 is complete.
		/// </summary>
		public EwfValidation( Action<PostBackValueDictionary, Validator> method )
			: this( validator => method( AppRequestState.Instance.EwfPageRequestState.PostBackValues, validator ) ) {}

		[ Obsolete( "Guaranteed through 31 October 2016. Use ValidationSetupState.ExecuteWithDataModifications instead." ) ]
		public EwfValidation( Action<PostBackValueDictionary, Validator> method, ValidationList validationList )
			: this( validator => method( AppRequestState.Instance.EwfPageRequestState.PostBackValues, validator ), validationList ) {}

		internal Action<Validator> Method { get { return method; } }
	}
}