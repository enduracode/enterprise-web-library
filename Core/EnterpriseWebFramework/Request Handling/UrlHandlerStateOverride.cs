namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal interface UrlHandlerStateOverride {
	private static Func<UrlHandlerStateOverride?> currentOverrideGetter = null!;
	private static OverrideMethodExecutor overrideMethodExecutor = null!;

	internal interface OverrideMethodExecutor {
		public T ExecuteWithUrlHandlerStateOverride<T>( SpecifiedValue<UrlHandlerStateOverride?>? state, Func<T> method );
	}

	internal static void Init( Func<UrlHandlerStateOverride?> currentOverrideGetter, OverrideMethodExecutor overrideMethodExecutor ) {
		UrlHandlerStateOverride.currentOverrideGetter = currentOverrideGetter;
		UrlHandlerStateOverride.overrideMethodExecutor = overrideMethodExecutor;
	}

	static UrlHandlerStateOverride? Current => currentOverrideGetter();

	static T ExecuteWithOverride<T>( Func<T> method ) => overrideMethodExecutor.ExecuteWithUrlHandlerStateOverride( null, method );

	void Set( IReadOnlyCollection<BasicUrlHandler> handlers, ResourceParent webItem );

	void Set( ResourceBase resource );

	public T ExecuteWithThis<T>( Func<T> method ) =>
		overrideMethodExecutor.ExecuteWithUrlHandlerStateOverride( new SpecifiedValue<UrlHandlerStateOverride?>( this ), method );
}