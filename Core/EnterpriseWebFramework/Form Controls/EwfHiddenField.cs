using System;
using System.Collections.Generic;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A hidden field.
	/// </summary>
	public class EwfHiddenField: FormControl<EtherealComponentOrElement> {
		private readonly EtherealComponentOrElement component;
		private readonly EwfValidation validation;

		/// <summary>
		/// Creates a hidden field.
		/// </summary>
		/// <param name="value">Do not pass null.</param>
		/// <param name="validationMethod">The validation method. Do not pass null.</param>
		/// <param name="id"></param>
		/// <param name="pageModificationValue"></param>
		public EwfHiddenField(
			string value, Action<PostBackValue<string>, Validator> validationMethod, HiddenFieldId id = null, PageModificationValue<string> pageModificationValue = null ) {
			pageModificationValue = pageModificationValue ?? new PageModificationValue<string>();

			var idLocal = id ?? new HiddenFieldId();
			var formValue = new FormValue<string>(
				() => value,
				() => idLocal.Id,
				v => v,
				rawValue => rawValue != null ? PostBackValueValidationResult<string>.CreateValid( rawValue ) : PostBackValueValidationResult<string>.CreateInvalid() );

			component = new ElementComponent(
				context => {
					idLocal.AddId( context.Id );
					return new ElementData(
						() => {
							var attributes = new List<Tuple<string, string>>();
							attributes.Add( Tuple.Create( "type", "hidden" ) );
							attributes.Add( Tuple.Create( "name", context.Id ) );
							attributes.Add( Tuple.Create( "value", pageModificationValue.Value ) );

							return new ElementLocalData( "input", attributes: attributes, includeIdAttribute: id != null );
						} );
				},
				formValue: formValue );

			validation = formValue.CreateValidation( validationMethod );

			formValue.AddPageModificationValue( pageModificationValue, v => v );
		}

		public EtherealComponentOrElement PageComponent => component;
		public EwfValidation Validation => validation;
	}
}