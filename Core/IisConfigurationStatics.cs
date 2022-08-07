using System.Reflection;

namespace EnterpriseWebLibrary {
	internal static class IisConfigurationStatics {
		internal delegate dynamic EnumGetter( string typeName, string valueName );

		internal static void ExecuteInServerManagerTransaction( bool iisExpress, Action<dynamic, EnumGetter> method ) {
			var assembly = getAssembly( iisExpress );
			using( dynamic serverManager = assembly.CreateInstance( "Microsoft.Web.Administration.ServerManager" ) ) {
				EnumGetter enumGetter = ( typeName, valueName ) => Enum.Parse( assembly.GetType( typeName ), valueName );
				method( serverManager, enumGetter );
				serverManager.CommitChanges();
			}
		}

		private static Assembly getAssembly( bool iisExpress ) {
			var assemblyName = "Microsoft.Web.Administration, Version=" + ( iisExpress ? "7.9.0.0" : "7.0.0.0" ) +
			                   ", Culture=neutral, PublicKeyToken=31bf3856ad364e35";
			return Assembly.Load( assemblyName );
		}
	}
}