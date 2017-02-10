using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.JavaScriptWriting;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The behavior for a hyperlink.
	/// </summary>
	public sealed class HyperlinkBehavior {
		public static implicit operator HyperlinkBehavior( ResourceInfo destination ) {
			return new HyperlinkBehavior( destination, "" );
		}

		internal static PostBack GetHyperlinkPostBack( ResourceInfo destination ) {
			var id = PostBack.GetCompositeId( "ewfLink", destination.GetUrl() );
			return EwfPage.Instance.GetPostBack( id ) ?? PostBack.CreateFull( id: id, actionGetter: () => new PostBackAction( destination ) );
		}

		private readonly ResourceInfo destination;
		internal readonly ElementClassSet Classes;
		internal readonly Func<IReadOnlyCollection<Tuple<string, string>>> AttributeGetter;
		internal readonly string Url;
		internal readonly bool IncludeIdAttribute;
		internal readonly IReadOnlyCollection<EtherealComponent> EtherealChildren;
		internal readonly Func<string, string> JsInitStatementGetter;
		internal readonly Action PostBackAdder;

		internal HyperlinkBehavior( ResourceInfo destination, string target ) {
			this.destination = destination;
			Classes = destination?.AlternativeMode is NewContentResourceMode ? ActionComponentCssElementCreator.NewContentClass : ElementClassSet.Empty;

			Url = destination != null ? destination.GetUrl( true, false, true ) : "";
			var isPostBackHyperlink = destination != null && !( destination.AlternativeMode is DisabledResourceMode ) && !target.Any() &&
			                          EwfPage.Instance.IsAutoDataUpdater;
			PostBack postBack = null;
			AttributeGetter =
				() =>
				( Url.Any() ? Tuple.Create( "href", Url ).ToCollection() : Enumerable.Empty<Tuple<string, string>>() ).Concat(
					target.Any() ? Tuple.Create( "target", target ).ToCollection() : Enumerable.Empty<Tuple<string, string>>() )
					.Concat(
						isPostBackHyperlink
							? Tuple.Create( JsWritingMethods.onclick, EwfPage.GetPostBackScript( postBack ) ).ToCollection()
							: Enumerable.Empty<Tuple<string, string>>() )
					.ToImmutableArray();

			var disabledResourceMode = destination?.AlternativeMode as DisabledResourceMode;
			if( disabledResourceMode != null ) {
				IncludeIdAttribute = true;
				Func<string, string> toolTipInitStatementGetter;
				EtherealChildren =
					new ToolTip(
						( disabledResourceMode.Message.Any() ? disabledResourceMode.Message : Translation.ThePageYouRequestedIsDisabled ).ToComponent().ToCollection(),
						out toolTipInitStatementGetter ).ToCollection();
				JsInitStatementGetter = id => "$( '#{0}' ).click( function( e ) { e.preventDefault(); } );".FormatWith( id ) + toolTipInitStatementGetter( id );
			}
			else {
				EtherealChildren = ImmutableArray<EtherealComponent>.Empty;
				JsInitStatementGetter = id => "";
			}

			if( isPostBackHyperlink )
				PostBackAdder = () => {
					postBack = GetHyperlinkPostBack( destination );
					EwfPage.Instance.AddPostBack( postBack );
				};
			else
				PostBackAdder = () => { };
		}

		internal HyperlinkBehavior( string mailtoUri ) {
			Classes = ElementClassSet.Empty;
			Url = "mailto:{0}".FormatWith( mailtoUri );
			AttributeGetter = () => Tuple.Create( "href", Url ).ToCollection();
			EtherealChildren = ImmutableArray<EtherealComponent>.Empty;
			JsInitStatementGetter = id => "";
			PostBackAdder = () => { };
		}

		public bool UserCanNavigateToDestination() {
			return destination == null || destination.UserCanAccessResource;
		}
	}

	public static class HyperlinkBehaviorExtensionCreators {
		/// <summary>
		/// Creates a behavior object that navigates to this resource in a new tab or window.
		/// </summary>
		/// <param name="destination">Where to navigate. Specify null if you don't want the link to do anything.</param>
		/// <returns></returns>
		public static HyperlinkBehavior ToHyperlinkNewTabBehavior( this ResourceInfo destination ) {
			return new HyperlinkBehavior( destination, "_blank" );
		}

		/// <summary>
		/// Creates a behavior object that will create an email message to this address.
		/// </summary>
		/// <param name="toAddress">Address to appear in the To: field. Do not pass null or the empty string.</param>
		/// <param name="ccAddress">Address to appear in the CC: field.</param>
		/// <param name="bccAddress">Address to appear in the BCC: field.</param>
		/// <param name="subject">Text to appear in the subject field.</param>
		/// <param name="body">Message to appear in the body.</param>
		public static HyperlinkBehavior ToHyperlinkBehavior(
			this EmailAddress toAddress, string ccAddress = "", string bccAddress = "", string subject = "", string body = "" ) {
			return
				new HyperlinkBehavior(
					StringTools.ConcatenateWithDelimiter(
						"?",
						toAddress.Address,
						StringTools.ConcatenateWithDelimiter(
							"&",
							ccAddress.PrependDelimiter( "cc=" ),
							bccAddress.PrependDelimiter( "bcc=" ),
							HttpUtility.UrlEncode( subject ).PrependDelimiter( "subject=" ),
							HttpUtility.UrlEncode( body ).PrependDelimiter( "body=" ) ) ) );
		}
	}
}