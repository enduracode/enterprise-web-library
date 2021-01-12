using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EnterpriseWebLibrary.Configuration;
using Humanizer;
using StackExchange.Profiling;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class BasicPageContent: PageContent {
		private static Func<IReadOnlyCollection<PageContent>, IEnumerable<ResourceInfo>> cssInfoCreator;

		internal static void Init( Func<IReadOnlyCollection<PageContent>, IEnumerable<ResourceInfo>> cssInfoCreator ) {
			BasicPageContent.cssInfoCreator = cssInfoCreator;
		}

		internal static ( PageContent, FlowComponent, FlowComponent, FlowComponent, Action, bool ) GetContent(
			PageContent content, Func<string> hiddenFieldValueGetter, Func<string> jsInitStatementGetter ) {
			var contentObjects = new List<PageContent>();
			while( !( content is BasicPageContent ) ) {
				contentObjects.Add( content );
				content = content.GetContent();
			}
			var basicContent = (BasicPageContent)content;

			var jsInitElement = getJsInitElement( jsInitStatementGetter );
			return ( basicContent, basicContent.componentGetter( contentObjects, hiddenFieldValueGetter, jsInitElement ), basicContent.etherealContainer,
				       jsInitElement, basicContent.dataUpdateModificationMethod, basicContent.isAutoDataUpdater );
		}

		private static FlowComponent getJsInitElement( Func<string> jsInitStatementGetter ) =>
			new ElementComponent(
				context => new ElementData(
					() => new ElementLocalData(
						"script",
						focusDependentData: new ElementFocusDependentData(
							attributes: Tuple.Create(
									"src",
									"data:{0};charset=utf-8;base64,{1}".FormatWith(
										TewlContrib.ContentTypes.JavaScript,
										Convert.ToBase64String(
											Encoding.UTF8.GetBytes( "window.addEventListener( 'DOMContentLoaded', function() { " + jsInitStatementGetter() + " } );" ) ) ) )
								.ToCollection()
								.Append( Tuple.Create( "defer", "" ) ) ) ) ) );

		private readonly Func<IReadOnlyCollection<PageContent>, Func<string>, FlowComponent, FlowComponent> componentGetter;
		internal bool FormUsesMultipartEncoding;
		private readonly List<FlowComponent> bodyContent = new List<FlowComponent>();
		private readonly FlowComponent etherealContainer;
		internal readonly ModalBoxId BrowsingModalBoxId = new ModalBoxId();
		private readonly List<EtherealComponent> etherealContent = new List<EtherealComponent>();
		private readonly Action dataUpdateModificationMethod;
		private readonly bool isAutoDataUpdater;

		/// <summary>
		/// Creates a basic page content object.
		/// </summary>
		/// <param name="titleOverride">Do not pass null.</param>
		/// <param name="customHeadElements"></param>
		/// <param name="bodyClasses"></param>
		/// <param name="dataUpdateModificationMethod">The modification method for the page’s data-update modification.</param>
		/// <param name="isAutoDataUpdater">Pass true to force a post-back when a hyperlink is clicked.</param>
		public BasicPageContent(
			string titleOverride = "", TrustedHtmlString customHeadElements = null, ElementClassSet bodyClasses = null, Action dataUpdateModificationMethod = null,
			bool isAutoDataUpdater = false ) {
			var preContentComponents = BasicPage.GetPreContentComponents();
			var postContentComponents = BasicPage.GetPostContentComponents();

			var statusMessageModalBox = BasicPage.GetStatusMessageModalBox();

			componentGetter = ( contentObjects, hiddenFieldValueGetter, jsInitElement ) => new ElementComponent(
				documentContext => new ElementData(
					() => new ElementLocalData( "html" ),
					children: new ElementComponent(
							headContext => new ElementData(
								() => new ElementLocalData( "head" ),
								children: new ElementComponent(
										titleContext => new ElementData(
											() => new ElementLocalData( "title" ),
											children: ( titleOverride.Any() ? titleOverride : getTitle() ).ToComponents() ) ).Concat( getMetadataAndFaviconLinks() )
									.Concat( getTypekitLogicIfNecessary() )
									.Concat(
										from i in cssInfoCreator( contentObjects ) select getStyleSheetLink( EwfPage.Instance.GetClientUrl( i.GetUrl( false, false, false ) ) ) )
									.Append( getModernizrLogic() )
									.Concat( getGoogleAnalyticsLogicIfNecessary() )
									.Concat( getJavaScriptIncludes() )
									.Append( ( customHeadElements ?? new TrustedHtmlString( "" ) ).ToComponent() )
									.Materialize() ) ).Append(
							new ElementComponent(
								bodyContext => new ElementData(
									() => {
										var attributes = new List<Tuple<string, string>>();
										attributes.Add( Tuple.Create( "onpagehide", "deactivateProcessingDialog();" ) );
										attributes.Add( Tuple.Create( "data-instant-whitelist", "data-instant-whitelist" ) ); // for https://instant.page/

										return new ElementLocalData( "body", focusDependentData: new ElementFocusDependentData( attributes: attributes ) );
									},
									classes: bodyClasses,
									children: new ElementComponent(
											formContext => new ElementData(
												() => {
													var attributes = new List<Tuple<string, string>>();
													attributes.Add( Tuple.Create( "action", EwfPage.Instance.InfoAsBaseType.GetUrl() ) );
													attributes.Add( Tuple.Create( "method", "post" ) );
													if( FormUsesMultipartEncoding )
														attributes.Add( Tuple.Create( "enctype", "multipart/form-data" ) );
													attributes.Add( Tuple.Create( "novalidate", "" ) );

													return new ElementLocalData( "form", focusDependentData: new ElementFocusDependentData( attributes: attributes ) );
												},
												clientSideIdOverride: EwfPage.FormId,
												children: preContentComponents.Concat( bodyContent )
													.Concat( postContentComponents )
													.Append( etherealContainer )
													.Append(
														new ElementComponent(
															context => new ElementData(
																() => {
																	var attributes = new List<Tuple<string, string>>();
																	attributes.Add( Tuple.Create( "type", "hidden" ) );
																	attributes.Add( Tuple.Create( "name", EwfPage.HiddenFieldName ) );
																	attributes.Add( Tuple.Create( "value", hiddenFieldValueGetter() ) );

																	return new ElementLocalData(
																		"input",
																		focusDependentData: new ElementFocusDependentData( attributes: attributes, includeIdAttribute: true ) );
																},
																clientSideIdOverride: EwfPage.HiddenFieldName ) ) )
													.Materialize(),
												etherealChildren: ModalBox.CreateBrowsingModalBox( BrowsingModalBoxId )
													.Append( statusMessageModalBox )
													.Concat( etherealContent )
													.Materialize() ) ).Append( jsInitElement )
										.Materialize() ) ) )
						.Materialize() ) );

			etherealContainer = new GenericFlowContainer( null );
			this.dataUpdateModificationMethod = dataUpdateModificationMethod;
			this.isAutoDataUpdater = isAutoDataUpdater;
		}

		private string getTitle() =>
			StringTools.ConcatenateWithDelimiter(
				" - ",
				EwfApp.Instance.AppDisplayName.Length > 0 ? EwfApp.Instance.AppDisplayName : ConfigurationStatics.SystemName,
				ResourceBase.CombineResourcePathStrings(
					ResourceBase.ResourcePathSeparator,
					EwfPage.Instance.InfoAsBaseType.ParentResourceEntityPathString,
					EwfPage.Instance.InfoAsBaseType.ResourceFullName ) );

		private IReadOnlyCollection<FlowComponent> getMetadataAndFaviconLinks() {
			var components = new List<FlowComponent>();

			components.Add(
				getMeta( "application-name", EwfApp.Instance.AppDisplayName.Length > 0 ? EwfApp.Instance.AppDisplayName : ConfigurationStatics.SystemName ) );

			// Chrome start URL
			components.Add( getMeta( "application-url", EwfPage.Instance.GetClientUrl( NetTools.HomeUrl ) ) );

			// IE9 start URL
			components.Add( getMeta( "msapplication-starturl", EwfPage.Instance.GetClientUrl( NetTools.HomeUrl ) ) );

			var faviconPng48X48 = EwfApp.Instance.FaviconPng48X48;
			if( faviconPng48X48 != null && faviconPng48X48.UserCanAccessResource )
				components.Add(
					getLink(
						EwfPage.Instance.GetClientUrl( faviconPng48X48.GetUrl( true, true, false ) ),
						"icon",
						attributes: Tuple.Create( "sizes", "48x48" ).ToCollection() ) );

			var favicon = EwfApp.Instance.Favicon;
			if( favicon != null && favicon.UserCanAccessResource )
				// rel="shortcut icon" is deprecated and will be replaced with rel="icon".
				components.Add( getLink( EwfPage.Instance.GetClientUrl( favicon.GetUrl( true, true, false ) ), "shortcut icon" ) );

			components.Add( getMeta( "viewport", "initial-scale=1" ) );

			return components;
		}

		private IEnumerable<FlowComponent> getTypekitLogicIfNecessary() {
			if( EwfApp.Instance.TypekitId.Any() ) {
				yield return new TrustedHtmlString(
					"<script type=\"text/javascript\" src=\"http" + ( EwfApp.Instance.RequestIsSecure( EwfPage.Instance.Request ) ? "s" : "" ) + "://use.typekit.com/" +
					EwfApp.Instance.TypekitId + ".js\"></script>" ).ToComponent();
				yield return new TrustedHtmlString( "<script type=\"text/javascript\">try{Typekit.load();}catch(e){}</script>" ).ToComponent();
			}
		}

		private FlowComponent getStyleSheetLink( string url ) => getLink( url, "stylesheet", attributes: Tuple.Create( "type", "text/css" ).ToCollection() );

		private FlowComponent getModernizrLogic() =>
			new TrustedHtmlString(
				"<script type=\"text/javascript\" src=\"" +
				EwfPage.Instance.GetClientUrl( EwfApp.MetaLogicFactory.CreateModernizrJavaScriptInfo().GetUrl( false, false, false ) ) + "\"></script>" ).ToComponent();

		private IEnumerable<FlowComponent> getGoogleAnalyticsLogicIfNecessary() {
			if( EwfApp.Instance.GoogleAnalyticsWebPropertyId.Length == 0 )
				yield break;
			using( var sw = new StringWriter() ) {
				sw.WriteLine( "<script>" );
				sw.WriteLine( "(function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){" );
				sw.WriteLine( "(i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o)," );
				sw.WriteLine( "m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)" );
				sw.WriteLine( "})(window,document,'script','//www.google-analytics.com/analytics.js','ga');" );

				var userId = EwfApp.Instance.GetGoogleAnalyticsUserId();
				sw.WriteLine(
					"ga('create', '" + EwfApp.Instance.GoogleAnalyticsWebPropertyId + "', 'auto'{0});",
					userId.Any() ? ", {{'userId': '{0}'}}".FormatWith( userId ) : "" );

				sw.WriteLine( "ga('send', 'pageview');" );
				sw.WriteLine( "</script>" );
				yield return new TrustedHtmlString( sw.ToString() ).ToComponent();
			}
		}

		private IReadOnlyCollection<FlowComponent> getJavaScriptIncludes() {
			var components = new List<FlowComponent>();

			FlowComponent getElement( ResourceInfo resource ) =>
				new TrustedHtmlString( "<script src=\"{0}\" defer></script>".FormatWith( EwfPage.Instance.GetClientUrl( resource.GetUrl( false, false, false ) ) ) )
					.ToComponent();

			components.AddRange( EwfApp.MetaLogicFactory.CreateJavaScriptInfos().Select( getElement ) );
			components.Add( new TrustedHtmlString( MiniProfiler.RenderIncludes().ToHtmlString() ).ToComponent() );
			components.AddRange( EwfApp.Instance.GetJavaScriptFiles().Select( getElement ) );

			return components;
		}

		private FlowComponent getLink( string href, string rel, IReadOnlyCollection<Tuple<string, string>> attributes = null ) =>
			new ElementComponent(
				context => new ElementData(
					() => new ElementLocalData(
						"link",
						focusDependentData: new ElementFocusDependentData(
							attributes: Tuple.Create( "href", href )
								.ToCollection()
								.Append( Tuple.Create( "rel", rel ) )
								.Concat( attributes ?? Enumerable.Empty<Tuple<string, string>>() ) ) ) ) );

		private FlowComponent getMeta( string name, string content ) =>
			new ElementComponent(
				context => new ElementData(
					() => {
						var attributes = new List<Tuple<string, string>>();
						attributes.Add( Tuple.Create( "name", name ) );
						attributes.Add( Tuple.Create( "content", content ) );
						return new ElementLocalData( "meta", focusDependentData: new ElementFocusDependentData( attributes: attributes ) );
					} ) );

		public BasicPageContent Add( IReadOnlyCollection<FlowComponent> components ) {
			bodyContent.AddRange( components );
			return this;
		}

		public BasicPageContent Add( FlowComponent component ) {
			bodyContent.Add( component );
			return this;
		}

		public BasicPageContent Add( IReadOnlyCollection<EtherealComponent> components ) {
			etherealContent.AddRange( components );
			return this;
		}

		public BasicPageContent Add( EtherealComponent component ) {
			etherealContent.Add( component );
			return this;
		}

		protected internal override PageContent GetContent() {
			throw new NotSupportedException();
		}
	}
}