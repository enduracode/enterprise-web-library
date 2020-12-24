using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EnterpriseWebLibrary.Email;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The behavior for a hyperlink.
	/// </summary>
	public sealed class HyperlinkBehavior {
		public static implicit operator HyperlinkBehavior( ResourceInfo destination ) {
			return new HyperlinkBehavior( destination, "", null );
		}

		internal static FormAction GetHyperlinkPostBackAction( ResourceInfo destination ) {
			var id = PostBack.GetCompositeId( "ewfLink", destination.GetUrl() );
			return new PostBackFormAction(
				EwfPage.Instance.GetPostBack( id ) ?? PostBack.CreateFull( id: id, actionGetter: () => new PostBackAction( destination ) ) );
		}

		private readonly ResourceInfo destination;
		internal readonly ElementClassSet Classes;
		internal readonly Func<bool, IReadOnlyCollection<Tuple<string, string>>> AttributeGetter;
		internal readonly Lazy<string> Url;
		internal readonly Func<bool, bool> IncludesIdAttribute;
		internal readonly IReadOnlyCollection<EtherealComponent> EtherealChildren;
		internal readonly Func<string, bool, string> JsInitStatementGetter;
		internal readonly bool IsFocusable;
		internal readonly Action PostBackAdder;

		internal HyperlinkBehavior( ResourceInfo destination, string target, Func<string, string> actionStatementGetter ) {
			this.destination = destination;
			Classes = destination?.AlternativeMode is NewContentResourceMode ? ActionComponentCssElementCreator.NewContentClass : ElementClassSet.Empty;

			Url = new Lazy<string>( () => destination != null ? destination.GetUrl( true, false, true ) : "" );
			var isPostBackHyperlink = new Lazy<bool>(
				() => destination != null && !( destination.AlternativeMode is DisabledResourceMode ) && !target.Any() && EwfPage.Instance.IsAutoDataUpdater.Value );
			AttributeGetter = forNonHyperlinkElement =>
				( destination != null && !forNonHyperlinkElement ? Tuple.Create( "href", Url.Value ).ToCollection() : Enumerable.Empty<Tuple<string, string>>() )
				.Concat(
					destination != null && target.Any() && !forNonHyperlinkElement
						? Tuple.Create( "target", target ).ToCollection()
						: Enumerable.Empty<Tuple<string, string>>() )
				.Concat(
					// for https://instant.page/
					!isPostBackHyperlink.Value && destination is ResourceBase && !( destination.AlternativeMode is DisabledResourceMode ) && !forNonHyperlinkElement
						? Tuple.Create( "data-instant", "data-instant" ).ToCollection()
						: Enumerable.Empty<Tuple<string, string>>() )
				.Materialize();

			FormAction postBackAction = null;
			string getActionInitStatements( string id, bool omitPreventDefaultStatement, string actionStatements ) =>
				"$( '#{0}' ).click( function( e ) {{ {1} }} );".FormatWith(
					id,
					( omitPreventDefaultStatement ? "" : "e.preventDefault();" ).ConcatenateWithSpace( actionStatements ) );
			if( destination?.AlternativeMode is DisabledResourceMode disabledResourceMode ) {
				IncludesIdAttribute = forNonHyperlinkElement => true;
				EtherealChildren = new ToolTip(
					( disabledResourceMode.Message.Any() ? disabledResourceMode.Message : Translation.ThePageYouRequestedIsDisabled ).ToComponents(),
					out var toolTipInitStatementGetter ).ToCollection();
				JsInitStatementGetter = ( id, forNonHyperlinkElement ) =>
					( forNonHyperlinkElement ? "" : getActionInitStatements( id, false, "" ) + " " ) + toolTipInitStatementGetter( id );
			}
			else {
				IncludesIdAttribute = forNonHyperlinkElement =>
					isPostBackHyperlink.Value || ( destination != null && ( actionStatementGetter != null || forNonHyperlinkElement ) );
				EtherealChildren = null;
				JsInitStatementGetter = ( id, forNonHyperlinkElement ) => {
					var actionStatements = isPostBackHyperlink.Value ? postBackAction.GetJsStatements() :
					                       destination != null && actionStatementGetter != null ? actionStatementGetter( Url.Value ) :
					                       destination != null && forNonHyperlinkElement ? !target.Any()
						                                                                       ? "window.location.href = '{0}';".FormatWith( Url.Value )
						                                                                       :
						                                                                       target == "_parent"
							                                                                       ?
							                                                                       "window.parent.location.href = '{0}';".FormatWith( Url.Value )
							                                                                       : "window.open( '{0}', '{1}' );".FormatWith( Url.Value, target ) : "";
					return actionStatements.Any() ? getActionInitStatements( id, forNonHyperlinkElement, actionStatements ) : "";
				};
			}

			IsFocusable = destination != null;

			PostBackAdder = () => {
				if( !isPostBackHyperlink.Value )
					return;
				postBackAction = GetHyperlinkPostBackAction( destination );
				postBackAction.AddToPageIfNecessary();
			};
		}

		internal HyperlinkBehavior( string mailtoUri ) {
			Classes = ElementClassSet.Empty;
			Url = new Lazy<string>( () => "mailto:{0}".FormatWith( mailtoUri ) );
			AttributeGetter = forNonHyperlinkElement =>
				forNonHyperlinkElement ? Enumerable.Empty<Tuple<string, string>>().Materialize() : Tuple.Create( "href", Url.Value ).ToCollection();
			IncludesIdAttribute = forNonHyperlinkElement => forNonHyperlinkElement;
			EtherealChildren = null;
			JsInitStatementGetter = ( id, forNonHyperlinkElement ) => forNonHyperlinkElement ? "window.location.href = '{0}';".FormatWith( Url.Value ) : "";
			IsFocusable = true;
			PostBackAdder = () => {};
		}

		internal bool HasDestination() => destination != null;
		public bool UserCanNavigateToDestination() => destination == null || destination.UserCanAccessResource;
	}

	public static class HyperlinkBehaviorExtensionCreators {
		private static Func<BrowsingContextSetup, string, string> browsingModalBoxOpenStatementGetter;

		internal static void Init( Func<BrowsingContextSetup, string, string> browsingModalBoxOpenStatementGetter ) {
			HyperlinkBehaviorExtensionCreators.browsingModalBoxOpenStatementGetter = browsingModalBoxOpenStatementGetter;
		}

		/// <summary>
		/// Creates a behavior object that navigates to this resource in a new tab or window.
		/// </summary>
		/// <param name="destination">Where to navigate. Specify null if you don't want the link to do anything.</param>
		public static HyperlinkBehavior ToHyperlinkNewTabBehavior( this ResourceInfo destination ) {
			return new HyperlinkBehavior( destination, "_blank", null );
		}

		/// <summary>
		/// Creates a behavior object that navigates to this resource in a modal box.
		/// </summary>
		/// <param name="destination">Where to navigate. Specify null if you don't want the link to do anything.</param>
		/// <param name="browsingContextSetup">The setup object for the browsing context (i.e. the iframe).</param>
		public static HyperlinkBehavior ToHyperlinkModalBoxBehavior( this ResourceInfo destination, BrowsingContextSetup browsingContextSetup = null ) {
			return new HyperlinkBehavior( destination, "_blank", url => browsingModalBoxOpenStatementGetter( browsingContextSetup, url ) );
		}

		/// <summary>
		/// Creates a behavior object that navigates to this resource in the parent browsing context.
		/// </summary>
		/// <param name="destination">Where to navigate. Specify null if you don't want the link to do anything.</param>
		public static HyperlinkBehavior ToHyperlinkParentContextBehavior( this ResourceInfo destination ) {
			return new HyperlinkBehavior( destination, "_parent", null );
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
			return new HyperlinkBehavior(
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