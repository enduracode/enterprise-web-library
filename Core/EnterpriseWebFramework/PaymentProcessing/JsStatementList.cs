﻿namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A list of JavaScript statements.
/// </summary>
public sealed class JsStatementList {
	private Func<string>? statementGetter;

	/// <summary>
	/// Creates a JavaScript statement list.
	/// </summary>
	public JsStatementList() {}

	/// <summary>
	/// Adds the JavaScript statement getter. This can only be called once.
	/// </summary>
	internal void AddStatementGetter( Func<string> statementGetter ) {
		PageBase.AssertPageTreeNotBuilt();
		if( this.statementGetter is not null )
			throw new ApplicationException( "The statement getter was already added." );
		this.statementGetter = statementGetter;
	}

	/// <summary>
	/// Returns the JavaScript statements. Not available until after rendering preparation has started.
	/// </summary>
	public string GetStatements() {
		PageBase.AssertRenderingPreparationStarted();
		return statementGetter is not null ? statementGetter() : "";
	}
}