namespace EnterpriseWebLibrary.Configuration {
	internal class DefaultCookieAttributes {
		internal readonly string Domain;
		internal readonly string Path;
		internal readonly string NamePrefix;

		internal DefaultCookieAttributes( string domain, string path, string namePrefix ) {
			Domain = domain;
			Path = path;
			NamePrefix = namePrefix;
		}
	}
}