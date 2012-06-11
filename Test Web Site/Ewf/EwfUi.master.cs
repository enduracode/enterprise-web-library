using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.AlternativePageModes;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Entity;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Page;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui.Entity;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.WebSessionState;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.RedStapler.TestWebSite.Ewf {
	public partial class EwfUi: MasterPage, ControlTreeDataLoader, AppEwfUiMasterPage {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string BodyCssClass = "ewf";

			internal const string GlobalBlockCssClass = "ewfUiGlobal";
			internal const string AppLogoAndUserInfoBlockCssClass = "ewfUiAppLogoAndUserInfo";
			internal const string AppLogoBlockCssClass = "ewfUiAppLogo";
			internal const string UserInfoListCssClass = "ewfUiUserInfo";
			internal const string GlobalNavBlockCssClass = "ewfUiGlobalNav";

			internal const string TopErrorMessageListBlockCssClass = "ewfUiStatus";

			internal const string EntityAndTabAndContentBlockCssClass = "ewfUiEntityAndTabsAndContent";

			internal const string EntityAndTopTabBlockCssClass = "ewfUiEntityAndTopTabs";

			internal const string EntityBlockCssClass = "ewfUiEntity";
			internal const string EntityNavAndActionBlockCssClass = "ewfUiEntityNavAndActions";
			internal const string EntityNavListCssClass = "ewfUiEntityNav";
			internal const string EntityActionListCssClass = "ewfUiEntityActions";
			internal const string EntitySummaryBlockCssClass = "ewfUiEntitySummary";

			internal const string TopTabListCssClass = "ewfUiTopTabs";

			internal const string SideTabAndContentBlockCssClass = "ewfUiTabsAndContent";

			internal const string SideTabCellCssClass = "ewfTabs";
			internal const string SideTabGroupHeadCssClass = "ewfEditorTabSeparator";

			internal const string CurrentTabCssClass = "ewfEditorSelectedTab";
			internal const string DisabledTabCssClass = "ewfUiDisabledTab";

			internal const string ContentCellCssClass = "ewfContentBox";
			internal const string ContentFootCellCssClass = "ewfStandardEntityDisplayButtons";
			internal const string ContentFootBlockCssClass = "ewfButtons";
			internal const string ContentFootActionListCssClass = "ewfUiCfActions";


			// Some of the elements below cover a subset of other CSS elements in a more specific way. For example, UiGlobalNavControlList selects the control list
			// used for global navigation. This control list is also selected, with lower specificity, by the CSS element that selects all control lists. In general
			// this is a bad situation, but in this case we think it's ok because web apps are not permitted to add their own CSS classes to the controls selected
			// here and therefore it will be difficult for a web app to accidentally trump a CSS element here by adding classes to a lower specificity element.

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				// NOTE: Remove this when applications can have CSS files that are only loaded when the EWF UI is being used.
				var bodyElement = new CssElement( "UiBody", "body." + BodyCssClass ).ToSingleElementArray();

				return bodyElement.Concat( getGlobalElements() ).Concat( getEntityAndTabAndContentElements() ).ToArray();
			}

			private static IEnumerable<CssElement> getGlobalElements() {
				const string globalBlockSelector = "div." + GlobalBlockCssClass;
				const string globalNavBlockSelector = globalBlockSelector + " " + "div." + GlobalNavBlockCssClass;
				return new[]
				       	{
				       		new CssElement( "UiGlobalBlock", globalBlockSelector ),
				       		new CssElement( "UiAppLogoAndUserInfoBlock",
				       		                EwfTable.CssElementCreator.Selectors.Select( i => globalBlockSelector + " " + i + "." + AppLogoAndUserInfoBlockCssClass ).ToArray() )
				       		,
				       		new CssElement( "UiAppLogoBlock",
				       		                EwfImage.CssElementCreator.Selectors.Concat( "div".ToSingleElementArray() ).Select(
				       		                	i => globalBlockSelector + " " + i + "." + AppLogoBlockCssClass ).ToArray() ),
				       		new CssElement( "UiUserInfoControlList",
				       		                ControlStack.CssElementCreator.Selectors.Select( i => globalBlockSelector + " " + i + "." + UserInfoListCssClass ).ToArray() ),
				       		new CssElement( "UiGlobalNavBlock", globalNavBlockSelector ),
				       		new CssElement( "UiGlobalNavControlList", ControlLine.CssElementCreator.Selectors.Select( i => globalNavBlockSelector + " > " + i ).ToArray() ),
				       		new CssElement( "UiTopErrorMessageControlListBlock",
				       		                ErrorMessageControlListBlockStatics.CssElementCreator.Selectors.Select(
				       		                	i => globalBlockSelector + " " + i + "." + TopErrorMessageListBlockCssClass ).ToArray() )
				       	};
			}

			private static IEnumerable<CssElement> getEntityAndTabAndContentElements() {
				var elements = new List<CssElement>();

				const string entityAndTabAndContentBlockSelector = "div." + EntityAndTabAndContentBlockCssClass;
				elements.Add( new CssElement( "UiEntityAndTabAndContentBlock", entityAndTabAndContentBlockSelector ) );

				const string entityAndTopTabBlockSelector = entityAndTabAndContentBlockSelector + " > " + "div." + EntityAndTopTabBlockCssClass;
				elements.Add( new CssElement( "UiEntityAndTopTabBlock", entityAndTopTabBlockSelector ) );

				elements.AddRange( getEntityElements( entityAndTopTabBlockSelector ) );
				elements.Add( new CssElement( "UiTopTabControlList",
				                              ControlLine.CssElementCreator.Selectors.Select( i => entityAndTopTabBlockSelector + " > " + i + "." + TopTabListCssClass ).
				                              	ToArray() ) );
				elements.AddRange( getSideTabAndContentElements( entityAndTabAndContentBlockSelector ) );
				elements.AddRange( getTabElements() );
				return elements;
			}

			private static IEnumerable<CssElement> getEntityElements( string entityAndTopTabBlockSelector ) {
				var entityBlockSelector = entityAndTopTabBlockSelector + " > " + "div." + EntityBlockCssClass;
				return new[]
				       	{
				       		new CssElement( "UiEntityBlock", entityBlockSelector ),
				       		new CssElement( "UiEntityNavAndActionBlock",
				       		                EwfTable.CssElementCreator.Selectors.Select( i => entityBlockSelector + " > " + i + "." + EntityNavAndActionBlockCssClass ).
				       		                	ToArray() ),
				       		new CssElement( "UiEntityNavControlList",
				       		                ControlLine.CssElementCreator.Selectors.Select( i => entityBlockSelector + " " + i + "." + EntityNavListCssClass ).ToArray() ),
				       		new CssElement( "UiEntityActionControlList",
				       		                ControlLine.CssElementCreator.Selectors.Select( i => entityBlockSelector + " " + i + "." + EntityActionListCssClass ).ToArray() ),
				       		new CssElement( "UiEntitySummaryBlock", entityBlockSelector + " > " + "div." + EntitySummaryBlockCssClass )
				       	};
			}

			private static IEnumerable<CssElement> getSideTabAndContentElements( string entityAndTabAndContentBlockSelector ) {
				var sideTabAndContentBlockSelectors =
					EwfTable.CssElementCreator.Selectors.Select( i => entityAndTabAndContentBlockSelector + " > " + i + "." + SideTabAndContentBlockCssClass );
				var contentFootCellSelectors = sideTabAndContentBlockSelectors.Select( i => i + " td." + ContentFootCellCssClass );
				var contentFootBlockSelectors = from contentFootCellSelector in contentFootCellSelectors
				                                from tableSelector in EwfTable.CssElementCreator.Selectors
				                                select contentFootCellSelector + " > " + tableSelector + "." + ContentFootBlockCssClass;
				return new[]
				       	{
				       		new CssElement( "UiSideTabAndContentBlock", sideTabAndContentBlockSelectors.ToArray() ),
				       		new CssElement( "UiSideTabCell", sideTabAndContentBlockSelectors.Select( i => i + " td." + SideTabCellCssClass ).ToArray() ),
				       		new CssElement( "UiSideTabGroupHead", "div." + SideTabGroupHeadCssClass ),
				       		new CssElement( "UiContentCell", sideTabAndContentBlockSelectors.Select( i => i + " td." + ContentCellCssClass ).ToArray() ),
				       		new CssElement( "UiContentFootCell", contentFootCellSelectors.ToArray() ),
				       		new CssElement( "UiContentFootBlock", contentFootBlockSelectors.ToArray() ),
				       		new CssElement( "UiContentFootActionControlList",
				       		                ( from contentFootBlockSelector in contentFootBlockSelectors
				       		                  from controlLineSelector in ControlLine.CssElementCreator.Selectors
				       		                  select contentFootBlockSelector + " " + controlLineSelector + "." + ContentFootActionListCssClass ).ToArray() )
				       	};
			}

			private static IEnumerable<CssElement> getTabElements() {
				return new[] { new CssElement( "UiCurrentTab", "div." + CurrentTabCssClass ), new CssElement( "UiDisabledTab", "div." + DisabledTabCssClass ) };
			}
		}

		private ActionButtonSetup[] contentFootActions = new ActionButtonSetup[ 0 ];
		private Control[] contentFootControls;

		void AppEwfUiMasterPage.SetContentFootActions( params ActionButtonSetup[] actions ) {
			contentFootActions = actions;
			contentFootControls = null;
		}

		void AppEwfUiMasterPage.SetContentFootControls( params Control[] controls ) {
			contentFootActions = null;
			contentFootControls = controls;
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			EwfPage.Instance.SetContentContainer( contentPlace );

			globalPlace.AddControlsReturnThis( getGlobalBlock() );
			entityAndTopTabPlace.AddControlsReturnThis( getEntityAndTopTabBlock() );
			if( entityUsesTabMode( TabMode.Vertical ) )
				setUpSideTabs();
			contentFootCell.Attributes.Add( "class", CssElementCreator.ContentFootCellCssClass );
			var contentFootBlock = getContentFootBlock();
			if( contentFootBlock != null )
				contentFootCell.Controls.AddAt( 0, contentFootBlock );
			globalFootPlace.AddControlsReturnThis( EwfUiStatics.AppProvider.GetGlobalFootControls() );

			BasicPage.Instance.Body.Attributes[ "class" ] = CssElementCreator.BodyCssClass;

			if( AppRequestState.Instance.Browser.IsOldVersionOfMajorBrowser() && !StandardLibrarySessionState.Instance.HideBrowserWarningForRemainderOfSession ) {
				EwfPage.AddStatusMessage( StatusMessageType.Warning,
				                          StringTools.ConcatenateWithDelimiter( " ",
				                                                                new[]
				                                                                	{
				                                                                		"We've detected that you are not using the latest version of your browser.",
				                                                                		"While most features of this site will work, and you will be safe browsing here, we strongly recommend using the newest version of your browser in order to provide a better experience on this site and a safer experience throughout the internet."
				                                                                	} ) + "<br/>" +
				                          NetTools.BuildBasicLink( "Click here to get Internet Explorer 9 (it's free)",
				                                                   new ExternalPageInfo( "http://www.beautyoftheweb.com/" ).GetUrl(),
				                                                   true ) + "<br/>" +
				                          NetTools.BuildBasicLink( "Click here to get Firefox (it's free)",
				                                                   new ExternalPageInfo( "http://www.getfirefox.com" ).GetUrl(),
				                                                   true ) );
			}
			StandardLibrarySessionState.Instance.HideBrowserWarningForRemainderOfSession = true;
		}

		private Control getGlobalBlock() {
			return
				new Block(
					new[] { getAppLogoAndUserInfoBlock(), getGlobalNavBlock(), new ModificationErrorPlaceholder( null, getErrorMessageList ) }.Where( i => i != null ).ToArray() )
					{ CssClass = CssElementCreator.GlobalBlockCssClass };
		}

		private static Control getAppLogoAndUserInfoBlock() {
			var table = EwfTable.Create( style: EwfTableStyle.StandardLayoutOnly, classes: CssElementCreator.AppLogoAndUserInfoBlockCssClass.ToSingleElementArray() );

			var appLogoBlock = EwfUiStatics.AppProvider.GetLogoControl() ??
			                   new Panel().AddControlsReturnThis(
			                   	( EwfApp.Instance.AppDisplayName.Length > 0 ? EwfApp.Instance.AppDisplayName : AppTools.SystemName ).GetLiteralControl() );
			appLogoBlock.CssClass = CssElementCreator.AppLogoBlockCssClass;

			ControlStack userInfoList = null;
			var changePasswordPage = UserManagement.ChangePassword.Page.GetInfo( EwfPage.Instance.InfoAsBaseType.GetUrl() );
			if( changePasswordPage.UserCanAccessPageAndAllControls && AppTools.User != null ) {
				var userInfo = new UserInfo();
				userInfo.LoadData( changePasswordPage );
				userInfoList = ControlStack.CreateWithControls( true, userInfo );
				userInfoList.CssClass = CssElementCreator.UserInfoListCssClass;
			}

			table.AddItem( () => new EwfTableItem( new EwfTableCell( appLogoBlock ), new EwfTableCell( userInfoList ) ) );
			return table;
		}

		private static Control getGlobalNavBlock() {
			// This check exists to prevent the display of lookup boxes or other post back controls. With these controls we sometimes don't have a specific
			// destination page to use for an authorization check, meaning that the system code has no way to prevent their display when there is no intermediate
			// user.
			if( AppTools.IsIntermediateInstallation && !AppRequestState.Instance.IntermediateUserExists )
				return null;

			// NOTE: Remove this condition after all ActionButtonSetups are created with PageInfo objects instead of URLs.
			if( AppTools.User == null && UserManagementStatics.UserManagementEnabled )
				return null;

			var controls =
				getActionControls( EwfUiStatics.AppProvider.GetGlobalNavActionControls() ).Concat( from i in EwfUiStatics.AppProvider.GetGlobalNavLookupBoxSetups()
				                                                                                   select i.BuildLookupBoxPanel() ).ToArray();
			if( !controls.Any() )
				return null;
			return new Block( new ControlLine( controls ) { ItemsSeparatedWithPipe = EwfUiStatics.AppProvider.GlobalNavItemsSeparatedWithPipe() } )
			       	{ CssClass = CssElementCreator.GlobalNavBlockCssClass };
		}

		private static IEnumerable<Control> getErrorMessageList( IEnumerable<string> errors ) {
			if( !errors.Any() )
				yield break;
			var list = ErrorMessageControlListBlockStatics.CreateErrorMessageListBlock( errors );
			list.CssClass = list.CssClass.ConcatenateWithSpace( CssElementCreator.TopErrorMessageListBlockCssClass );
			yield return list;
		}

		private static Control getEntityAndTopTabBlock() {
			var controls = new List<Control> { getEntityBlock() };
			if( entityUsesTabMode( TabMode.Horizontal ) ) {
				var pageGroups = getEntityPageGroups();
				if( pageGroups.Count > 1 )
					throw new ApplicationException( "Top tabs are not supported with multiple page groups." );
				if( pageGroups.Any() )
					controls.Add( getTopTabList( pageGroups.Single() ) );
			}
			return new Block( controls.ToArray() ) { CssClass = CssElementCreator.EntityAndTopTabBlockCssClass };
		}

		private static Control getEntityBlock() {
			return new Block( new[] { getPagePath(), getEntityNavAndActionBlock(), getEntitySummaryBlock() }.Where( i => i != null ).ToArray() )
			       	{ CssClass = CssElementCreator.EntityBlockCssClass };
		}

		private static Control getPagePath() {
			var currentPageBehavior = PagePathCurrentPageBehavior.IncludeCurrentPageAndExcludePageNameIfEntitySetupExists;
			if( !getEntityPageGroups().Any() )
				currentPageBehavior = PagePathCurrentPageBehavior.IncludeCurrentPage;
			return new PagePath { CurrentPageBehavior = currentPageBehavior };
		}

		private static Control getEntityNavAndActionBlock() {
			var controls = new[] { getEntityNavList(), getEntityActionList() }.Where( i => i != null );
			if( !controls.Any() )
				return null;
			var table = EwfTable.Create( style: EwfTableStyle.Raw, classes: CssElementCreator.EntityNavAndActionBlockCssClass.ToSingleElementArray() );
			table.AddItem( () => new EwfTableItem( controls.Select( i => new EwfTableCell( i ) { TextAlignment = TextAlignment.Right } ) ) );
			return table;
		}

		private static Control getEntityNavList() {
			if( entityDisplaySetup == null )
				return null;
			var controls =
				getActionControls( entityDisplaySetup.CreateNavButtonSetups() ).Concat(
					( from i in entityDisplaySetup.CreateLookupBoxSetups() select i.BuildLookupBoxPanel() ) ).ToArray();
			return !controls.Any()
			       	? null
			       	: new ControlLine( controls )
			       	  	{
			       	  		CssClass = CssElementCreator.EntityNavListCssClass,
			       	  		ItemsSeparatedWithPipe = EwfUiStatics.AppProvider.EntityNavAndActionItemsSeparatedWithPipe()
			       	  	};
		}

		private static Control getEntityActionList() {
			if( entityDisplaySetup == null || EwfPage.Instance.InfoAsBaseType.ParentPage != null )
				return null;
			var actionControls = getActionControls( entityDisplaySetup.CreateActionButtonSetups() ).ToArray();
			return !actionControls.Any()
			       	? null
			       	: new ControlLine( actionControls )
			       	  	{
			       	  		CssClass = CssElementCreator.EntityActionListCssClass,
			       	  		ItemsSeparatedWithPipe = EwfUiStatics.AppProvider.EntityNavAndActionItemsSeparatedWithPipe()
			       	  	};
		}

		private static IEnumerable<Control> getActionControls( IEnumerable<ActionButtonSetup> actionButtonSetups ) {
			return from actionButtonSetup in actionButtonSetups
			       let actionControl = actionButtonSetup.BuildButton( text => new TextActionControlStyle( text ), false )
			       let asEwfLink = actionControl as EwfLink
			       where asEwfLink == null || asEwfLink.UserCanNavigateToDestination()
			       select actionControl;
		}

		private static Control getEntitySummaryBlock() {
			// If the entity setup is a nonempty control, display it as an entity summary.
			var entitySummary = EwfPage.Instance.EsAsBaseType as Control;
			if( entitySummary != null && entitySummary.Controls.Count > 0 )
				return new Block( entitySummary ) { CssClass = CssElementCreator.EntitySummaryBlockCssClass };

			return null;
		}

		private static Control getTopTabList( PageGroup pageGroup ) {
			return new ControlLine( getTabControlsForPages( pageGroup ).ToArray() )
			       	{ CssClass = CssElementCreator.TopTabListCssClass, VerticalAlignment = TableCellVerticalAlignment.Bottom };
		}

		private static bool entityUsesTabMode( TabMode tabMode ) {
			var entitySetupInfo = EwfPage.Instance.InfoAsBaseType.EsInfoAsBaseType;
			return entitySetupInfo != null && EwfPage.Instance.InfoAsBaseType.ParentPage == null && entitySetupInfo.GetTabMode() == tabMode;
		}

		private void setUpSideTabs() {
			var pageGroups = getEntityPageGroups();
			tabCell.Visible = pageGroups.Any();

			foreach( var pageGroup in pageGroups ) {
				var tabs = getTabControlsForPages( pageGroup );
				if( tabs.Any() && pageGroup.Name.Length > 0 ) {
					var groupHead = new Panel { CssClass = CssElementCreator.SideTabGroupHeadCssClass };
					groupHead.Controls.Add( pageGroup.Name.GetLiteralControl() );
					tabCell.Controls.Add( groupHead );
				}
				foreach( var control in tabs )
					tabCell.Controls.Add( control );
			}
		}

		private static ReadOnlyCollection<PageGroup> getEntityPageGroups() {
			var entitySetupInfo = EwfPage.Instance.InfoAsBaseType.EsInfoAsBaseType;
			return entitySetupInfo != null && EwfPage.Instance.InfoAsBaseType.ParentPage == null ? entitySetupInfo.Pages : new List<PageGroup>().AsReadOnly();
		}

		private static IEnumerable<Control> getTabControlsForPages( PageGroup pageGroup ) {
			var tabs = new List<Control>();
			foreach( var page in pageGroup.Pages.Where( p => p.UserCanAccessPageAndAllControls ) ) {
				if( page.IsIdenticalToCurrent() ) {
					var tab = new Panel { CssClass = CssElementCreator.CurrentTabCssClass };
					tab.Controls.Add( page.PageName.GetLiteralControl() );
					tabs.Add( tab );
				}
				else if( page.AlternativeMode is DisabledPageMode ) {
					var tab = new Panel { CssClass = CssElementCreator.DisabledTabCssClass };
					tab.Controls.Add( page.PageName.GetLiteralControl() );
					tabs.Add( tab );
				}
				else {
					// NOTE: Should we use CustomActionControlStyle for the link so it doesn't have any built-in styling?
					var tab = EwfLink.Create( page, new TextActionControlStyle( page.PageName ) );
					if( page.AlternativeMode is NewContentPageMode )
						tab.CssClass += " ewfNewness";
					tabs.Add( tab );
				}
			}
			return tabs;
		}

		private Control getContentFootBlock() {
			if( Page is DataModifierWithRightButton ) {
				var dmWithRb = Page as DataModifierWithRightButton;
				var button = new PostBackButton( new DataModification(),
				                                 () => EwfPage.Instance.EhValidateAndModifyDataAndRedirect( dmWithRb.ValidateFormValues, dmWithRb.ModifyData ) );
				EwfUiStatics.SetContentFootActions( new ActionButtonSetup( dmWithRb.RightButtonText, button ) );
			}
			else if( Page is PageWithRightButton ) {
				var rbs = ( Page as PageWithRightButton ).CreateRightButtonSetup();
				EwfUiStatics.SetContentFootActions( new ActionButtonSetup( rbs.Text, new EwfLink( new ExternalPageInfo( rbs.Url ) ) ) );
			}

			var rightControls = new List<Control>();
			var centerControls = new List<Control>();
			if( contentFootActions != null ) {
				if( contentFootActions.Any() ) {
					var first = from i in contentFootActions.Take( 1 )
					            select i.BuildButton( text => new ButtonActionControlStyle( ButtonActionControlStyle.ButtonSize.Large ) { Text = text }, true );
					var remaining = from i in contentFootActions.Skip( 1 )
					                select i.BuildButton( text => new ButtonActionControlStyle( ButtonActionControlStyle.ButtonSize.Large ) { Text = text }, false );
					rightControls.Add( new ControlLine( first.Concat( remaining ).ToArray() ) { CssClass = CssElementCreator.ContentFootActionListCssClass } );
				}
			}
			else
				centerControls.AddRange( contentFootControls.ToList() );

			if( Page is AutoDataModifier ) {
				if( contentFootControls != null )
					throw new ApplicationException( "AutoDataModifier is not currently compatible with custom content foot controls." );
				centerControls.Add( new PostBackButton( new DataModification(), null, new ButtonActionControlStyle( "Update Now" ), !contentFootActions.Any() ) );
			}

			if( !rightControls.Any() && !centerControls.Any() )
				return null;

			var table = EwfTable.Create( style: EwfTableStyle.StandardLayoutOnly,
			                             classes: CssElementCreator.ContentFootBlockCssClass.ToSingleElementArray(),
			                             fields: ( rightControls.Any() ? new[] { 3, 1, 3 } : new[] { 1 } ).Select( i => new EwfTableField( size: Unit.Percentage( i ) ) ) );
			table.AddItem( rightControls.Any()
			               	? new EwfTableItem( "".ToCell(),
			               	                    new EwfTableCell( new PlaceHolder().AddControlsReturnThis( centerControls ) ) { TextAlignment = TextAlignment.Center },
			               	                    new EwfTableCell( new PlaceHolder().AddControlsReturnThis( rightControls ) ) { TextAlignment = TextAlignment.Right } )
			               	: new EwfTableItem( new EwfTableCell( new PlaceHolder().AddControlsReturnThis( centerControls ) ) { TextAlignment = TextAlignment.Center } ) );
			return table;
		}

		private static EntityDisplaySetup entityDisplaySetup { get { return EwfPage.Instance.EsAsBaseType as EntityDisplaySetup; } }
	}
}