using System.Text.RegularExpressions;
using EnterpriseWebLibrary.Email;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

[ PublicAPI ]
public static class EmailSendingFormItems {
	public static FormItem GetSubjectFormItem( this EmailMessage emailMessage, TextControlSetup? controlSetup = null, string value = "" ) =>
		new TextControl(
			value,
			false,
			setup: controlSetup,
			validationMethod: ( postBackValue, validator ) => {
				emailMessage.Subject = postBackValue;
				if( Regex.Match( emailMessage.Subject, RegularExpressions.HtmlTag, RegexOptions.IgnoreCase ).Success )
					validator.NoteErrorAndAddMessage( "HTML is not allowed in the subject field." );
			} ).ToFormItem( label: "Subject".ToComponents() );

	public static FormItem GetBodyHtmlFormItem( this EmailMessage emailMessage, WysiwygHtmlEditorSetup? editorSetup = null, string value = "" ) =>
		new WysiwygHtmlEditor( value, true, ( postBackValue, _ ) => emailMessage.BodyHtml = postBackValue, setup: editorSetup ).ToFormItem(
			label: "Body".ToComponents() );
}