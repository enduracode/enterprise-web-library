using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using EnterpriseWebLibrary.IO;
using Tewl.InputValidation;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A file-upload control.
	/// </summary>
	public class FileUpload: FormControl<PhrasingComponent> {
		private static Action formMultipartEncodingSetter;

		internal static void Init( Action formMultipartEncodingSetter ) {
			FileUpload.formMultipartEncodingSetter = formMultipartEncodingSetter;
		}

		public FormControlLabeler Labeler { get; }
		public PhrasingComponent PageComponent { get; }
		public EwfValidation Validation { get; }

		/// <summary>
		/// Creates a file-upload control.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
		public FileUpload(
			DisplaySetup displaySetup = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null,
			Action<RsFile, Validator> validationMethod = null ) {
			Labeler = new FormControlLabeler();

			var id = new ElementId();
			var formValue = new FormValue<HttpPostedFile>(
				() => null,
				() => id.Id,
				v => "",
				rawValue => rawValue == null
					            ? PostBackValueValidationResult<HttpPostedFile>.CreateInvalid()
					            : PostBackValueValidationResult<HttpPostedFile>.CreateValid( rawValue.ContentLength > 0 ? rawValue : null ) );

			PageComponent = new CustomPhrasingComponent(
				new DisplayableElement(
					context => {
						formMultipartEncodingSetter();
						return new DisplayableElementData(
							displaySetup,
							() => {
								var attributes = new List<Tuple<string, string>>();
								attributes.Add( Tuple.Create( "type", "file" ) );
								attributes.Add( Tuple.Create( "name", context.Id ) );

								return new DisplayableElementLocalData(
									"input",
									new FocusabilityCondition( true ),
									isFocused => {
										if( isFocused )
											attributes.Add( Tuple.Create( "autofocus", "autofocus" ) );
										return new DisplayableElementFocusDependentData( attributes: attributes );
									} );
							},
							clientSideIdReferences: id.ToCollection().Append( Labeler.ControlId ) );
					},
					formValue: formValue ).ToCollection() );

			if( validationMethod != null )
				Validation = formValue.CreateValidation(
					( postBackValue, validator ) => {
						if( validationPredicate != null && !validationPredicate( postBackValue.ChangedOnPostBack ) )
							return;
						validationMethod( getRsFile( postBackValue.Value ), validator );
					} );
		}

		private RsFile getRsFile( HttpPostedFile file ) {
			if( file == null )
				return null;
			using( var ms = new MemoryStream() ) {
				file.InputStream.CopyTo( ms );
				return new RsFile( ms.ToArray(), Path.GetFileName( file.FileName ), contentType: file.ContentType );
			}
		}
	}
}