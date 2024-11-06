using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A validation.
/// </summary>
// We renamed this from just Validation because it conflicted with the Validation namespace in the System.Collections.Immutable package.
public class EwfValidation {
	private static Func<Func<bool>> validationPredicateGetter = null!;
	private static Action<EwfValidation> validationAdder = null!;

	internal static void Init( Func<Func<bool>> validationPredicateGetter, Action<EwfValidation> validationAdder ) {
		EwfValidation.validationPredicateGetter = validationPredicateGetter;
		EwfValidation.validationAdder = validationAdder;
	}

	private readonly Action<Validator> method;

	/// <summary>
	/// Creates a validation with the specified method and adds it to the current data modifications.
	/// </summary>
	/// <param name="method">The method that will be called by the data modification(s) to which this validation is added. Within the method, do not add
	/// modification methods to outside lists; this adds confusion and commonly leads to modification methods being skipped or executing in the wrong order.
	/// </param>
	public EwfValidation( Action<Validator> method ) {
		var predicate = validationPredicateGetter();
		this.method = validator => {
			if( predicate() )
				method( validator );
		};

		validationAdder( this );
	}

	internal Action<Validator> Method => method;
}