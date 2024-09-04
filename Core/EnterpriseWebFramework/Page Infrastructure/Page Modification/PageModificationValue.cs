#nullable disable
using System.Text;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A form-control-dependent value used to modify the page consistently on both the server and the client.
/// </summary>
public class PageModificationValue<T> {
	private readonly InitializationAwareValue<T> value = new();
	private readonly List<Func<string, string>> jsModificationStatements = new();

	/// <summary>
	/// Creates a page-modification value.
	/// </summary>
	public PageModificationValue() {}

	/// <summary>
	/// Adds a JavaScript statement that should be executed when the value changes.
	/// </summary>
	/// <param name="statementGetter">A function that takes the value expression and returns a complete statement. Do not pass null.</param>
	public void AddJsModificationStatement( Func<string, string> statementGetter ) {
		// This dependency on PageBase should not exist.
		PageBase.AssertPageTreeNotBuilt();

		jsModificationStatements.Add( statementGetter );
	}

	internal void AddValue( T value ) {
		if( this.value.Initialized )
			throw new ApplicationException( "The value was already added." );
		this.value.Value = value;
	}

	/// <summary>
	/// Gets the value. Not available until after rendering preparation has started.
	/// </summary>
	public T Value {
		get {
			// This dependency on PageBase should not exist.
			PageBase.AssertRenderingPreparationStarted();

			return value.Value;
		}
	}

	/// <summary>
	/// Returns the JavaScript statements that should be executed when the value changes. Not available until after rendering preparation has started.
	/// </summary>
	public string GetJsModificationStatements( string valueExpression ) {
		// This dependency on PageBase should not exist.
		PageBase.AssertRenderingPreparationStarted();

		return jsModificationStatements.Aggregate(
			new StringBuilder(),
			( builder, statementGetter ) => builder.Append( statementGetter( valueExpression ) ),
			i => i.ToString() );
	}
}