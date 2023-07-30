#nullable disable
using System;
using EnterpriseWebLibrary.IO;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a BLOB file manager.
	/// </summary>
	public class BlobFileManagerSetup {
		/// <summary>
		/// Creates a setup object for a BLOB file manager.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the element.</param>
		/// <param name="thumbnailResourceGetter">A function that takes a file ID and returns the corresponding thumbnail resource. Do not return null.</param>
		/// <param name="omitNoExistingFileMessage">Pass true if you do not want to show “No existing file” when there is no file in the database.</param>
		/// <param name="uploadValidationPredicate"></param>
		/// <param name="uploadValidationErrorNotifier"></param>
		/// <param name="uploadValidationMethod"></param>
		public static BlobFileManagerSetup Create(
			DisplaySetup displaySetup = null, ElementClassSet classes = null, Func<int, ResourceInfo> thumbnailResourceGetter = null,
			bool omitNoExistingFileMessage = false, Func<bool, bool> uploadValidationPredicate = null, Action uploadValidationErrorNotifier = null,
			Action<RsFile, Validator> uploadValidationMethod = null ) =>
			new BlobFileManagerSetup(
				displaySetup,
				classes,
				thumbnailResourceGetter,
				omitNoExistingFileMessage,
				uploadValidationPredicate,
				uploadValidationErrorNotifier,
				uploadValidationMethod );

		internal readonly DisplaySetup DisplaySetup;
		internal readonly ElementClassSet Classes;
		internal readonly Func<int, ResourceInfo> ThumbnailResourceGetter;
		internal readonly bool OmitNoExistingFileMessage;
		internal readonly Func<bool, bool> UploadValidationPredicate;
		internal readonly Action UploadValidationErrorNotifier;
		internal readonly Action<RsFile, Validator> UploadValidationMethod;

		internal BlobFileManagerSetup(
			DisplaySetup displaySetup, ElementClassSet classes, Func<int, ResourceInfo> thumbnailResourceGetter, bool omitNoExistingFileMessage,
			Func<bool, bool> uploadValidationPredicate, Action uploadValidationErrorNotifier, Action<RsFile, Validator> uploadValidationMethod ) {
			DisplaySetup = displaySetup;
			Classes = classes;
			ThumbnailResourceGetter = thumbnailResourceGetter;
			OmitNoExistingFileMessage = omitNoExistingFileMessage;
			UploadValidationPredicate = uploadValidationPredicate;
			UploadValidationErrorNotifier = uploadValidationErrorNotifier;
			UploadValidationMethod = uploadValidationMethod;
		}
	}
}