using System;
using System.Collections.Generic;
using EnterpriseWebLibrary.InputValidation;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A hidden field.
	/// </summary>
	public class EwfHiddenField: FormControl<EtherealComponent> {
		private readonly EtherealComponent component;
		private readonly EwfValidation validation;

		/// <summary>
		/// Creates a hidden field.
		/// </summary>
		/// <param name="value">Do not pass null.</param>
		/// <param name="validationMethod">The validation method. Do not pass null.</param>
		/// <param name="id"></param>
		/// <param name="pageModificationValue"></param>
		public EwfHiddenField(
			string value, Action<PostBackValue<string>, Validator> validationMethod, HiddenFieldId id = null,
			PageModificationValue<string> pageModificationValue = null ) {
			pageModificationValue = pageModificationValue ?? new PageModificationValue<string>();

			var elementId = new ElementId();
			var formValue = new FormValue<string>(
				() => value,
				() => elementId.Id,
				v => v,
				rawValue => rawValue != null ? PostBackValueValidationResult<string>.CreateValid( rawValue ) : PostBackValueValidationResult<string>.CreateInvalid() );

			component = new ElementComponent(
				context => {
					elementId.AddId( context.Id );
					id?.AddId( context.Id );
					return new ElementData(
						() => {
							var attributes = new List<Tuple<string, string>>();
							attributes.Add( Tuple.Create( "type", "hidden" ) );
							attributes.Add( Tuple.Create( "name", context.Id ) );
							attributes.Add( Tuple.Create( "value", pageModificationValue.Value ) );

							return new ElementLocalData(
								"input",
								attributes: attributes,
								includeIdAttribute: id != null || pageModificationValue != null,
								jsInitStatements: pageModificationValue != null
									                  ? "$( '#{0}' ).change( function() {{ {1} }} );".FormatWith(
										                  context.Id,
										                  pageModificationValue.GetJsModificationStatements( "$( this ).val()" ) )
									                  : "" );
						} );
				},
				formValue: formValue );

			validation = formValue.CreateValidation( validationMethod );

			formValue.AddPageModificationValue( pageModificationValue, v => v );
		}

		public EtherealComponent PageComponent => component;
		public EwfValidation Validation => validation;
	}
}