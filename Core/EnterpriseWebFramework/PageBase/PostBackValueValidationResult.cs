namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class PostBackValueValidationResult<T> {
		internal static PostBackValueValidationResult<T> CreateInvalid() {
			return new PostBackValueValidationResult<T>( false, false );
		}

		internal static PostBackValueValidationResult<T> CreateValidWithNoValue() {
			return new PostBackValueValidationResult<T>( true, false );
		}

		internal static PostBackValueValidationResult<T> CreateValidWithValue( T value ) {
			return new PostBackValueValidationResult<T>( true, true, value );
		}

		private readonly bool isValid;
		private readonly bool hasValue;
		private readonly T value;

		internal PostBackValueValidationResult( bool isValid, bool hasValue, T value = default( T ) ) {
			this.isValid = isValid;
			this.hasValue = hasValue;
			this.value = value;
		}

		internal bool IsValid { get { return isValid; } }
		internal bool HasValue { get { return hasValue; } }
		internal T Value { get { return value; } }
	}
}