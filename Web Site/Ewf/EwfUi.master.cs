using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Entity;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui.Entity;
using RedStapler.StandardLibrary.WebSessionState;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	public partial class EwfUi: MasterPage, ControlTreeDataLoader, AppEwfUiMasterPage {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string GlobalBlockId = "ewfUiGlobal";
			internal const string AppLogoAndUserInfoBlockCssClass = "ewfUiAppLogoAndUserInfo";
			internal const string AppLogoBlockCssClass = "ewfUiAppLogo";
			internal const string UserInfoListCssClass = "ewfUiUserInfo";
			internal const string GlobalNavBlockCssClass = "ewfUiGlobalNav";

			internal const string TopErrorMessageListBlockCssClass = "ewfUiStatus";

			internal const string EntityAndTabAndContentBlockId = "ewfUiEntityAndTabsAndContent";

			internal const string EntityAndTopTabBlockCssClass = "ewfUiEntityAndTopTabs";

			internal const string EntityBlockCssClass = "ewfUiEntity";
			internal const string EntityNavAndActionBlockCssClass = "ewfUiEntityNavAndActions";
			internal const string EntityNavListCssClass = "ewfUiEntityNav";
			internal const string EntityActionListCssClass = "ewfUiEntityActions";
			internal const string EntitySummaryBlockCssClass = "ewfUiEntitySummary";

			internal const string TopTabCssClass = "ewfUiTopTab";

			internal const string SideTabAndContentBlockCssClass = "ewfUiTabsAndContent";

			internal const string SideTabCssClass = "ewfUiSideTab";
			internal const string SideTabGroupHeadCssClass = "ewfEditorTabSeparator";

			internal const string CurrentTabCssClass = "ewfEditorSelectedTab";
			internal const string DisabledTabCssClass = "ewfUiDisabledTab";

			internal const string ContentCssClass = "ewfUiContent";
			internal const string ContentFootBlockCssClass = "ewfButtons";
			internal const string ContentFootActionListCssClass = "ewfUiCfActions";

			internal const string GlobalFootBlockId = "ewfUiGlobalFoot";
			internal const string PoweredByEwlFooterCssClass = "ewfUiPoweredBy";


			// Some of the elements below cover a subset of other CSS elements in a more specific way. For example, UiGlobalNavControlList selects the control list
			// used for global navigation. This control list is also selected, with lower specificity, by the CSS element that selects all control lists. In general
			// this is a bad situation, but in this case we think it's ok because web apps are not permitted to add their own CSS classes to the controls selected
			// here and therefore it will be difficult for a web app to accidentally trump a CSS element here by adding classes to a lower-specificity element. It
			// would still be possible to accidentally trump some of the rules in the EWF UI style sheets by chaining together several lower-specificity elements, but
			// we protect against this by incorporating an ID into the selectors here. A selector with an ID should always trump a selector without any IDs.

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return getGlobalElements().Concat( getEntityAndTabAndContentElements() ).Concat( getGlobalFootElements() ).ToArray();
			}

			private IEnumerable<CssElement> getGlobalElements() {
				const string globalBlockSelector = "div#" + GlobalBlockId;
				const string globalNavBlockSelector = globalBlockSelector + " " + "div." + GlobalNavBlockCssClass;
				return new[]
					{
						new CssElement( "UiGlobalBlock", globalBlockSelector ),
						new CssElement(
							"UiAppLogoAndUserInfoBlock",
							EwfTable.CssElementCreator.Selectors.Select( i => globalBlockSelector + " " + i + "." + AppLogoAndUserInfoBlockCssClass ).ToArray() ),
						new CssElement(
							"UiAppLogoBlock",
							EwfImage.CssElementCreator.Selectors.Concat( "div".ToSingleElementArray() )
								.Select( i => globalBlockSelector + " " + i + "." + AppLogoBlockCssClass )
								.ToArray() ),
						new CssElement(
							"UiUserInfoControlList",
							ControlStack.CssElementCreator.Selectors.Select( i => globalBlockSelector + " " + i + "." + UserInfoListCssClass ).ToArray() ),
						new CssElement( "UiGlobalNavBlock", globalNavBlockSelector ),
						new CssElement( "UiGlobalNavControlList", ControlLine.CssElementCreator.Selectors.Select( i => globalNavBlockSelector + " > " + i ).ToArray() ),
						new CssElement(
							"UiTopErrorMessageControlListBlock",
							ErrorMessageControlListBlockStatics.CssElementCreator.Selectors.Select( i => globalBlockSelector + " " + i + "." + TopErrorMessageListBlockCssClass )
								.ToArray() )
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
							"UiEntityNavAndActionBlock",
							EwfTable.CssElementCreator.Selectors.Select( i => entityAndTabAndContentBlockSelector + " " + i + "." + EntityNavAndActionBlockCssClass ).ToArray() ),
						new CssElement(
							"UiEntityNavControlList",
							ControlLine.CssElementCreator.Selectors.Select( i => entityAndTabAndContentBlockSelector + " " + i + "." + EntityNavListCssClass ).ToArray() ),
						new CssElement(
							"UiEntityActionControlList",
							ControlLine.CssElementCreator.Selectors.Select( i => entityAndTabAndContentBlockSelector + " " + i + "." + EntityActionListCssClass ).ToArray() ),
						new CssElement( "UiEntitySummaryBlock", entityAndTabAndContentBlockSelector + " " + "div." + EntitySummaryBlockCssClass )
					};
			}

			private IEnumerable<CssElement> getSideTabAndContentElements( string entityAndTabAndContentBlockSelector ) {
				var pageActionAndContentAndContentFootCellSelector = entityAndTabAndContentBlockSelector + " td." + ContentCssClass;
				return new[]
					{
						new CssElement(
							"UiSideTabAndContentBlock",
							EwfTable.CssElementCreator.Selectors.Select( i => entityAndTabAndContentBlockSelector + " > " + i + "." + SideTabAndContentBlockCssClass ).ToArray() ),
						new CssElement( "UiSideTabBlockCell", entityAndTabAndContentBlockSelector + " td." + SideTabCssClass ),
						new CssElement( "UiSideTabBlock", entityAndTabAndContentBlockSelector + " div." + SideTabCssClass ),
						new CssElement( "UiSideTabGroupHead", entityAndTabAndContentBlockSelector + " div." + SideTabGroupHeadCssClass ),
						new CssElement( "UiPageActionAndContentAndContentFootCell", pageActionAndContentAndContentFootCellSelector ),
						new CssElement(
							"UiPageActionControlList",
							ControlLine.CssElementCreator.Selectors.Select( i => pageActionAndContentAndContentFootCellSelector + " > " + i ).ToArray() ),
						new CssElement( "UiContentBlock", entityAndTabAndContentBlockSelector + " " + "div." + ContentCssClass ),
						new CssElement(
							"UiContentFootBlock",
							EwfTable.CssElementCreator.Selectors.Select( i => entityAndTabAndContentBlockSelector + " " + i + "." + ContentFootBlockCssClass ).ToArray() ),
						new CssElement(
							"UiContentFootActionControlList",
							ControlLine.CssElementCreator.Selectors.Select( i => entityAndTabAndContentBlockSelector + " " + i + "." + ContentFootActionListCssClass ).ToArray() )
					};
			}

			private IEnumerable<CssElement> getTabElements() {
				return new[]
					{
						new CssElement(
							"UiCurrentTabActionControl",
							EnterpriseWebFramework.Controls.CssElementCreator.Selectors.Select( i => i + "." + CurrentTabCssClass ).ToArray() ),
						new CssElement(
							"UiDisabledTabActionControl",
							EnterpriseWebFramework.Controls.CssElementCreator.Selectors.Select( i => i + "." + DisabledTabCssClass ).ToArray() )
					};
			}

			private IEnumerable<CssElement> getGlobalFootElements() {
				const string globalFootBlockSelector = "div#" + GlobalFootBlockId;
				return new[]
					{
						new CssElement( "UiGlobalFootBlock", globalFootBlockSelector ),
						new CssElement( "UiPoweredByEwlFooterBlock", globalFootBlockSelector + " ." + PoweredByEwlFooterCssClass )
					};
			}
		}

		private ActionButtonSetup[] pageActions = new ActionButtonSetup[ 0 ];
		private ActionButtonSetup[] contentFootActions = new ActionButtonSetup[ 0 ];
		private Control[] contentFootControls;

		void AppEwfUiMasterPage.SetPageActions( params ActionButtonSetup[] actions ) {
			pageActions = actions;
		}

		void AppEwfUiMasterPage.SetContentFootActions( params ActionButtonSetup[] actions ) {
			contentFootActions = actions;
			contentFootControls = null;
		}

		void AppEwfUiMasterPage.SetContentFootControls( params Control[] controls ) {
			contentFootActions = null;
			contentFootControls = controls;
		}

		void ControlTreeDataLoader.LoadData() {
			EwfPage.Instance.SetContentContainer( contentPlace );

			globalPlace.AddControlsReturnThis( getGlobalBlock() );
			entityAndTopTabPlace.AddControlsReturnThis( getEntityAndTopTabBlock() );
			if( entityUsesTabMode( TabMode.Vertical ) )
				setUpSideTabs();
			pageActionPlace.AddControlsReturnThis( getPageActionList() );
			var contentFootBlock = getContentFootBlock();
			if( contentFootBlock != null )
				contentFootPlace.AddControlsReturnThis( contentFootBlock );
			var globalFootBlock = getGlobalFootBlock();
			if( globalFootBlock != null )
				globalFootPlace.AddControlsReturnThis( globalFootBlock );

			if( !EwfUiStatics.AppProvider.BrowserWarningDisabled() ) {
				if( AppRequestState.Instance.Browser.IsOldVersionOfMajorBrowser() && !StandardLibrarySessionState.Instance.HideBrowserWarningForRemainderOfSession ) {
					EwfPage.AddStatusMessage(
						StatusMessageType.Warning,
						StringTools.ConcatenateWithDelimiter(
							" ",
							"We've detected that you are not using the latest version of your browser.",
							"While most features of this site will work, and you will be safe browsing here, we strongly recommend using the newest version of your browser in order to provide a better experience on this site and a safer experience throughout the Internet." ) +
						"<br/>" + NetTools.BuildBasicLink( "Click here to get Firefox (it's free)", new ExternalResourceInfo( "http://www.getfirefox.com" ).GetUrl(), true ) +
						"<br />" +
						NetTools.BuildBasicLink(
							"Click here to get Chrome (it's free)",
							new ExternalResourceInfo( "https://www.google.com/intl/en/chrome/browser/" ).GetUrl(),
							true ) + "<br />" +
						NetTools.BuildBasicLink(
							"Click here to get the latest Internet Explorer (it's free)",
							new ExternalResourceInfo( "http://www.beautyoftheweb.com/" ).GetUrl(),
							true ) );
				}
				StandardLibrarySessionState.Instance.HideBrowserWarningForRemainderOfSession = true;
			}
		}

		private Control getGlobalBlock() {
			// ReSharper disable once SuspiciousTypeConversion.Global
			var appLogoAndUserInfoControlOverrider = EwfUiStatics.AppProvider as AppLogoAndUserInfoControlOverrider;

			return
				new Block(
					new[]
						{
							appLogoAndUserInfoControlOverrider != null ? appLogoAndUserInfoControlOverrider.GetAppLogoAndUserInfoControl() : getAppLogoAndUserInfoBlock(),
							getGlobalNavBlock(), new ModificationErrorPlaceholder( null, getErrorMessageList )
						}.Where( i => i != null ).ToArray() )
					{
						ClientIDMode = ClientIDMode.Static,
						ID = CssElementCreator.GlobalBlockId
					};
		}

		private Control getAppLogoAndUserInfoBlock() {
			var table = EwfTable.Create( style: EwfTableStyle.StandardLayoutOnly, classes: CssElementCreator.AppLogoAndUserInfoBlockCssClass.ToSingleElementArray() );

			var appLogoBlock = ( !AppTools.IsIntermediateInstallation || AppRequestState.Instance.IntermediateUserExists
				                     ? EwfUiStatics.AppProvider.GetLogoControl()
				                     : null ) ??
			                   new Panel().AddControlsReturnThis(
				                   ( EwfApp.Instance.AppDisplayName.Length > 0 ? EwfApp.Instance.AppDisplayName : AppTools.SystemName ).GetLiteralControl() );
			appLogoBlock.CssClass = CssElementCreator.AppLogoBlockCssClass;

			ControlStack userInfoList = null;
			if( AppRequestState.Instance.UserAccessible ) {
				var changePasswordPage = UserManagement.ChangePassword.Page.GetInfo( EwfPage.Instance.InfoAsBaseType.GetUrl() );
				if( changePasswordPage.UserCanAccessResource && AppTools.User != null ) {
					var userInfo = new UserInfo();
					userInfo.LoadData( changePasswordPage );
					userInfoList = ControlStack.CreateWithControls( true, userInfo );
					userInfoList.CssClass = CssElementCreator.UserInfoListCssClass;
				}
			}

			table.AddItem( () => new EwfTableItem( appLogoBlock, userInfoList ) );
			return table;
		}

		private Control getGlobalNavBlock() {
			// This check exists to prevent the display of lookup boxes or other post back controls. With these controls we sometimes don't have a specific
			// destination page to use for an authorization check, meaning that the system code has no way to prevent their display when there is no intermediate
			// user.
			if( AppTools.IsIntermediateInstallation && !AppRequestState.Instance.IntermediateUserExists )
				return null;

			var controls =
				getActionControls( EwfUiStatics.AppProvider.GetGlobalNavActionControls() )
					.Concat( from i in EwfUiStatics.AppProvider.GetGlobalNavLookupBoxSetups() select i.BuildLookupBoxPanel() )
					.ToArray();
			if( !controls.Any() )
				return null;
			return new Block( new ControlLine( controls ) { ItemsSeparatedWithPipe = EwfUiStatics.AppProvider.GlobalNavItemsSeparatedWithPipe() } )
				{
					CssClass = CssElementCreator.GlobalNavBlockCssClass
				};
		}

		private IEnumerable<Control> getErrorMessageList( IEnumerable<string> errors ) {
			if( !errors.Any() )
				yield break;
			var list = ErrorMessageControlListBlockStatics.CreateErrorMessageListBlock( errors );
			list.CssClass = list.CssClass.ConcatenateWithSpace( CssElementCreator.TopErrorMessageListBlockCssClass );
			yield return list;
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

		private Control getEntityBlock() {
			return new Block( new[] { getPagePath(), getEntityNavAndActionBlock(), getEntitySummaryBlock() }.Where( i => i != null ).ToArray() )
				{
					CssClass = CssElementCreator.EntityBlockCssClass
				};
		}

		private Control getPagePath() {
			var entitySetupInfo = EwfPage.Instance.InfoAsBaseType.EsInfoAsBaseType;
			var pagePath =
				new PagePath(
					currentPageBehavior:
						entitySetupInfo != null && EwfPage.Instance.InfoAsBaseType.ParentResource == null && entitySetupInfo.Resources.Any()
							? PagePathCurrentPageBehavior.IncludeCurrentPageAndExcludePageNameIfEntitySetupExists
							: PagePathCurrentPageBehavior.IncludeCurrentPage );
			return pagePath.IsEmpty ? null : pagePath;
		}

		private Control getEntityNavAndActionBlock() {
			var cells = new[] { getEntityNavCell(), getEntityActionCell() }.Where( i => i != null );
			if( !cells.Any() )
				return null;
			var table = EwfTable.Create( style: EwfTableStyle.Raw, classes: CssElementCreator.EntityNavAndActionBlockCssClass.ToSingleElementArray() );
			table.AddItem( new EwfTableItem( cells ) );
			return table;
		}

		private EwfTableCell getEntityNavCell() {
			if( entityDisplaySetup == null )
				return null;
			var controls =
				getActionControls( entityDisplaySetup.CreateNavButtonSetups() )
					.Concat( ( from i in entityDisplaySetup.CreateLookupBoxSetups() select i.BuildLookupBoxPanel() ) )
					.ToArray();
			if( !controls.Any() )
				return null;
			return new ControlLine( controls )
				{
					CssClass = CssElementCreator.EntityNavListCssClass,
					ItemsSeparatedWithPipe = EwfUiStatics.AppProvider.EntityNavAndActionItemsSeparatedWithPipe()
				};
		}

		private EwfTableCell getEntityActionCell() {
			if( entityDisplaySetup == null || EwfPage.Instance.InfoAsBaseType.ParentResource != null )
				return null;
			var actionControls = getActionControls( entityDisplaySetup.CreateActionButtonSetups() ).ToArray();
			if( !actionControls.Any() )
				return null;
			return
				new ControlLine( actionControls )
					{
						CssClass = CssElementCreator.EntityActionListCssClass,
						ItemsSeparatedWithPipe = EwfUiStatics.AppProvider.EntityNavAndActionItemsSeparatedWithPipe()
					}.ToCell(
						new TableCellSetup( textAlignment: TextAlignment.Right ) );
		}

		private Control getEntitySummaryBlock() {
			// If the entity setup is a nonempty control, display it as an entity summary.
			//
			// It's a hack to call GetDescendants this early in the life cycle, but we should be able to fix it when we separate EWF from Web Forms. This is
			// EnduraCode goal 790. What we are essentially doing here is determining whether there is at least one "component" in the entity summary.
			var entitySummary = EwfPage.Instance.EsAsBaseType as Control;
			if( entitySummary != null && EwfPage.Instance.GetDescendants( entitySummary ).Any( i => !( i is PlaceHolder ) ) )
				return new Block( entitySummary ) { CssClass = CssElementCreator.EntitySummaryBlockCssClass };

			return null;
		}

		private Control getTopTabBlock( ResourceGroup resourceGroup ) {
			return
				new Block(
					new ControlLine( getTabControlsForResources( resourceGroup, false ).ToArray() )
						{
							CssClass = CssElementCreator.TopTabCssClass,
							VerticalAlignment = TableCellVerticalAlignment.Bottom
						} ) { CssClass = CssElementCreator.TopTabCssClass };
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
					controls.Add( new Block( resourceGroup.Name.GetLiteralControl() ) { CssClass = CssElementCreator.SideTabGroupHeadCssClass } );
				controls.AddRange( tabs );
			}
			sideTabCell.AddControlsReturnThis( new Block( controls.ToArray() ) { CssClass = CssElementCreator.SideTabCssClass } );
		}

		private IEnumerable<Control> getTabControlsForResources( ResourceGroup resourceGroup, bool includeIcons ) {
			var tabs = new List<Control>();
			foreach( var resource in resourceGroup.Resources.Where( p => p.UserCanAccessResource ) ) {
				var tab = EwfLink.Create(
					resource.IsIdenticalToCurrent() ? null : resource,
					new TextActionControlStyle(
						resource.ResourceName,
						icon: includeIcons ? new ActionControlIcon( new FontAwesomeIcon( resource.IsIdenticalToCurrent() ? "fa-circle" : "fa-circle-thin" ) ) : null ) );

				tab.CssClass = resource.IsIdenticalToCurrent()
					               ? CssElementCreator.CurrentTabCssClass
					               : resource.AlternativeMode is DisabledResourceMode ? CssElementCreator.DisabledTabCssClass : "";
				tabs.Add( tab );
			}
			return tabs;
		}

		private IEnumerable<Control> getPageActionList() {
			var actionControls = getActionControls( pageActions ).ToArray();
			if( !actionControls.Any() )
				yield break;
			yield return new ControlLine( actionControls ) { ItemsSeparatedWithPipe = EwfUiStatics.AppProvider.PageActionItemsSeparatedWithPipe() };
		}

		private IEnumerable<Control> getActionControls( IEnumerable<ActionButtonSetup> actionButtonSetups ) {
			return from actionButtonSetup in actionButtonSetups
			       let actionControl = actionButtonSetup.BuildButton( text => new TextActionControlStyle( text ), false )
			       let asEwfLink = actionControl as EwfLink
			       where asEwfLink == null || asEwfLink.UserCanNavigateToDestination()
			       select actionControl;
		}

		private Control getContentFootBlock() {
			var controls = new List<Control>();
			if( contentFootActions != null ) {
				if( contentFootActions.Any() ) {
					var first = from i in contentFootActions.Take( 1 )
					            select i.BuildButton( text => new ButtonActionControlStyle( text, ButtonActionControlStyle.ButtonSize.Large ), true );
					var remaining = from i in contentFootActions.Skip( 1 )
					                select i.BuildButton( text => new ButtonActionControlStyle( text, ButtonActionControlStyle.ButtonSize.Large ), false );
					controls.Add( new ControlLine( first.Concat( remaining ).ToArray() ) { CssClass = CssElementCreator.ContentFootActionListCssClass } );
				}
				else if( EwfPage.Instance.IsAutoDataUpdater )
					controls.Add( new PostBackButton( EwfPage.Instance.DataUpdatePostBack, new ButtonActionControlStyle( "Update Now" ) ) );
			}
			else {
				if( EwfPage.Instance.IsAutoDataUpdater )
					throw new ApplicationException( "AutoDataUpdater is not currently compatible with custom content foot controls." );
				controls.AddRange( contentFootControls.ToList() );
			}

			if( !controls.Any() )
				return null;

			var table = EwfTable.Create( style: EwfTableStyle.StandardLayoutOnly, classes: CssElementCreator.ContentFootBlockCssClass.ToSingleElementArray() );
			table.AddItem(
				new EwfTableItem(
					controls.ToCell( new TableCellSetup( textAlignment: contentFootActions != null && contentFootActions.Any() ? TextAlignment.Right : TextAlignment.Center ) ) ) );
			return table;
		}

		private Control getGlobalFootBlock() {
			var controls = new List<Control>();

			// This check exists to prevent the display of post back controls. With these controls we sometimes don't have a specific destination page to use for an
			// authorization check, meaning that the system code has no way to prevent their display when there is no intermediate user.
			if( !AppTools.IsIntermediateInstallation || AppRequestState.Instance.IntermediateUserExists )
				controls.AddRange( EwfUiStatics.AppProvider.GetGlobalFootControls() );

			var ewlWebSite = new ExternalResourceInfo( "http://enterpriseweblibrary.org/" );
			if( ewlWebSite.UserCanAccessResource && !EwfUiStatics.AppProvider.PoweredByEwlFooterDisabled() ) {
				controls.Add(
					new Paragraph(
						"Powered by the ".GetLiteralControl(),
						EwfLink.CreateForNavigationInNewWindow( ewlWebSite, new TextActionControlStyle( "Enterprise Web Library" ) ),
						( " (" + TimeZoneInfo.ConvertTime( AppTools.EwlBuildDateTime, TimeZoneInfo.Local ).ToMonthYearString() + " version)" ).GetLiteralControl() )
						{
							CssClass = CssElementCreator.PoweredByEwlFooterCssClass
						} );
			}

			return controls.Any() ? new Block( controls.ToArray() ) { ClientIDMode = ClientIDMode.Static, ID = CssElementCreator.GlobalFootBlockId } : null;
		}

		private EntityDisplaySetup entityDisplaySetup { get { return EwfPage.Instance.EsAsBaseType as EntityDisplaySetup; } }
	}
}