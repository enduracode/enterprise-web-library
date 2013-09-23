using System;
using System.Linq;
using System.Web;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	internal interface FormValue {
		/// <summary>
		/// Returns the empty string if this form value is inactive.
		/// </summary>
		string GetPostBackValueKey();

		string GetDurableValueAsString();
		bool PostBackValueIsValid( PostBackValueDictionary postBackValues );
		bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues );
	}

	internal class FormValue<T>: FormValue {
		private readonly Func<T> durableValueGetter;

		// This should not be called until after control tree validation, to enable scenarios such as RadioButtonGroup using its first on-page button's UniqueID as
		// the key.
		private readonly Func<string> postBackValueKeyGetter;

		private readonly Func<T, string> stringValueSelector;
		private readonly Func<string, PostBackValueValidationResult<T>> stringPostBackValueValidator;
		private readonly Func<HttpPostedFile, PostBackValueValidationResult<T>> filePostBackValueValidator;

		/// <summary>
		/// Creates a form value.
		/// </summary>
		/// <param name="durableValueGetter"></param>
		/// <param name="postBackValueKeyGetter"></param>
		/// <param name="stringValueSelector"></param>
		/// <param name="stringPostBackValueValidator">Avoid using exceptions in this method if possible since it is sometimes called many times during a request,
		/// and we've seen exceptions take as long as 50 ms each when debugging.</param>
		internal FormValue( Func<T> durableValueGetter, Func<string> postBackValueKeyGetter, Func<T, string> stringValueSelector,
		                    Func<string, PostBackValueValidationResult<T>> stringPostBackValueValidator ) {
			this.durableValueGetter = durableValueGetter;
			this.postBackValueKeyGetter = postBackValueKeyGetter;
			this.stringValueSelector = stringValueSelector;
			this.stringPostBackValueValidator = stringPostBackValueValidator;

			EwfPage.Instance.AddFormValue( this );
		}

		/// <summary>
		/// Creates a form value.
		/// </summary>
		/// <param name="durableValueGetter"></param>
		/// <param name="postBackValueKeyGetter"></param>
		/// <param name="stringValueSelector"></param>
		/// <param name="filePostBackValueValidator">Avoid using exceptions in this method if possible since it is sometimes called many times during a request, and
		/// we've seen exceptions take as long as 50 ms each when debugging.</param>
		internal FormValue( Func<T> durableValueGetter, Func<string> postBackValueKeyGetter, Func<T, string> stringValueSelector,
		                    Func<HttpPostedFile, PostBackValueValidationResult<T>> filePostBackValueValidator ) {
			this.durableValueGetter = durableValueGetter;
			this.postBackValueKeyGetter = postBackValueKeyGetter;
			this.stringValueSelector = stringValueSelector;
			this.filePostBackValueValidator = filePostBackValueValidator;

			EwfPage.Instance.AddFormValue( this );
		}

		internal T GetDurableValue() {
			return durableValueGetter();
		}

		string FormValue.GetPostBackValueKey() {
			return postBackValueKeyGetter();
		}

		string FormValue.GetDurableValueAsString() {
			return stringValueSelector( durableValueGetter() );
		}

		bool FormValue.PostBackValueIsValid( PostBackValueDictionary postBackValues ) {
			return validatePostBackValue( postBackValues ).IsValid;
		}

		internal T GetValue( PostBackValueDictionary postBackValues ) {
			if( postBackValues == null )
				return durableValueGetter();
			var result = validatePostBackValue( postBackValues );
			return result.IsValid && result.HasValue ? result.Value : durableValueGetter();
		}

		private PostBackValueValidationResult<T> validatePostBackValue( PostBackValueDictionary postBackValues ) {
			var key = postBackValueKeyGetter();
			if( !key.Any() )
				return PostBackValueValidationResult<T>.CreateInvalid();

			var value = postBackValues.GetValue( key );

			if( filePostBackValueValidator != null ) {
				var fileValue = value as HttpPostedFile;
				return value == null || fileValue != null ? filePostBackValueValidator( fileValue ) : PostBackValueValidationResult<T>.CreateInvalid();
			}

			var stringValue = value as string;
			return value == null || stringValue != null ? stringPostBackValueValidator( stringValue ) : PostBackValueValidationResult<T>.CreateInvalid();
		}

		internal bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return !StandardLibraryMethods.AreEqual( GetValue( postBackValues ), durableValueGetter() );
		}

		bool FormValue.ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return ValueChangedOnPostBack( postBackValues );
		}
	}
}