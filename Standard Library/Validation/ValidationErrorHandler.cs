using System;
using System.Collections.Generic;

namespace RedStapler.StandardLibrary.Validation {
	/// <summary>
	/// This class allows you to control what happens when a validation method generates an error. Every validation method takes a ValidationErrorHandler object
	/// as the first parameter. Currently you can't re-use these objects for more than one validation call since most validation methods don't reset LastResult.
	/// </summary>
	public class ValidationErrorHandler {
		/// <summary>
		/// Method that handles errors instead of the default handling mechanism.
		/// </summary>
		public delegate void CustomHandler( Validator validator, ErrorCondition errorCondition );

		private readonly string subject = "field";
		private readonly CustomHandler customHandler;
		private readonly Dictionary<ErrorCondition, string> customMessages = new Dictionary<ErrorCondition, string>();
		private ValidationResult validationResult = ValidationResult.NoError();

		/// <summary>
		/// Creates an error handler that adds standard error messages, based on the specified subject, to the validator. If the subject is used to start a
		/// sentence, it will be automatically capitalized.
		/// </summary>
		public ValidationErrorHandler( string subject ) {
			this.subject = subject;
		}

		/// <summary>
		/// Creates an object that invokes code of your choice if an error occurs. The given custom handler will
		/// only be invoked in the case of an error, and it will prevent the default error message from being
		/// added to the validator's error collection.  Even if the handler does not add an error to the validator,
		/// valdiator.HasErrors will return true because an error has still occurred.
		/// </summary>
		public ValidationErrorHandler( CustomHandler customHandler ) {
			this.customHandler = customHandler;
		}

		/// <summary>
		/// Modifies this error handler to use a custom message if any errors occur with the specified conditions. If no error conditions are passed, the message
		/// will be used for all errors. This method has no effect if a custom handler has been specified.
		/// </summary>
		public void AddCustomErrorMessage( string message, params ErrorCondition[] errorConditions ) {
			if( errorConditions.Length > 0 ) {
				foreach( var e in errorConditions )
					customMessages.Add( e, message );
			}
			else {
				foreach( var e in EnumTools.GetValues<ErrorCondition>() )
					customMessages.Add( e, message );
			}
		}

		/// <summary>
		/// The subject of the error message, if one needs to be generated.
		/// </summary>
		internal string Subject { get { return subject; } }

		private bool used;

		internal void SetValidationResult( ValidationResult validationResult ) {
			if( used )
				throw new ApplicationException( "Validation error handlers cannot be re-used." );
			used = true;
			this.validationResult = validationResult;
		}

		/// <summary>
		/// Returns the ErrorCondition resulting from the validation of the data associated with this package.
		/// </summary>
		public ErrorCondition LastResult { get { return validationResult.ErrorCondition; } }

		/// <summary>
		/// If LastResult is not NoError, this method invokes the appropriate behavior according to how this error handler was created.
		/// </summary>
		internal void HandleResult( Validator validator, bool errorWouldResultInUnusableReturnValue ) {
			if( validationResult.ErrorCondition == ErrorCondition.NoError )
				return;

			// if there is a custom handler, run it and do nothing else
			if( customHandler != null ) {
				validator.NoteError();
				customHandler( validator, validationResult.ErrorCondition );
				return;
			}

			// build the error message
			string message;
			if( !customMessages.TryGetValue( validationResult.ErrorCondition, out message ) )
				// NOTE: Do we really need custom message, or can the custom handler manage that?
				message = validationResult.GetErrorMessage( subject );

			validator.AddError( new Error( message, errorWouldResultInUnusableReturnValue ) );
		}
	}
}