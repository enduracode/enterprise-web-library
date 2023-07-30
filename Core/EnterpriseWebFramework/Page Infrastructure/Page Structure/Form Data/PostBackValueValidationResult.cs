#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class PostBackValueValidationResult<T> {
		internal static PostBackValueValidationResult<T> CreateInvalid() {
			return new PostBackValueValidationResult<T>( false );
		}

		internal static PostBackValueValidationResult<T> CreateValid( T value ) {
			return new PostBackValueValidationResult<T>( true, value );
		}

		private readonly bool isValid;
		private readonly T value;

		private PostBackValueValidationResult( bool isValid, T value = default( T ) ) {
			this.isValid = isValid;
			this.value = value;
		}

		internal bool IsValid { get { return isValid; } }
		internal T Value { get { return value; } }
	}
}