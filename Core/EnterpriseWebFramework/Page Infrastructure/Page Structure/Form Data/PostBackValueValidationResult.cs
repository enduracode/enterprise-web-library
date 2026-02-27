namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal class PostBackValueValidationResult<T> {
	internal static PostBackValueValidationResult<T> CreateInvalid() => new( false );

	internal static PostBackValueValidationResult<T> CreateValid( T value ) => new( true, value );

	private readonly bool isValid;
	private readonly T? value;

	private PostBackValueValidationResult( bool isValid, T? value = default ) {
		this.isValid = isValid;
		this.value = value;
	}

	internal bool IsValid => isValid;
	internal T Value => value!;
}