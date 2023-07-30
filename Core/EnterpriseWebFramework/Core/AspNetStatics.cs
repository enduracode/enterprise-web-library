#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework.Core {
	public static class AspNetStatics {
		private static Func<IServiceProvider> currentServicesGetter;

		internal static void Init( Func<IServiceProvider> currentServicesGetter ) {
			AspNetStatics.currentServicesGetter = currentServicesGetter;
		}

		/// <summary>
		/// Gets the service container for the current request, or for the application if called outside of a request.
		/// </summary>
		public static IServiceProvider Services => currentServicesGetter();
	}
}