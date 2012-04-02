using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Defines how HTML block editing operations will be carried out against a database.
	/// The reason that we use a separate HTML block table rather than having everyone have their own CLOB field is to make it easy
	/// to add meta-data to the HTML block such as required CSS file paths, what version of HTML it is, etc.
	/// The schema for the database should be:
	///   CREATE TABLE HtmlBlocks(
	///     HtmlBlockId int NOT NULL CONSTRAINT pkHtmlBlocks PRIMARY KEY,
	///     Html nvarchar( MAX ) NOT NULL
	///   );
	/// We probably want to represent "no HTML" with a foreign key to an empty row rather than a null foreign key. This allows the HTML content
	/// to be cleared without clearing out any meta data (see above for examples of meta data that may eventually exist).
	/// </summary>
	public interface HtmlBlockEditingSetup {
		/// <summary>
		/// Retrieves the HTML from the specified HTML block.
		/// </summary>
		string GetHtml( DBConnection cn, int htmlBlockId );

		/// <summary>
		/// Updates the HTML in a specified HTML block.
		/// </summary>
		void UpdateHtml( DBConnection cn, int htmlBlockId, string html );

		/// <summary>
		/// Inserts a new row into the database with the given html.
		/// </summary>
		int InsertHtmlBlock( DBConnection cn, string html );
	}
}