using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Ui {
	internal static class EwfUiStatics {
		private const string providerName = "EwfUi";
		private static AppEwfUiProvider provider;

		internal static void Init( Type globalType ) {
			var appAssembly = globalType.Assembly;
			var typeName = globalType.Namespace + ".Providers." + providerName + "Provider";

			if( appAssembly.GetType( typeName ) != null )
				provider = appAssembly.CreateInstance( typeName ) as AppEwfUiProvider;
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public static AppEwfUiProvider AppProvider {
			get {
				if( provider == null )
					throw new ApplicationException(
						providerName + " provider not found in application. To implement, create a class named " + providerName +
						@"Provider in ""Your Web Site\Providers"" that derives from App" + providerName + "Provider." );
				return provider;
			}
		}
	}
}