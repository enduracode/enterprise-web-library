// EwlResource

namespace EnterpriseWebLibrary.Website.TestPages;

partial class LegacyUrlFolderSetup {
	protected override UrlHandler getUrlParent() => LegacyUrlStatics.GetParent();
	public override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

	protected override IEnumerable<UrlPattern> getChildUrlPatterns() {
		var patterns = new List<UrlPattern>();
		patterns.Add(
			new UrlPattern(
				encoder => encoder is ActionControls.UrlEncoder ? EncodingUrlSegment.Create( "ActionControls.aspx" ) : null,
				url => string.Equals( url.Segment, "ActionControls.aspx", StringComparison.OrdinalIgnoreCase ) ? new ActionControls.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is BoxDemo.UrlEncoder ? EncodingUrlSegment.Create( "BoxDemo.aspx" ) : null,
				url => string.Equals( url.Segment, "BoxDemo.aspx", StringComparison.OrdinalIgnoreCase ) ? new BoxDemo.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is Charts.UrlEncoder ? EncodingUrlSegment.Create( "Charts.aspx" ) : null,
				url => string.Equals( url.Segment, "Charts.aspx", StringComparison.OrdinalIgnoreCase ) ? new Charts.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is Checkboxes.UrlEncoder ? EncodingUrlSegment.Create( "Checkboxes.aspx" ) : null,
				url => string.Equals( url.Segment, "Checkboxes.aspx", StringComparison.OrdinalIgnoreCase ) ? new Checkboxes.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is CheckboxListDemo.UrlEncoder ? EncodingUrlSegment.Create( "CheckboxListDemo.aspx" ) : null,
				url => string.Equals( url.Segment, "CheckboxListDemo.aspx", StringComparison.OrdinalIgnoreCase ) ? new CheckboxListDemo.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is ColumnPrimaryTableDemo.UrlEncoder ? EncodingUrlSegment.Create( "ColumnPrimaryTableDemo.aspx" ) : null,
				url => string.Equals( url.Segment, "ColumnPrimaryTableDemo.aspx", StringComparison.OrdinalIgnoreCase )
					       ? new ColumnPrimaryTableDemo.UrlDecoder()
					       : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is ComponentLists.UrlEncoder ? EncodingUrlSegment.Create( "ComponentLists.aspx" ) : null,
				url => string.Equals( url.Segment, "ComponentLists.aspx", StringComparison.OrdinalIgnoreCase ) ? new ComponentLists.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is EwfTableDemo.UrlEncoder ? EncodingUrlSegment.Create( "EwfTableDemo.aspx" ) : null,
				url => string.Equals( url.Segment, "EwfTableDemo.aspx", StringComparison.OrdinalIgnoreCase ) ? new EwfTableDemo.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is GetImage.UrlEncoder ? EncodingUrlSegment.Create( "GetImage.aspx" ) : null,
				url => string.Equals( url.Segment, "GetImage.aspx", StringComparison.OrdinalIgnoreCase ) ? new GetImage.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is HtmlEditing.UrlEncoder ? EncodingUrlSegment.Create( "HtmlEditing.aspx" ) : null,
				url => string.Equals( url.Segment, "HtmlEditing.aspx", StringComparison.OrdinalIgnoreCase ) ? new HtmlEditing.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is IntermediatePostBacks.UrlEncoder ? EncodingUrlSegment.Create( "IntermediatePostBacks.aspx" ) : null,
				url => string.Equals( url.Segment, "IntermediatePostBacks.aspx", StringComparison.OrdinalIgnoreCase )
					       ? new IntermediatePostBacks.UrlDecoder()
					       : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is MailMerging.UrlEncoder ? EncodingUrlSegment.Create( "MailMerging.aspx" ) : null,
				url => string.Equals( url.Segment, "MailMerging.aspx", StringComparison.OrdinalIgnoreCase ) ? new MailMerging.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is ModalBoxes.UrlEncoder ? EncodingUrlSegment.Create( "ModalBoxes.aspx" ) : null,
				url => string.Equals( url.Segment, "ModalBoxes.aspx", StringComparison.OrdinalIgnoreCase ) ? new ModalBoxes.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is NumberControlDemo.UrlEncoder ? EncodingUrlSegment.Create( "NumberControlDemo.aspx" ) : null,
				url => string.Equals( url.Segment, "NumberControlDemo.aspx", StringComparison.OrdinalIgnoreCase ) ? new NumberControlDemo.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is OmniDemo.UrlEncoder ? EncodingUrlSegment.Create( "OmniDemo.aspx" ) : null,
				url => string.Equals( url.Segment, "OmniDemo.aspx", StringComparison.OrdinalIgnoreCase ) ? new OmniDemo.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is OptionalParametersDemo.UrlEncoder ? EncodingUrlSegment.Create( "OptionalParameters.aspx" ) : null,
				url => string.Equals( url.Segment, "OptionalParameters.aspx", StringComparison.OrdinalIgnoreCase ) ? new OptionalParametersDemo.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is RegexHelper.UrlEncoder ? EncodingUrlSegment.Create( "RegexHelper.aspx" ) : null,
				url => string.Equals( url.Segment, "RegexHelper.aspx", StringComparison.OrdinalIgnoreCase ) ? new RegexHelper.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is SelectListDemo.UrlEncoder ? EncodingUrlSegment.Create( "SelectListDemo.aspx" ) : null,
				url => string.Equals( url.Segment, "SelectListDemo.aspx", StringComparison.OrdinalIgnoreCase ) ? new SelectListDemo.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is StatusMessages.UrlEncoder ? EncodingUrlSegment.Create( "StatusMessages.aspx" ) : null,
				url => string.Equals( url.Segment, "StatusMessages.aspx", StringComparison.OrdinalIgnoreCase ) ? new StatusMessages.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is TestService.UrlEncoder ? EncodingUrlSegment.Create( "TestService.aspx" ) : null,
				url => string.Equals( url.Segment, "TestService.aspx", StringComparison.OrdinalIgnoreCase ) ? new TestService.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is TextControlDemo.UrlEncoder ? EncodingUrlSegment.Create( "TextControlDemo.aspx" ) : null,
				url => string.Equals( url.Segment, "TextControlDemo.aspx", StringComparison.OrdinalIgnoreCase ) ? new TextControlDemo.UrlDecoder() : null ) );
		patterns.Add( Basic.LegacyUrlFolderSetup.UrlPatterns.Literal( "Basic" ) );
		patterns.Add( SubFolder.LegacyUrlFolderSetup.UrlPatterns.Literal( "SubFolder" ) );
		return patterns;
	}
}