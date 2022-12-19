using System.Web;
using EnterpriseWebLibrary.Email;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The behavior for a hyperlink.
	/// </summary>
	public sealed class HyperlinkBehavior {
		public static implicit operator HyperlinkBehavior( ResourceInfo destination ) => new( destination, false, false, "", null );

		private readonly bool hasDestination;
		private readonly Func<bool> userCanNavigateToDestinationPredicate;

		internal readonly ElementClassSet Classes;
		internal readonly Func<bool, IReadOnlyCollection<ElementAttribute>> AttributeGetter;
		internal readonly Lazy<string> Url;
		internal readonly Func<bool, bool> IncludesIdAttribute;
		internal readonly IReadOnlyCollection<EtherealComponent> EtherealChildren;
		internal readonly Func<string, bool, string> JsInitStatementGetter;
		internal readonly bool IsFocusable;
		internal readonly Action PostBackAdder;

		internal HyperlinkBehavior(
			ResourceInfo destination, bool disableAuthorizationCheck, bool prerenderDestination, string target, Func<string, string> actionStatementGetter ) {
			hasDestination = destination != null;
			userCanNavigateToDestinationPredicate = () => !hasDestination || disableAuthorizationCheck || destination.UserCanAccessResource;

			var destinationAlternativeMode = hasDestination && !disableAuthorizationCheck ? destination.AlternativeMode : null;
			Classes = destinationAlternativeMode is NewContentResourceMode ? ActionComponentCssElementCreator.NewContentClass : ElementClassSet.Empty;

			Url = new Lazy<string>( () => hasDestination ? destination.GetUrl( !disableAuthorizationCheck, false ) : "" );
			var isPostBackHyperlink = new Lazy<bool>(
				() => hasDestination && !( destinationAlternativeMode is DisabledResourceMode ) && !target.Any() && PageBase.Current.IsAutoDataUpdater.Value );
			AttributeGetter = forNonHyperlinkElement =>
				( hasDestination && !forNonHyperlinkElement ? new ElementAttribute( "href", Url.Value ).ToCollection() : Enumerable.Empty<ElementAttribute>() ).Concat(
					hasDestination && target.Any() && !forNonHyperlinkElement
						? new ElementAttribute( "target", target ).ToCollection()
						: Enumerable.Empty<ElementAttribute>() )
				.Concat(
					// for prerendering and https://instant.page/
					!isPostBackHyperlink.Value && destination is ResourceBase && !( destinationAlternativeMode is DisabledResourceMode ) && !forNonHyperlinkElement
						? new ElementAttribute(
							prerenderDestination
								? "data-{0}-prerender".FormatWith( EwlStatics.EwlInitialism.ToLowerInvariant() ) /* duplicated in JavaScript file */
								: "data-instant" ).ToCollection()
						: Enumerable.Empty<ElementAttribute>() )
				.Materialize();

			FormAction postBackAction = null;
			string getActionInitStatements( string id, bool omitPreventDefaultStatement, string actionStatements ) =>
				"$( '#{0}' ).click( function( e ) {{ {1} }} );".FormatWith(
					id,
					( omitPreventDefaultStatement ? "" : "e.preventDefault();" ).ConcatenateWithSpace( actionStatements ) );
			if( destinationAlternativeMode is DisabledResourceMode disabledResourceMode ) {
				IncludesIdAttribute = _ => true;
				EtherealChildren = new ToolTip(
					( disabledResourceMode.Message.Any() ? disabledResourceMode.Message : Translation.ThePageYouRequestedIsDisabled ).ToComponents(),
					out var toolTipInitStatementGetter ).ToCollection();
				JsInitStatementGetter = ( id, forNonHyperlinkElement ) =>
					( forNonHyperlinkElement ? "" : getActionInitStatements( id, false, "" ) + " " ) + toolTipInitStatementGetter( id );
			}
			else {
				IncludesIdAttribute = forNonHyperlinkElement =>
					isPostBackHyperlink.Value || ( hasDestination && ( actionStatementGetter != null || forNonHyperlinkElement ) );
				EtherealChildren = null;
				JsInitStatementGetter = ( id, forNonHyperlinkElement ) => {
					var actionStatements = isPostBackHyperlink.Value
						                       ? postBackAction.GetJsStatements()
						                       :
						                       hasDestination && actionStatementGetter != null
							                       ? actionStatementGetter( Url.Value )
							                       :
							                       hasDestination && forNonHyperlinkElement
								                       ?
								                       !target.Any() ? "window.location.href = '{0}';".FormatWith( Url.Value ) :
								                       target == "_parent" ? "window.parent.location.href = '{0}';".FormatWith( Url.Value ) :
								                       "window.open( '{0}', '{1}' );".FormatWith( Url.Value, target )
								                       : "";
					return actionStatements.Any() ? getActionInitStatements( id, forNonHyperlinkElement, actionStatements ) : "";
				};
			}

			IsFocusable = hasDestination;

			PostBackAdder = () => {
				if( !isPostBackHyperlink.Value )
					return;
				var postBackId = PostBack.GetCompositeId( "hyperlink", destination.GetUrl(), disableAuthorizationCheck.ToString() );
				postBackAction = new PostBackFormAction(
					PageBase.Current.GetPostBack( postBackId ) ?? PostBack.CreateFull(
						id: postBackId,
						actionGetter: () => new PostBackAction( destination, authorizationCheckDisabledPredicate: _ => disableAuthorizationCheck ) ) );
				postBackAction.AddToPageIfNecessary();
			};
		}

		internal HyperlinkBehavior( string mailtoUri ) {
			hasDestination = true;
			userCanNavigateToDestinationPredicate = () => true;

			Classes = ElementClassSet.Empty;
			Url = new Lazy<string>( () => "mailto:{0}".FormatWith( mailtoUri ) );
			AttributeGetter = forNonHyperlinkElement =>
				forNonHyperlinkElement ? Enumerable.Empty<ElementAttribute>().Materialize() : new ElementAttribute( "href", Url.Value ).ToCollection();
			IncludesIdAttribute = forNonHyperlinkElement => forNonHyperlinkElement;
			EtherealChildren = null;
			JsInitStatementGetter = ( _, forNonHyperlinkElement ) => forNonHyperlinkElement ? "window.location.href = '{0}';".FormatWith( Url.Value ) : "";
			IsFocusable = true;
			PostBackAdder = () => {};
		}

		/// <summary>
		/// Gets whether the behavior object has a destination.
		/// </summary>
		public bool HasDestination => hasDestination;

		public bool UserCanNavigateToDestination() => userCanNavigateToDestinationPredicate();
	}

	public static class HyperlinkBehaviorExtensionCreators {
		private static Func<BrowsingContextSetup, string, string> browsingModalBoxOpenStatementGetter;

		internal static void Init( Func<BrowsingContextSetup, string, string> browsingModalBoxOpenStatementGetter ) {
			HyperlinkBehaviorExtensionCreators.browsingModalBoxOpenStatementGetter = browsingModalBoxOpenStatementGetter;
		}

		/// <summary>
		/// Creates a behavior object that navigates to this resource in the default way. If you don’t need to pass any arguments, don’t use this method; resource
		/// info objects are implicitly converted to hyperlink behavior objects.
		/// </summary>
		/// <param name="destination">Where to navigate. Specify null if you don’t want the link to do anything.</param>
		/// <param name="disableAuthorizationCheck">Pass true to allow navigation to a resource that the authenticated user cannot access. Use with caution.</param>
		/// <param name="prerenderDestination">Pass true to encourage the browser to prerender the destination if possible.</param>
		public static HyperlinkBehavior ToHyperlinkDefaultBehavior(
			this ResourceInfo destination, bool disableAuthorizationCheck = false, bool prerenderDestination = false ) =>
			new( destination, disableAuthorizationCheck, prerenderDestination, "", null );

		/// <summary>
		/// Creates a behavior object that navigates to this resource in a new tab or window.
		/// </summary>
		/// <param name="destination">Where to navigate. Specify null if you don’t want the link to do anything.</param>
		/// <param name="disableAuthorizationCheck">Pass true to allow navigation to a resource that the authenticated user cannot access. Use with caution.</param>
		public static HyperlinkBehavior ToHyperlinkNewTabBehavior( this ResourceInfo destination, bool disableAuthorizationCheck = false ) =>
			new( destination, disableAuthorizationCheck, false, "_blank", null );

		/// <summary>
		/// Creates a behavior object that navigates to this resource in a modal box.
		/// </summary>
		/// <param name="destination">Where to navigate. Specify null if you don’t want the link to do anything.</param>
		/// <param name="disableAuthorizationCheck">Pass true to allow navigation to a resource that the authenticated user cannot access. Use with caution.</param>
		/// <param name="browsingContextSetup">The setup object for the browsing context (i.e. the iframe).</param>
		public static HyperlinkBehavior ToHyperlinkModalBoxBehavior(
			this ResourceInfo destination, bool disableAuthorizationCheck = false, BrowsingContextSetup browsingContextSetup = null ) =>
			new( destination, disableAuthorizationCheck, false, "_blank", url => browsingModalBoxOpenStatementGetter( browsingContextSetup, url ) );

		/// <summary>
		/// Creates a behavior object that navigates to this resource in the parent browsing context.
		/// </summary>
		/// <param name="destination">Where to navigate. Specify null if you don’t want the link to do anything.</param>
		/// <param name="disableAuthorizationCheck">Pass true to allow navigation to a resource that the authenticated user cannot access. Use with caution.</param>
		public static HyperlinkBehavior ToHyperlinkParentContextBehavior( this ResourceInfo destination, bool disableAuthorizationCheck = false ) =>
			new( destination, disableAuthorizationCheck, false, "_parent", null );

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