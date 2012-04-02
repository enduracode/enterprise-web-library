using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Method that marks the file with the given ID as read.
	/// </summary>
	public delegate void MarkFileAsReadMethod( DBConnection cn, int fileId );
}