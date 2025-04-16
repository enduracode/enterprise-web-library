using System.Text.RegularExpressions;
using EnterpriseWebLibrary.Email;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

[ PublicAPI ]
public static class EmailSendingFormItems {
	public static FormItem GetSubjectFormItem(
		this EmailMessage message, IReadOnlyCollection<PhrasingComponent>? label = null, TextControlSetup? controlSetup = null, string? value = null ) {
		var dataValue = new DataValue<string>( message.Subject.Length > 0, () => message.Subject );
		return dataValue.ToTextControl(
				false,
				setup: controlSetup,
				value: value,
				additionalValidationMethod: validator => {
					if( Regex.Match( message.Subject, RegularExpressions.HtmlTag, RegexOptions.IgnoreCase ).Success )
						validator.NoteErrorAndAddMessage( "HTML is not allowed in the subject field." );
					else
						message.Subject = dataValue.Value;
				} )
			.ToFormItem( label: label ?? "Subject".ToComponents() );
	}

	public static FormItem GetBodyHtmlFormItem(
		this EmailMessage message, bool allowEmpty, IReadOnlyCollection<PhrasingComponent>? label = null, WysiwygHtmlEditorSetup? editorSetup = null,
		string? value = null ) {
		var dataValue = new DataValue<string>( message.BodyHtml.Length > 0, () => message.BodyHtml );
		return dataValue.ToHtmlEditor( allowEmpty, setup: editorSetup, value: value, additionalValidationMethod: _ => message.BodyHtml = dataValue.Value )
			.ToFormItem( label: label ?? "Body".ToComponents() );
	}
}