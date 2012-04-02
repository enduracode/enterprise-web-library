using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Method invoked when a new file is uploaded.
	/// </summary>
	public delegate void NewFileNotificationMethod( DBConnection cn, int newFileId );
}