namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// An autofocus condition.
/// </summary>
public sealed class AutofocusCondition {
	/// <summary>
	/// Creates a condition that will be true if this is an initial request for the page.
	/// </summary>
	/// <param name="pageCondition"></param>
	public static AutofocusCondition InitialRequest( PageModificationValueCondition? pageCondition = null ) => new( ( "", pageCondition ).ToCollection() );

	/// <summary>
	/// Creates a condition that will be true if this is a post-back request and the current focus key matches the specified value.
	/// </summary>
	/// <param name="focusKey">Do not pass null or the empty string.</param>
	/// <param name="pageCondition"></param>
	public static AutofocusCondition PostBack( string focusKey, PageModificationValueCondition? pageCondition = null ) =>
		new( ( focusKey, pageCondition ).ToCollection() );

	private readonly IReadOnlyCollection<( string focusKey, PageModificationValueCondition? pageCondition )> focusKeyAndPageConditionPairs;

	private AutofocusCondition( IReadOnlyCollection<( string focusKey, PageModificationValueCondition? pageCondition )> focusKeyAndPageConditionPairs ) {
		this.focusKeyAndPageConditionPairs = focusKeyAndPageConditionPairs;
	}

	/// <summary>
	/// Returns a condition that will be true if either this or the specified condition is true.
	/// </summary>
	public AutofocusCondition Or( AutofocusCondition condition ) =>
		new( focusKeyAndPageConditionPairs.Concat( condition.focusKeyAndPageConditionPairs ).Materialize() );

	internal bool IsTrue( string? focusKey ) =>
		focusKeyAndPageConditionPairs.Any( i =>
			( i.focusKey.Any() ? i.focusKey == focusKey : focusKey == null ) && ( i.pageCondition == null || i.pageCondition.IsTrue ) );
}