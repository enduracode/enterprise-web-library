using Microsoft.AspNetCore.Http;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface FormValue {
		/// <summary>
		/// Returns the empty string if this form value is inactive.
		/// </summary>
		string GetPostBackValueKey();

		string GetDurableValueAsString();
		bool PostBackValueIsInvalid();
		IReadOnlyCollection<DataModification> DataModifications { get; }
		bool ValueChangedOnPostBack();
		void SetPageModificationValues();
	}

	internal class FormValue<T>: FormValue {
		private readonly Func<T> durableValueGetter;

		// This should not be called until after the page tree has been built, to enable scenarios such as RadioButtonGroup using its first on-page button's ID as
		// the key.
		private readonly Func<string> postBackValueKeyGetter;

		private readonly Func<T, string> stringValueSelector;
		private readonly Func<string, PostBackValueValidationResult<T>> stringPostBackValueValidator;
		private readonly Func<IFormFile, PostBackValueValidationResult<T>> filePostBackValueValidator;
		private readonly List<Action<T>> pageModificationValueAdders = new List<Action<T>>();
		private readonly HashSet<DataModification> dataModifications = new HashSet<DataModification>();

		/// <summary>
		/// Creates a form value.
		/// </summary>
		/// <param name="durableValueGetter"></param>
		/// <param name="postBackValueKeyGetter"></param>
		/// <param name="stringValueSelector"></param>
		/// <param name="stringPostBackValueValidator">Avoid using exceptions in this method if possible since it is sometimes called many times during a request,
		/// and we've seen exceptions take as long as 50 ms each when debugging.</param>
		public FormValue(
			Func<T> durableValueGetter, Func<string> postBackValueKeyGetter, Func<T, string> stringValueSelector,
			Func<string, PostBackValueValidationResult<T>> stringPostBackValueValidator ) {
			this.durableValueGetter = durableValueGetter;
			this.postBackValueKeyGetter = postBackValueKeyGetter;
			this.stringValueSelector = stringValueSelector;
			this.stringPostBackValueValidator = stringPostBackValueValidator;

			FormValueStatics.FormValueAdder( this );
		}

		/// <summary>
		/// Creates a form value.
		/// </summary>
		/// <param name="durableValueGetter"></param>
		/// <param name="postBackValueKeyGetter"></param>
		/// <param name="stringValueSelector"></param>
		/// <param name="filePostBackValueValidator">Avoid using exceptions in this method if possible since it is sometimes called many times during a request, and
		/// we've seen exceptions take as long as 50 ms each when debugging.</param>
		public FormValue(
			Func<T> durableValueGetter, Func<string> postBackValueKeyGetter, Func<T, string> stringValueSelector,
			Func<IFormFile, PostBackValueValidationResult<T>> filePostBackValueValidator ) {
			this.durableValueGetter = durableValueGetter;
			this.postBackValueKeyGetter = postBackValueKeyGetter;
			this.stringValueSelector = stringValueSelector;
			this.filePostBackValueValidator = filePostBackValueValidator;

			FormValueStatics.FormValueAdder( this );
		}

		/// <summary>
		/// Adds the specified page-modification value.
		/// </summary>
		public void AddPageModificationValue<ModificationValueType>(
			PageModificationValue<ModificationValueType> pageModificationValue, Func<T, ModificationValueType> modificationValueSelector ) {
			pageModificationValueAdders.Add( value => pageModificationValue.AddValue( modificationValueSelector( value ) ) );
		}

		/// <summary>
		/// Creates a validation with the specified method and adds it to the current data modifications.
		/// </summary>
		/// <param name="validationMethod">The method that will be called by the data modification(s) to which this validation is added.</param>
		public EwfValidation CreateValidation( Action<PostBackValue<T>, Validator> validationMethod ) {
			dataModifications.UnionWith( FormValueStatics.DataModificationGetter() );
			return new EwfValidation( validator => validationMethod( new PostBackValue<T>( getValue(), valueChangedOnPostBack() ), validator ) );
		}

		public T GetDurableValue() {
			return durableValueGetter();
		}

		string FormValue.GetPostBackValueKey() {
			return postBackValueKeyGetter();
		}

		string FormValue.GetDurableValueAsString() {
			return stringValueSelector( durableValueGetter() );
		}

		bool FormValue.PostBackValueIsInvalid() {
			var postBackValues = FormValueStatics.PostBackValueDictionaryGetter();
			var key = postBackValueKeyGetter();
			return !postBackValues.KeyRemoved( key ) && !validatePostBackValue( postBackValues.GetValue( key ) ).IsValid;
		}

		IReadOnlyCollection<DataModification> FormValue.DataModifications => dataModifications;

		bool FormValue.ValueChangedOnPostBack() {
			return valueChangedOnPostBack();
		}

		private bool valueChangedOnPostBack() => !EwlStatics.AreEqual( getValue(), durableValueGetter() );

		void FormValue.SetPageModificationValues() {
			foreach( var i in pageModificationValueAdders )
				i( getValue() );
		}

		private T getValue() {
			var postBackValues = FormValueStatics.PostBackValueDictionaryGetter();
			var key = postBackValueKeyGetter();
			if( !key.Any() || postBackValues == null || postBackValues.KeyRemoved( key ) )
				return durableValueGetter();
			var result = validatePostBackValue( postBackValues.GetValue( key ) );
			return result.IsValid ? result.Value : durableValueGetter();
		}

		private PostBackValueValidationResult<T> validatePostBackValue( object value ) {
			if( filePostBackValueValidator != null )
				return value != null ? filePostBackValueValidator( value as IFormFile ) : PostBackValueValidationResult<T>.CreateInvalid();

			var stringValue = value as string;
			return value == null || stringValue != null ? stringPostBackValueValidator( stringValue ) : PostBackValueValidationResult<T>.CreateInvalid();
		}
	}

	internal static class FormValueStatics {
		internal static Action<FormValue> FormValueAdder;
		internal static Func<IReadOnlyCollection<DataModification>> DataModificationGetter;
		internal static Func<PostBackValueDictionary> PostBackValueDictionaryGetter;

		internal static void Init(
			Action<FormValue> formValueAdder, Func<IReadOnlyCollection<DataModification>> dataModificationGetter,
			Func<PostBackValueDictionary> postBackValueDictionaryGetter ) {
			FormValueAdder = formValueAdder;
			DataModificationGetter = dataModificationGetter;
			PostBackValueDictionaryGetter = postBackValueDictionaryGetter;
		}
	}
}