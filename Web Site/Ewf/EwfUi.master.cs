using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui.Entity;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	public partial class EwfUi: MasterPage, ControlTreeDataLoader, AppEwfUiMasterPage {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string GlobalContainerId = "ewfUiGlobal";
			internal static readonly ElementClass AppLogoAndUserInfoBlockClass = new ElementClass( "ewfUiAppLogoAndUserInfo" );
			internal static readonly ElementClass AppLogoClass = new ElementClass( "ewfUiAppLogo" );
			internal static readonly ElementClass UserInfoClass = new ElementClass( "ewfUiUserInfo" );
			internal static readonly ElementClass TopErrorMessageListContainerClass = new ElementClass( "ewfUiStatus" );
			internal static readonly ElementClass GlobalNavListContainerClass = new ElementClass( "ewfUiGlobalNav" );

			internal const string EntityAndTabAndContentBlockId = "ewfUiEntityAndTabsAndContent";

			internal const string EntityAndTopTabBlockCssClass = "ewfUiEntityAndTopTabs";

			internal const string EntityBlockCssClass = "ewfUiEntity";
			internal static readonly ElementClass EntityNavAndActionContainerClass = new ElementClass( "ewfUiEntityNavAndActions" );
			internal static readonly ElementClass EntityNavListContainerClass = new ElementClass( "ewfUiEntityNav" );
			internal static readonly ElementClass EntityActionListContainerClass = new ElementClass( "ewfUiEntityActions" );
			internal const string EntitySummaryBlockCssClass = "ewfUiEntitySummary";

			internal const string TopTabCssClass = "ewfUiTopTab";

			internal const string SideTabAndContentBlockCssClass = "ewfUiTabsAndContent";

			internal const string SideTabCssClass = "ewfUiSideTab";
			internal static readonly ElementClass SideTabGroupHeadClass = new ElementClass( "ewfEditorTabSeparator" );

			internal static readonly ElementClass CurrentTabClass = new ElementClass( "ewfEditorSelectedTab" );
			internal static readonly ElementClass DisabledTabClass = new ElementClass( "ewfUiDisabledTab" );

			internal static readonly ElementClass ContentClass = new ElementClass( "ewfUiContent" );
			internal static readonly ElementClass PageActionListContainerClass = new ElementClass( "ewfUiPageAction" );
			internal static readonly ElementClass ContentFootBlockClass = new ElementClass( "ewfButtons" );
			internal static readonly ElementClass ContentFootActionListContainerClass = new ElementClass( "ewfUiCfActions" );

			internal const string GlobalFootBlockId = "ewfUiGlobalFoot";
			internal static readonly ElementClass PoweredByEwlFooterClass = new ElementClass( "ewfUiPoweredBy" );


			// Some of the elements below cover a subset of other CSS elements in a more specific way. For example, UiGlobalNavControlList selects the control list
			// used for global navigation. This control list is also selected, with lower specificity, by the CSS element that selects all control lists. In general
			// this is a bad situation, but in this case we think it's ok because web apps are not permitted to add their own CSS classes to the controls selected
			// here and therefore it will be difficult for a web app to accidentally trump a CSS element here by adding classes to a lower-specificity element. It
			// would still be possible to accidentally trump some of the rules in the EWF UI style sheets by chaining together several lower-specificity elements, but
			// we protect against this by incorporating an ID into the selectors here. A selector with an ID should always trump a selector without any IDs.

			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return getGlobalElements().Concat( getEntityAndTabAndContentElements() ).Concat( getGlobalFootElements() ).ToArray();
			}

			private IEnumerable<CssElement> getGlobalElements() {
				const string globalContainerSelector = "div#" + GlobalContainerId;
				return new[]
					{
						new CssElement( "UiGlobalContainer", globalContainerSelector ),
						new CssElement(
							"UiAppLogoAndUserInfoBlock",
							TableCssElementCreator.Selectors.Select( i => globalContainerSelector + " " + i + "." + AppLogoAndUserInfoBlockClass.ClassName ).ToArray() ),
						new CssElement( "UiAppLogoContainer", globalContainerSelector + " " + "div." + AppLogoClass.ClassName ),
						new CssElement( "UiUserInfoContainer", globalContainerSelector + " " + "div." + UserInfoClass.ClassName ),
						new CssElement(
							"UiTopErrorMessageListContainer",
							ListErrorDisplayStyle.CssSelectors.Select( i => globalContainerSelector + " " + i + "." + TopErrorMessageListContainerClass.ClassName )
								.ToArray() ),
						new CssElement( "UiGlobalNavListContainer", globalContainerSelector + " " + "div." + GlobalNavListContainerClass.ClassName )
					};
			}

			private IEnumerable<CssElement> getEntityAndTabAndContentElements() {
				var elements = new List<CssElement>();

				const string entityAndTabAndContentBlockSelector = "div#" + EntityAndTabAndContentBlockId;
				elements.Add( new CssElement( "UiEntityAndTabAndContentBlock", entityAndTabAndContentBlockSelector ) );

				elements.Add( new CssElement( "UiEntityAndTopTabBlock", entityAndTabAndContentBlockSelector + " > " + "div." + EntityAndTopTabBlockCssClass ) );
				elements.AddRange( getEntityElements( entityAndTabAndContentBlockSelector ) );
				elements.Add( new CssElement( "UiTopTabBlock", entityAndTabAndContentBlockSelector + " " + "div." + TopTabCssClass ) );
				elements.AddRange( getSideTabAndContentElements( entityAndTabAndContentBlockSelector ) );
				elements.AddRange( getTabElements() );
				return elements;
			}

			private IEnumerable<CssElement> getEntityElements( string entityAndTabAndContentBlockSelector ) {
				return new[]
					{
						new CssElement( "UiEntityBlock", entityAndTabAndContentBlockSelector + " " + "div." + EntityBlockCssClass ),
						new CssElement(
							"UiEntityNavAndActionContainer",
							entityAndTabAndContentBlockSelector + " " + "div." + EntityNavAndActionContainerClass.ClassName ),
						new CssElement( "UiEntityNavListContainer", entityAndTabAndContentBlockSelector + " " + "div." + EntityNavListContainerClass.ClassName ),
						new CssElement( "UiEntityActionListContainer", entityAndTabAndContentBlockSelector + " " + "div." + EntityActionListContainerClass.ClassName ),
						new CssElement( "UiEntitySummaryBlock", entityAndTabAndContentBlockSelector + " " + "div." + EntitySummaryBlockCssClass )
					};
			}

			private IEnumerable<CssElement> getSideTabAndContentElements( string entityAndTabAndContentBlockSelector ) {
				return new[]
					{
						new CssElement(
							"UiSideTabAndContentBlock",
							TableCssElementCreator.Selectors.Select( i => entityAndTabAndContentBlockSelector + " > " + i + "." + SideTabAndContentBlockCssClass )
								.ToArray() ),
						new CssElement( "UiSideTabBlockCell", entityAndTabAndContentBlockSelector + " td." + SideTabCssClass ),
						new CssElement( "UiSideTabBlock", entityAndTabAndContentBlockSelector + " div." + SideTabCssClass ),
						new CssElement( "UiSideTabGroupHead", entityAndTabAndContentBlockSelector + " div." + SideTabGroupHeadClass.ClassName ),
						new CssElement( "UiPageActionAndContentAndContentFootCell", entityAndTabAndContentBlockSelector + " td." + ContentClass.ClassName ),
						new CssElement( "UiPageActionListContainer", entityAndTabAndContentBlockSelector + " " + "div." + PageActionListContainerClass.ClassName ),
						new CssElement( "UiContentBox", entityAndTabAndContentBlockSelector + " " + "div." + ContentClass.ClassName ),
						new CssElement(
							"UiContentFootBlock",
							TableCssElementCreator.Selectors.Select( i => entityAndTabAndContentBlockSelector + " " + i + "." + ContentFootBlockClass.ClassName )
								.ToArray() ),
						new CssElement(
							"UiContentFootActionListContainer",
							entityAndTabAndContentBlockSelector + " " + "div." + ContentFootActionListContainerClass.ClassName )
					};
			}

			private IEnumerable<CssElement> getTabElements() {
				return new[]
					{
						new CssElement(
							"UiCurrentTabActionControl",
							ActionComponentCssElementCreator.Selectors.Select( i => i + "." + CurrentTabClass.ClassName ).ToArray() ),
						new CssElement(
							"UiDisabledTabActionControl",
							ActionComponentCssElementCreator.Selectors.Select( i => i + "." + DisabledTabClass.ClassName ).ToArray() )
					};
			}

			private IEnumerable<CssElement> getGlobalFootElements() {
				const string globalFootBlockSelector = "div#" + GlobalFootBlockId;
				return new[]
					{
						new CssElement( "UiGlobalFootBlock", globalFootBlockSelector ),
						new CssElement( "UiPoweredByEwlFooterBlock", globalFootBlockSelector + " ." + PoweredByEwlFooterClass.ClassName )
					};
			}
		}

		private bool omitContentBox;
		private IReadOnlyCollection<ActionComponentSetup> pageActions = Enumerable.Empty<ActionComponentSetup>().Materialize();
		private IReadOnlyCollection<ButtonSetup> contentFootActions = Enumerable.Empty<ButtonSetup>().Materialize();
		private IReadOnlyCollection<FlowComponent> contentFootComponents;

		void AppEwfUiMasterPage.OmitContentBox() {
			omitContentBox = true;
		}

		void AppEwfUiMasterPage.SetPageActions( IReadOnlyCollection<ActionComponentSetup> actions ) {
			pageActions = actions;
		}

		void AppEwfUiMasterPage.SetContentFootActions( IReadOnlyCollection<ButtonSetup> actions ) {
			contentFootActions = actions;
			contentFootComponents = null;
		}

		void AppEwfUiMasterPage.SetContentFootComponents( IReadOnlyCollection<FlowComponent> components ) {
			contentFootActions = null;
			contentFootComponents = components;
		}

		void ControlTreeDataLoader.LoadData() {
			globalPlace.AddControlsReturnThis( getGlobalContainer() );
			entityAndTopTabPlace.AddControlsReturnThis( getEntityAndTopTabBlock() );
			if( entityUsesTabMode( TabMode.Vertical ) )
				setUpSideTabs();
			pageActionPlace.AddControlsReturnThis( getPageActionListContainer().GetControls() );
			if( !omitContentBox )
				contentContainer.Attributes.Add( "class", CssElementCreator.ContentClass.ClassName );
			contentFootPlace.AddControlsReturnThis( getContentFootBlock().GetControls() );
			var globalFootBlock = getGlobalFootBlock();
			if( globalFootBlock != null )
				globalFootPlace.AddControlsReturnThis( globalFootBlock );

			if( !EwfUiStatics.AppProvider.BrowserWarningDisabled() ) {
				if( AppRequestState.Instance.Browser.IsOldVersionOfMajorBrowser() && !StandardLibrarySessionState.Instance.HideBrowserWarningForRemainderOfSession )
					EwfPage.AddStatusMessage(
						StatusMessageType.Warning,
						StringTools.ConcatenateWithDelimiter(
							" ",
							"We've detected that you are not using the latest version of your browser.",
							"While most features of this site will work, and you will be safe browsing here, we strongly recommend using the newest version of your browser in order to provide a better experience on this site and a safer experience throughout the Internet." ) +
						"<br/>" +
						NetTools.BuildBasicLink( "Click here to get Firefox (it's free)", new ExternalResourceInfo( "http://www.getfirefox.com" ).GetUrl(), true ) +
						"<br />" +
						NetTools.BuildBasicLink(
							"Click here to get Chrome (it's free)",
							new ExternalResourceInfo( "https://www.google.com/intl/en/chrome/browser/" ).GetUrl(),
							true ) + "<br />" + NetTools.BuildBasicLink(
							"Click here to get the latest Internet Explorer (it's free)",
							new ExternalResourceInfo( "http://www.beautyoftheweb.com/" ).GetUrl(),
							true ) );
				StandardLibrarySessionState.Instance.HideBrowserWarningForRemainderOfSession = true;
			}
		}

		private Control getGlobalContainer() =>
			new Block(
				new[]
						{
							// ReSharper disable once SuspiciousTypeConversion.Global
							EwfUiStatics.AppProvider is AppLogoAndUserInfoControlOverrider appLogoAndUserInfoControlOverrider
								? appLogoAndUserInfoControlOverrider.GetAppLogoAndUserInfoControl()
								: new PlaceHolder().AddControlsReturnThis( getAppLogoAndUserInfoBlock().ToCollection().GetControls() ),
							new PlaceHolder().AddControlsReturnThis(
								new FlowErrorContainer(
										new ErrorSourceSet( includeGeneralErrors: true ),
										new ListErrorDisplayStyle( classes: CssElementCreator.TopErrorMessageListContainerClass ) ).ToCollection()
									.GetControls() ),
							getGlobalNavListContainer()
						}.Where( i => i != null )
					.ToArray() ) { ClientIDMode = ClientIDMode.Static, ID = CssElementCreator.GlobalContainerId };

		private FlowComponent getAppLogoAndUserInfoBlock() {
			var table = EwfTable.Create( style: EwfTableStyle.StandardLayoutOnly, classes: CssElementCreator.AppLogoAndUserInfoBlockClass );

			var appLogo = new GenericFlowContainer(
				( !ConfigurationStatics.IsIntermediateInstallation || AppRequestState.Instance.IntermediateUserExists
					  ? EwfUiStatics.AppProvider.GetLogoComponent()
					  : null ) ?? ( EwfApp.Instance.AppDisplayName.Length > 0 ? EwfApp.Instance.AppDisplayName : ConfigurationStatics.SystemName ).ToComponents(),
				classes: CssElementCreator.AppLogoClass );

			var userInfo = new List<FlowComponent>();
			if( AppRequestState.Instance.UserAccessible ) {
				var changePasswordPage = UserManagement.ChangePassword.Page.GetInfo( EwfPage.Instance.InfoAsBaseType.GetUrl() );
				if( changePasswordPage.UserCanAccessResource && AppTools.User != null )
					userInfo.Add( new GenericFlowContainer( getUserInfo( changePasswordPage ), classes: CssElementCreator.UserInfoClass ) );
			}

			table.AddItem( () => new EwfTableItem( appLogo.ToCollection().ToCell(), userInfo.ToCell() ) );
			return table;
		}

		private IReadOnlyCollection<FlowComponent> getUserInfo( PageInfo changePasswordPage ) {
			var components = new List<FlowComponent>();

			components.AddRange( "Logged in as {0}".FormatWith( AppTools.User.Email ).ToComponents() );
			if( !FormsAuthStatics.FormsAuthEnabled )
				return components;

			components.Add(
				new InlineList(
					new EwfHyperlink( changePasswordPage, new ButtonHyperlinkStyle( "Change password", buttonSize: ButtonSize.ShrinkWrap ) ).ToCollection()
						.ToComponentListItem()
						.ToCollection()
						.Append(
							new EwfButton(
									new StandardButtonStyle( "Log out", buttonSize: ButtonSize.ShrinkWrap ),
									behavior: new PostBackBehavior(
										postBack: PostBack.CreateFull(
											id: "ewfLogOut",
											firstModificationMethod: FormsAuthStatics.LogOutUser,
											actionGetter: () => {
												// NOTE: Is this the correct behavior if we are already on a public page?
												return new PostBackAction( new ExternalResourceInfo( NetTools.HomeUrl ) );
											} ) ) ).ToCollection()
								.ToComponentListItem() ) ) );

			return components;
		}

		private Control getGlobalNavListContainer() {
			// This check exists to prevent the display of lookup boxes or other post back controls. With these controls we sometimes don't have a specific
			// destination page to use for an authorization check, meaning that the system code has no way to prevent their display when there is no intermediate
			// user.
			if( ConfigurationStatics.IsIntermediateInstallation && !AppRequestState.Instance.IntermediateUserExists )
				return null;

			var formItems = EwfUiStatics.AppProvider.GetGlobalNavFormControls()
				.Select( ( control, index ) => control.GetFormItem( PostBack.GetCompositeId( "global", "nav", index.ToString() ) ) )
				.Materialize();
			var listItems = getActionListItems( EwfUiStatics.AppProvider.GetGlobalNavActions() )
				.Concat( formItems.Select( i => i.Content.ToComponentListItem() ) )
				.Materialize();
			if( !listItems.Any() )
				return null;

			return new PlaceHolder().AddControlsReturnThis(
				new GenericFlowContainer(
						( EwfUiStatics.AppProvider.GlobalNavItemsSeparatedWithPipe()
							  ? (FlowComponent)new InlineList( listItems )
							  : new LineList( listItems.Select( i => (LineListItem)i ) ) ).Append(
							new FlowErrorContainer( new ErrorSourceSet( validations: formItems.Select( i => i.Validation ) ), new ListErrorDisplayStyle() ) )
						.Materialize(),
						classes: CssElementCreator.GlobalNavListContainerClass ).ToCollection()
					.GetControls() );
		}

		private Control getEntityAndTopTabBlock() {
			var controls = new List<Control> { getEntityBlock() };
			if( entityUsesTabMode( TabMode.Horizontal ) ) {
				var resourceGroups = EwfPage.Instance.InfoAsBaseType.EsInfoAsBaseType.Resources;
				if( resourceGroups.Count > 1 )
					throw new ApplicationException( "Top tabs are not supported with multiple resource groups." );
				controls.Add( getTopTabBlock( resourceGroups.Single() ) );
			}
			return new Block( controls.ToArray() ) { CssClass = CssElementCreator.EntityAndTopTabBlockCssClass };
		}

		private Control getEntityBlock() =>
			new Block( getPagePath().Concat( getEntityNavAndActionContainer() ).GetControls().Concat( getEntitySummaryBlock() ).ToArray() )
				{
					CssClass = CssElementCreator.EntityBlockCssClass
				};

		private IReadOnlyCollection<FlowComponent> getPagePath() {
			var entitySetupInfo = EwfPage.Instance.InfoAsBaseType.EsInfoAsBaseType;
			var pagePath = new PagePath(
				currentPageBehavior: entitySetupInfo != null && EwfPage.Instance.InfoAsBaseType.ParentResource == null && entitySetupInfo.Resources.Any()
					                     ? PagePathCurrentPageBehavior.IncludeCurrentPageAndExcludePageNameIfEntitySetupExists
					                     : PagePathCurrentPageBehavior.IncludeCurrentPage );
			return pagePath.IsEmpty ? Enumerable.Empty<FlowComponent>().Materialize() : pagePath.ToCollection();
		}

		private IReadOnlyCollection<FlowComponent> getEntityNavAndActionContainer() {
			var items = new[] { getEntityNavListContainer(), getEntityActionListContainer() }.Where( i => i != null ).Materialize();
			return items.Any()
				       ? new GenericFlowContainer( items, classes: CssElementCreator.EntityNavAndActionContainerClass ).ToCollection()
				       : Enumerable.Empty<FlowComponent>().Materialize();
		}

		private FlowComponent getEntityNavListContainer() {
			if( uiEntitySetup == null )
				return null;

			var formItems = uiEntitySetup.GetNavFormControls()
				.Select( ( control, index ) => control.GetFormItem( PostBack.GetCompositeId( "entity", "nav", index.ToString() ) ) )
				.Materialize();
			var listItems = getActionListItems( uiEntitySetup.GetNavActions() ).Concat( formItems.Select( i => i.Content.ToComponentListItem() ) ).Materialize();
			if( !listItems.Any() )
				return null;

			return new GenericFlowContainer(
				( EwfUiStatics.AppProvider.EntityNavAndActionItemsSeparatedWithPipe()
					  ? (FlowComponent)new InlineList( listItems )
					  : new LineList( listItems.Select( i => (LineListItem)i ) ) ).Append(
					new FlowErrorContainer( new ErrorSourceSet( validations: formItems.Select( i => i.Validation ) ), new ListErrorDisplayStyle() ) )
				.Materialize(),
				classes: CssElementCreator.EntityNavListContainerClass );
		}

		private FlowComponent getEntityActionListContainer() {
			if( uiEntitySetup == null || EwfPage.Instance.InfoAsBaseType.ParentResource != null )
				return null;
			var listItems = getActionListItems( uiEntitySetup.GetActions() ).Materialize();
			if( !listItems.Any() )
				return null;
			return new GenericFlowContainer(
				( EwfUiStatics.AppProvider.EntityNavAndActionItemsSeparatedWithPipe()
					  ? (FlowComponent)new InlineList( listItems )
					  : new LineList( listItems.Select( i => (LineListItem)i ) ) ).ToCollection(),
				classes: CssElementCreator.EntityActionListContainerClass );
		}

		private IReadOnlyCollection<Control> getEntitySummaryBlock() {
			// If the entity setup is a nonempty control, display it as an entity summary.
			//
			// It's a hack to call GetDescendants this early in the life cycle, but we should be able to fix it when we separate EWF from Web Forms. This is
			// EnduraCode goal 790. What we are essentially doing here is determining whether there is at least one "component" in the entity summary.
			if( EwfPage.Instance.EsAsBaseType is Control entitySummary && EwfPage.Instance.GetDescendants( entitySummary ).Any( i => !( i is PlaceHolder ) ) )
				return new Block( entitySummary ) { CssClass = CssElementCreator.EntitySummaryBlockCssClass }.ToCollection();

			return Enumerable.Empty<Control>().Materialize();
		}

		private Control getTopTabBlock( ResourceGroup resourceGroup ) {
			return new Block(
				new ControlLine( getTabControlsForResources( resourceGroup, false ).ToArray() ) { VerticalAlignment = TableCellVerticalAlignment.Bottom } )
				{
					CssClass = CssElementCreator.TopTabCssClass
				};
		}

		private bool entityUsesTabMode( TabMode tabMode ) {
			var entitySetupInfo = EwfPage.Instance.InfoAsBaseType.EsInfoAsBaseType;
			return entitySetupInfo != null && EwfPage.Instance.InfoAsBaseType.ParentResource == null && entitySetupInfo.GetTabMode() == tabMode;
		}

		private void setUpSideTabs() {
			sideTabCell.Visible = true;
			var controls = new List<Control>();
			foreach( var resourceGroup in EwfPage.Instance.InfoAsBaseType.EsInfoAsBaseType.Resources ) {
				var tabs = getTabControlsForResources( resourceGroup, true );
				if( tabs.Any() && resourceGroup.Name.Any() )
					controls.AddRange(
						new GenericFlowContainer( resourceGroup.Name.ToComponents(), classes: CssElementCreator.SideTabGroupHeadClass ).ToCollection().GetControls() );
				controls.AddRange( tabs );
			}
			sideTabCell.AddControlsReturnThis( new Block( controls.ToArray() ) { CssClass = CssElementCreator.SideTabCssClass } );
		}

		private IEnumerable<Control> getTabControlsForResources( ResourceGroup resourceGroup, bool includeIcons ) {
			var tabs = new List<Control>();
			foreach( var resource in resourceGroup.Resources.Where( p => p.UserCanAccessResource ) ) {
				tabs.Add(
					new PlaceHolder().AddControlsReturnThis(
						new EwfHyperlink(
								resource.IsIdenticalToCurrent() ? null : resource,
								new StandardHyperlinkStyle(
									resource.ResourceName,
									icon: includeIcons
										      ? new ActionComponentIcon( new FontAwesomeIcon( resource.IsIdenticalToCurrent() ? "fa-circle" : "fa-circle-thin" ) )
										      : null ),
								classes: resource.IsIdenticalToCurrent() ? CssElementCreator.CurrentTabClass :
								         resource.AlternativeMode is DisabledResourceMode ? CssElementCreator.DisabledTabClass : ElementClassSet.Empty ).ToCollection()
							.GetControls() ) );
			}
			return tabs;
		}

		private IReadOnlyCollection<FlowComponent> getPageActionListContainer() {
			var listItems = getActionListItems( pageActions ).Materialize();
			if( !listItems.Any() )
				return Enumerable.Empty<FlowComponent>().Materialize();
			return new GenericFlowContainer(
				( EwfUiStatics.AppProvider.PageActionItemsSeparatedWithPipe()
					  ? (FlowComponent)new InlineList( listItems )
					  : new LineList( listItems.Select( i => (LineListItem)i ) ) ).ToCollection(),
				classes: CssElementCreator.PageActionListContainerClass ).ToCollection();
		}

		private IEnumerable<ComponentListItem> getActionListItems( IReadOnlyCollection<ActionComponentSetup> actions ) =>
			from action in actions
			let actionComponent = action.GetActionComponent(
				( text, icon ) => new ButtonHyperlinkStyle( text, buttonSize: ButtonSize.ShrinkWrap, icon: icon ),
				( text, icon ) => new StandardButtonStyle( text, buttonSize: ButtonSize.ShrinkWrap, icon: icon ) )
			where actionComponent != null
			select actionComponent.ToComponentListItem( displaySetup: action.DisplaySetup );

		private IReadOnlyCollection<FlowComponent> getContentFootBlock() {
			var components = new List<FlowComponent>();
			if( contentFootActions != null ) {
				if( contentFootActions.Any() )
					components.Add(
						new GenericFlowContainer(
							new LineList(
								contentFootActions.Select(
									( action, index ) => (LineListItem)action.GetActionComponent(
											null,
											( text, icon ) => new StandardButtonStyle( text, buttonSize: ButtonSize.Large, icon: icon ),
											enableSubmitButton: index == 0 )
										.ToComponentListItem( displaySetup: action.DisplaySetup ) ) ).ToCollection(),
							classes: CssElementCreator.ContentFootActionListContainerClass ) );
				else if( EwfPage.Instance.IsAutoDataUpdater )
					components.Add( new SubmitButton( new StandardButtonStyle( "Update Now" ), postBack: EwfPage.Instance.DataUpdatePostBack ) );
			}
			else {
				if( EwfPage.Instance.IsAutoDataUpdater )
					throw new ApplicationException( "AutoDataUpdater is not currently compatible with custom content foot controls." );
				components.AddRange( contentFootComponents );
			}

			if( !components.Any() )
				return Enumerable.Empty<FlowComponent>().Materialize();

			var table = EwfTable.Create( style: EwfTableStyle.StandardLayoutOnly, classes: CssElementCreator.ContentFootBlockClass );
			table.AddItem(
				new EwfTableItem(
					components.ToCell(
						new TableCellSetup(
							textAlignment: contentFootActions == null || !contentFootActions.Any() ? TextAlignment.Center : TextAlignment.NotSpecified ) ) ) );
			return table.ToCollection();
		}

		private Control getGlobalFootBlock() {
			var controls = new List<Control>();

			// This check exists to prevent the display of post back controls. With these controls we sometimes don't have a specific destination page to use for an
			// authorization check, meaning that the system code has no way to prevent their display when there is no intermediate user.
			if( !ConfigurationStatics.IsIntermediateInstallation || AppRequestState.Instance.IntermediateUserExists )
				controls.AddRange( EwfUiStatics.AppProvider.GetGlobalFootControls() );

			var ewlWebSite = new ExternalResourceInfo( "http://enterpriseweblibrary.org/" );
			if( ewlWebSite.UserCanAccessResource && !EwfUiStatics.AppProvider.PoweredByEwlFooterDisabled() )
				controls.AddRange(
					new Paragraph(
							"Powered by the ".ToComponents()
								.Append( new EwfHyperlink( ewlWebSite.ToHyperlinkNewTabBehavior(), new StandardHyperlinkStyle( EwlStatics.EwlName ) ) )
								.Concat(
									" ({0} version)".FormatWith( TimeZoneInfo.ConvertTime( EwlStatics.EwlBuildDateTime, TimeZoneInfo.Local ).ToMonthYearString() )
										.ToComponents() )
								.Materialize(),
							classes: CssElementCreator.PoweredByEwlFooterClass ).ToCollection()
						.GetControls() );

			return controls.Any() ? new Block( controls.ToArray() ) { ClientIDMode = ClientIDMode.Static, ID = CssElementCreator.GlobalFootBlockId } : null;
		}

		private UiEntitySetupBase uiEntitySetup => EwfPage.Instance.EsAsBaseType as UiEntitySetupBase;
	}
}