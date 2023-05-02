using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.UserManagement;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public class UiPageContent: PageContent {
	private static readonly ElementClass outerGlobalContainerClass = new( "ewfUiOuterGlobal" );
	private static readonly ElementClass innerGlobalContainerClass = new( "ewfUiInnerGlobal" );
	private static readonly ElementClass appLogoClass = new( "ewfUiAppLogo" );
	private static readonly ElementClass globalNavListContainerClass = new( "ewfUiGlobalNav" );
	private static readonly ElementClass userInfoClass = new( "ewfUiUserInfo" );

	private static readonly ElementClass mobileMenuClass = new( "ewfUiMobileMenu" );
	private static readonly ElementClass mobileMenuGlobalNavListContainerClass = new( "ewfUiMobileMenuGlobalNav" );
	private static readonly ElementClass mobileMenuEntityNavAndActionContainerClass = new( "ewfUiMobileMenuEntityNavAndActions" );
	private static readonly ElementClass mobileMenuEntityNavListContainerClass = new( "ewfUiMobileMenuEntityNav" );
	private static readonly ElementClass mobileMenuEntityActionListContainerClass = new( "ewfUiMobileMenuEntityActions" );
	private static readonly ElementClass mobileMenuTabContainerClass = new( "ewfUiMobileMenuTab" );
	private static readonly ElementClass mobileMenuTabGroupClass = new( "ewfUiMobileMenuTabGroup" );

	private static readonly ElementClass topErrorMessageListContainerClass = new( "ewfUiStatus" );

	private static readonly ElementClass entityAndTopTabContainerClass = new( "ewfUiEntityAndTopTabs" );

	private static readonly ElementClass entityContainerClass = new( "ewfUiEntity" );
	private static readonly ElementClass entityNavAndActionContainerClass = new( "ewfUiEntityNavAndActions" );
	private static readonly ElementClass entityNavListContainerClass = new( "ewfUiEntityNav" );
	private static readonly ElementClass entityActionListContainerClass = new( "ewfUiEntityActions" );
	private static readonly ElementClass entitySummaryContainerClass = new( "ewfUiEntitySummary" );

	private static readonly ElementClass topTabListContainerClass = new( "ewfUiTopTab" );

	private static readonly ElementClass sideTabContainerClass = new( "ewfUiSideTab" );
	private static readonly ElementClass sideTabGroupHeadClass = new( "ewfEditorTabSeparator" );

	private static readonly ElementClass currentTabClass = new( "ewfEditorSelectedTab" );
	private static readonly ElementClass disabledTabClass = new( "ewfUiDisabledTab" );

	private static readonly ElementClass pageActionListContainerClass = new( "ewfUiPageAction" );
	private static readonly ElementClass contentContainerClass = new( "ewfUiContent" );
	private static readonly ElementClass contentBoxClass = new( "ewfUiContentBox" );
	private static readonly ElementClass contentFootContainerClass = new( "ewfUiCf" );
	private static readonly ElementClass contentFootActionListContainerClass = new( "ewfUiCfActions" );

	private static readonly ElementClass globalFootContainerClass = new( "ewfUiGf" );
	private static readonly ElementClass poweredByEwlFooterClass = new( "ewfUiPoweredBy" );

	[ UsedImplicitly ]
	private class CssElementCreator: ControlCssElementCreator {
		// Some of the elements below cover a subset of other CSS elements in a more specific way. For example, UiGlobalNavControlList selects the control list
		// used for global navigation. This control list is also selected, with lower specificity, by the CSS element that selects all control lists. In general
		// this is a bad situation, but in this case we think it's ok because web apps are not permitted to add their own CSS classes to the controls selected
		// here and therefore it will be difficult for a web app to accidentally trump a CSS element here by adding classes to a lower-specificity element. It
		// would still be possible to accidentally trump some of the rules in the EWF UI style sheets by chaining together several lower-specificity elements, but
		// we protect against this by incorporating an ID into the selectors here. A selector with an ID should always trump a selector without any IDs.

		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
			return getGlobalElements().Concat( getEntityAndTabAndContentElements() ).Concat( getGlobalFootElements() ).ToArray();
		}

		private IEnumerable<CssElement> getGlobalElements() =>
			new[]
				{
					new CssElement( "UiOuterGlobalContainer", formSelector + " " + "div." + outerGlobalContainerClass.ClassName ),
					new CssElement( "UiInnerGlobalContainer", formSelector + " " + "div." + innerGlobalContainerClass.ClassName ),
					new CssElement( "UiAppLogoContainer", formSelector + " " + "div." + appLogoClass.ClassName ),
					new CssElement( "UiGlobalNavListContainer", formSelector + " " + "div." + globalNavListContainerClass.ClassName ),
					new CssElement( "UiUserInfoContainer", formSelector + " " + "div." + userInfoClass.ClassName ),
					new CssElement( "UiMobileMenuContainer", formSelector + " " + "div." + mobileMenuClass.ClassName ),
					new CssElement( "UiMobileMenu", formSelector + " " + "nav." + mobileMenuClass.ClassName ),
					new CssElement( "UiMobileMenuGlobalNavListContainer", formSelector + " div." + mobileMenuGlobalNavListContainerClass.ClassName ),
					new CssElement( "UiMobileMenuEntityNavAndActionContainer", formSelector + " div." + mobileMenuEntityNavAndActionContainerClass.ClassName ),
					new CssElement( "UiMobileMenuEntityNavListContainer", formSelector + " div." + mobileMenuEntityNavListContainerClass.ClassName ),
					new CssElement( "UiMobileMenuEntityActionListContainer", formSelector + " div." + mobileMenuEntityActionListContainerClass.ClassName ),
					new CssElement( "UiMobileMenuTabContainer", formSelector + " div." + mobileMenuTabContainerClass.ClassName ),
					new CssElement( "UiMobileMenuTabGroup", formSelector + " div." + mobileMenuTabGroupClass.ClassName ),
					new CssElement( "UiMobileMenuTabGroupName", formSelector + " p." + mobileMenuTabGroupClass.ClassName ),
					new CssElement(
						"UiTopErrorMessageListContainer",
						ListErrorDisplayStyle.CssSelectors.Select( i => formSelector + " " + i + "." + topErrorMessageListContainerClass.ClassName ).ToArray() )
				};

		private IEnumerable<CssElement> getEntityAndTabAndContentElements() {
			var elements = new List<CssElement>();
			elements.Add( new CssElement( "UiEntityAndTopTabContainer", formSelector + " " + "div." + entityAndTopTabContainerClass.ClassName ) );
			elements.AddRange( getEntityElements() );
			elements.Add( new CssElement( "UiTopTabListContainer", formSelector + " " + "div." + topTabListContainerClass.ClassName ) );
			elements.AddRange( getSideTabAndContentElements() );
			elements.AddRange( getTabElements() );
			return elements;
		}

		private IEnumerable<CssElement> getEntityElements() =>
			new[]
				{
					new CssElement( "UiEntityContainer", formSelector + " " + "div." + entityContainerClass.ClassName ),
					new CssElement( "UiEntityNavAndActionContainer", formSelector + " " + "div." + entityNavAndActionContainerClass.ClassName ),
					new CssElement( "UiEntityNavListContainer", formSelector + " " + "div." + entityNavListContainerClass.ClassName ),
					new CssElement( "UiEntityActionListContainer", formSelector + " " + "div." + entityActionListContainerClass.ClassName ),
					new CssElement( "UiEntitySummaryContainer", formSelector + " " + "div." + entitySummaryContainerClass.ClassName )
				};

		private IEnumerable<CssElement> getSideTabAndContentElements() =>
			new[]
				{
					new CssElement( "UiSideTabContainer", formSelector + " div." + sideTabContainerClass.ClassName ),
					new CssElement( "UiSideTabGroupHead", formSelector + " div." + sideTabGroupHeadClass.ClassName ),
					new CssElement( "UiPageActionListContainer", formSelector + " " + "div." + pageActionListContainerClass.ClassName ),
					new CssElement( "UiContentContainer", formSelector + " " + "div." + contentContainerClass.ClassName ),
					new CssElement( "UiContentBox", formSelector + " " + "div." + contentBoxClass.ClassName ),
					new CssElement( "UiContentFootContainer", formSelector + " " + "div." + contentFootContainerClass.ClassName ),
					new CssElement( "UiContentFootActionListContainer", formSelector + " " + "div." + contentFootActionListContainerClass.ClassName )
				};

		private IEnumerable<CssElement> getTabElements() =>
			new[]
				{
					new CssElement(
						"UiCurrentTabActionControl",
						ActionComponentCssElementCreator.Selectors.Select( i => i + "." + currentTabClass.ClassName ).ToArray() ),
					new CssElement(
						"UiDisabledTabActionControl",
						ActionComponentCssElementCreator.Selectors.Select( i => i + "." + disabledTabClass.ClassName ).ToArray() )
				};

		private IEnumerable<CssElement> getGlobalFootElements() =>
			new[]
				{
					new CssElement( "UiGlobalFootContainer", formSelector + " div." + globalFootContainerClass.ClassName ),
					new CssElement( "UiPoweredByEwlFooterContainer", formSelector + " ." + poweredByEwlFooterClass.ClassName )
				};


		private string formSelector => BasePageStatics.FormSelector;
	}

	private readonly BasicPageContent basicContent;
	private readonly EntityUiSetup entityUiSetup;
	private readonly List<FlowComponent> content = new();

	/// <summary>
	/// Creates a page content object that uses the EWF user interface.
	/// </summary>
	/// <param name="bodyClasses"></param>
	/// <param name="pageActions">The page actions. Any hyperlink with a destination to which the user cannot navigate (due to authorization logic) will be
	/// automatically hidden by the framework.</param>
	/// <param name="omitContentBox">Pass true to omit the box-style effect around the page content. Useful when all content is contained within multiple
	/// box-style sections.</param>
	/// <param name="contentFootActions">The content-foot actions. The first action, if it is a post-back, will produce a submit button.</param>
	/// <param name="contentFootComponents">The content-foot components.</param>
	/// <param name="dataUpdateModificationMethod">The modification method for the page’s data-update modification.</param>
	/// <param name="isAutoDataUpdater">Pass true to force a post-back when a hyperlink is clicked.</param>
	/// <param name="pageLoadPostBack">A post-back that will be triggered automatically by the browser when the page is finished loading. If this is not null, the
	/// framework will hide all content on the page and show a loading icon instead.</param>
	public UiPageContent(
		ElementClassSet bodyClasses = null, IReadOnlyCollection<ActionComponentSetup> pageActions = null, bool omitContentBox = false,
		IReadOnlyCollection<ButtonSetup> contentFootActions = null, IReadOnlyCollection<FlowComponent> contentFootComponents = null,
		Action dataUpdateModificationMethod = null, bool isAutoDataUpdater = false, ActionPostBack pageLoadPostBack = null ) {
		pageActions ??= Enumerable.Empty<ActionComponentSetup>().Materialize();
		if( contentFootActions != null && contentFootComponents != null )
			throw new ApplicationException( "Either contentFootActions or contentFootComponents may be specified, but not both." );
		if( contentFootActions == null && contentFootComponents == null )
			contentFootActions = Enumerable.Empty<ButtonSetup>().Materialize();

		entityUiSetup = ( PageBase.Current.EsAsBaseType as UiEntitySetup )?.GetUiSetup();
		basicContent =
			new BasicPageContent(
				bodyClasses: bodyClasses,
				dataUpdateModificationMethod: dataUpdateModificationMethod,
				isAutoDataUpdater: isAutoDataUpdater,
				pageLoadPostBack: pageLoadPostBack ).Add(
				getGlobalContainer()
					.Concat( getEntityAndTopTabContainer() )
					.Concat( entityUsesTabMode( TabMode.Vertical ) ? getSideTabContainer().ToCollection() : Enumerable.Empty<FlowComponent>() )
					.Concat( getPageActionListContainer( pageActions ) )
					.Append(
						new GenericFlowContainer(
							new DisplayableElement(
								_ => new DisplayableElementData(
									null,
									() => new DisplayableElementLocalData( "div" ),
									classes: omitContentBox ? null : contentBoxClass,
									children: content ) ).ToCollection(),
							classes: contentContainerClass ) )
					.Concat( getContentFootBlock( isAutoDataUpdater, contentFootActions, contentFootComponents ) )
					.Concat( getGlobalFootContainer() )
					.Materialize() );
	}

	private FlowComponent getGlobalContainer() {
		var appLogo = new GenericFlowContainer(
			( !ConfigurationStatics.IsIntermediateInstallation || AppRequestState.Instance.IntermediateUserExists
				  ? EwfUiStatics.AppProvider.GetLogoComponent()
				  : null ) ?? ( BasePageStatics.AppProvider.AppDisplayName.Length > 0
					                ? BasePageStatics.AppProvider.AppDisplayName
					                : ConfigurationStatics.SystemDisplayName ).ToComponents(),
			classes: appLogoClass );

		var userInfo = new List<FlowComponent>();
		if( AppRequestState.Instance.UserAccessible ) {
			var components = EwfUiStatics.AppProvider.GetUserInfoComponents() ?? getUserInfoComponents();
			if( components.Any() )
				userInfo.Add( new GenericFlowContainer( components, classes: userInfoClass ) );
		}

		return new GenericFlowContainer(
			new GenericFlowContainer(
				appLogo.Concat( getGlobalNavListContainer( false ) )
					.Concat( userInfo )
					.Append( getMobileMenuContainer() )
					.Append(
						new FlowErrorContainer(
							new ErrorSourceSet( includeGeneralErrors: true ),
							new ListErrorDisplayStyle( classes: topErrorMessageListContainerClass ) ) )
					.Materialize(),
				classes: innerGlobalContainerClass ).ToCollection(),
			classes: outerGlobalContainerClass );
	}

	private IReadOnlyCollection<FlowComponent> getUserInfoComponents() {
		var components = new List<FlowComponent>();

		var changePasswordPage = new UserManagement.Pages.ChangePassword( PageBase.Current.GetUrl() );
		if( !changePasswordPage.UserCanAccess || AppTools.User == null )
			return components;

		components.Add( new Paragraph( "Logged in as {0}".FormatWith( AppTools.User.Email ).ToComponents() ) );
		if( !UserManagementStatics.LocalIdentityProviderEnabled )
			return components;

		components.Add(
			new RawList(
				new EwfHyperlink(
						changePasswordPage,
						new CustomHyperlinkStyle( childGetter: _ => ActionComponentIcon.GetIconAndTextComponents( null, "Change password" ) ) ).ToComponentListItem()
					.Append(
						new EwfButton(
							new CustomButtonStyle( children: ActionComponentIcon.GetIconAndTextComponents( null, "Log out" ) ),
							behavior: new PostBackBehavior(
								postBack: PostBack.CreateFull(
									id: "ewfLogOut",
									modificationMethod: AuthenticationStatics.LogOutUser,
									actionGetter: () => new PostBackAction( null, authorizationCheckDisabledPredicate: _ => true ) ) ) ).ToComponentListItem() ) ) );

		return components;
	}

	private FlowComponent getMobileMenuContainer() {
		var menuDisplayed = new PageModificationValue<string>();
		var hiddenFieldId = new HiddenFieldId();
		return new GenericFlowContainer(
			new EwfButton(
					new CustomButtonStyle( attributes: new ElementAttribute( "aria-label", "Menu" ).ToCollection(), children: null ),
					behavior: new CustomButtonBehavior(
						() => hiddenFieldId.GetJsValueModificationStatements(
							"document.getElementById( '{0}' ).value === '{2}' ? '{1}' : '{2}'".FormatWith(
								hiddenFieldId.ElementId.Id,
								bool.FalseString,
								bool.TrueString ) ) ) )
				.Append<FlowComponent>(
					new DisplayableElement(
						_ => new DisplayableElementData(
							menuDisplayed.ToCondition( bool.TrueString.ToCollection() ).ToDisplaySetup(),
							() => new DisplayableElementLocalData( "nav" ),
							classes: mobileMenuClass,
							children: getGlobalNavListContainer( true )
								.Concat( getEntityNavAndActionContainer( true ) )
								.Concat( getMobileMenuTabContainer() )
								.Materialize() ) ) )
				.Materialize(),
			classes: mobileMenuClass,
			etherealContent: new EwfHiddenField( bool.FalseString, id: hiddenFieldId, pageModificationValue: menuDisplayed ).PageComponent.ToCollection() );
	}

	private IEnumerable<FlowComponent> getGlobalNavListContainer( bool inMobileMenu ) {
		// This check exists to prevent the display of lookup boxes or other post back controls. With these controls we sometimes don't have a specific
		// destination page to use for an authorization check, meaning that the system code has no way to prevent their display when there is no intermediate
		// user.
		if( ConfigurationStatics.IsIntermediateInstallation && !AppRequestState.Instance.IntermediateUserExists )
			return Enumerable.Empty<FlowComponent>();

		var postBackIdBase = inMobileMenu ? "mobileMenuGlobal" : "global";
		var formItems = EwfUiStatics.AppProvider.GetGlobalNavFormControls()
			.Select( ( control, index ) => control.GetFormItem( PostBack.GetCompositeId( postBackIdBase, "nav", index.ToString() ) ) );
		var listItems = getActionListItems( EwfUiStatics.AppProvider.GetGlobalNavActions( postBackIdBase ) )
			.Concat( formItems.Select( i => (WrappingListItem)i.ToListItem() ) )
			.Materialize();
		if( !listItems.Any() )
			return Enumerable.Empty<FlowComponent>();

		return new GenericFlowContainer(
			new WrappingList( listItems ).ToCollection(),
			classes: inMobileMenu ? mobileMenuGlobalNavListContainerClass : globalNavListContainerClass ).ToCollection();
	}

	private IEnumerable<FlowComponent> getMobileMenuTabContainer() {
		if( entityUiSetup == null || !PageBase.Current.EntitySetupIsParent )
			return Enumerable.Empty<FlowComponent>();

		var components = PageBase.Current.EsAsBaseType.ListedResources.SelectMany(
				resourceGroup => {
					var tabs = getTabHyperlinksForResources( resourceGroup );
					return tabs.Any()
						       ? new GenericFlowContainer(
							       ( resourceGroup.Name.Any()
								         ? new Paragraph( resourceGroup.Name.ToComponents(), classes: mobileMenuTabGroupClass ).ToCollection()
								         : Enumerable.Empty<FlowComponent>() ).Append( new StackList( tabs.Select( i => i.ToComponentListItem() ) ) )
							       .Materialize(),
							       classes: mobileMenuTabGroupClass ).ToCollection()
						       : Enumerable.Empty<FlowComponent>();
				} )
			.Materialize();
		return components.Any() ? new GenericFlowContainer( components, classes: mobileMenuTabContainerClass ).ToCollection() : Enumerable.Empty<FlowComponent>();
	}

	private IEnumerable<FlowComponent> getEntityAndTopTabContainer() {
		var components = new List<FlowComponent>();
		components.AddRange( getEntityContainer() );
		if( entityUsesTabMode( TabMode.Horizontal ) ) {
			var resourceGroups = PageBase.Current.EsAsBaseType.ListedResources;
			if( resourceGroups.Count > 1 )
				throw new ApplicationException( "Top tabs are not supported with multiple resource groups." );
			components.Add( getTopTabListContainer( resourceGroups.Single() ) );
		}
		return components.Any() ? new GenericFlowContainer( components, classes: entityAndTopTabContainerClass ).ToCollection() : Enumerable.Empty<FlowComponent>();
	}

	private IEnumerable<FlowComponent> getEntityContainer() {
		var components = getPagePath().Concat( getEntityNavAndActionContainer( false ) ).Concat( getEntitySummaryContainer() ).Materialize();
		return components.Any() ? new GenericFlowContainer( components, classes: entityContainerClass ).ToCollection() : Enumerable.Empty<FlowComponent>();
	}

	private IReadOnlyCollection<FlowComponent> getPagePath() {
		var pagePath = new PagePath(
			currentPageBehavior: PageBase.Current.EsAsBaseType?.ListedResources.Any() == true
				                     ? PagePathCurrentPageBehavior.IncludeCurrentPageAndUseEntitySetupNameIfEntitySetupIsParent
				                     : PagePathCurrentPageBehavior.IncludeCurrentPage );
		return pagePath.IsEmpty ? Enumerable.Empty<FlowComponent>().Materialize() : pagePath.ToCollection();
	}

	private IReadOnlyCollection<FlowComponent> getEntityNavAndActionContainer( bool inMobileMenu ) {
		var items = new[] { getEntityNavListContainer( inMobileMenu ), getEntityActionListContainer( inMobileMenu ) }.Where( i => i != null ).Materialize();
		return items.Any()
			       ? new GenericFlowContainer( items, classes: inMobileMenu ? mobileMenuEntityNavAndActionContainerClass : entityNavAndActionContainerClass )
				       .ToCollection()
			       : Enumerable.Empty<FlowComponent>().Materialize();
	}

	private FlowComponent getEntityNavListContainer( bool inMobileMenu ) {
		if( entityUiSetup == null )
			return null;

		var postBackIdBase = inMobileMenu ? "mobileMenuEntity" : "entity";
		var formItems = entityUiSetup.NavFormControls.Select(
			( control, index ) => control.GetFormItem( PostBack.GetCompositeId( postBackIdBase, "nav", index.ToString() ) ) );
		var listItems = getActionListItems( entityUiSetup.NavActionGetter( postBackIdBase ) )
			.Concat( formItems.Select( i => (WrappingListItem)i.ToListItem() ) )
			.Materialize();
		if( !listItems.Any() )
			return null;

		return new GenericFlowContainer(
			new WrappingList( listItems ).ToCollection(),
			classes: inMobileMenu ? mobileMenuEntityNavListContainerClass : entityNavListContainerClass );
	}

	private FlowComponent getEntityActionListContainer( bool inMobileMenu ) {
		if( entityUiSetup == null || !PageBase.Current.EntitySetupIsParent )
			return null;
		var listItems = getActionListItems( entityUiSetup.ActionGetter( inMobileMenu ? "mobileMenuEntity" : "entity" ) ).Materialize();
		if( !listItems.Any() )
			return null;
		return new GenericFlowContainer(
			new WrappingList( listItems ).ToCollection(),
			classes: inMobileMenu ? mobileMenuEntityActionListContainerClass : entityActionListContainerClass );
	}

	private IReadOnlyCollection<FlowComponent> getEntitySummaryContainer() {
		if( entityUiSetup?.EntitySummaryContent != null )
			return new GenericFlowContainer( entityUiSetup.EntitySummaryContent, classes: entitySummaryContainerClass ).ToCollection();
		return Enumerable.Empty<FlowComponent>().Materialize();
	}

	private FlowComponent getTopTabListContainer( ResourceGroup resourceGroup ) =>
		new GenericFlowContainer(
			new LineList(
				getTabHyperlinksForResources( resourceGroup ).Select( i => (LineListItem)i.ToComponentListItem() ),
				verticalAlignment: FlexboxVerticalAlignment.Bottom ).ToCollection(),
			classes: topTabListContainerClass );

	private bool entityUsesTabMode( TabMode tabMode ) =>
		entityUiSetup != null && PageBase.Current.EntitySetupIsParent && entityUiSetup.GetTabMode( PageBase.Current.EsAsBaseType ) == tabMode;

	private FlowComponent getSideTabContainer() {
		var components = new List<FlowComponent>();
		foreach( var resourceGroup in PageBase.Current.EsAsBaseType.ListedResources ) {
			var tabs = getTabHyperlinksForResources( resourceGroup );
			if( tabs.Any() && resourceGroup.Name.Any() )
				components.Add( new GenericFlowContainer( resourceGroup.Name.ToComponents(), classes: sideTabGroupHeadClass ) );
			components.AddRange( tabs );
		}
		return new GenericFlowContainer( components, classes: sideTabContainerClass );
	}

	private IReadOnlyCollection<PhrasingComponent> getTabHyperlinksForResources( ResourceGroup resourceGroup ) {
		var hyperlinks = new List<PhrasingComponent>();
		foreach( var resource in resourceGroup.Resources.Where( p => p.UserCanAccess ) )
			hyperlinks.Add(
				new EwfHyperlink(
					resource.MatchesCurrent() ? null : resource,
					new CustomHyperlinkStyle(
						childGetter: destinationUrl => ActionComponentIcon.GetIconAndTextComponents(
							null,
							resource.ResourceName.Any() ? resource.ResourceName : destinationUrl ) ),
					classes: resource.MatchesCurrent() ? currentTabClass :
					         resource.AlternativeMode is DisabledResourceMode ? disabledTabClass : ElementClassSet.Empty ) );
		return hyperlinks;
	}

	private IReadOnlyCollection<FlowComponent> getPageActionListContainer( IReadOnlyCollection<ActionComponentSetup> pageActions ) {
		var listItems = getActionListItems( pageActions ).Materialize();
		if( !listItems.Any() )
			return Enumerable.Empty<FlowComponent>().Materialize();
		return new GenericFlowContainer( new WrappingList( listItems ).ToCollection(), classes: pageActionListContainerClass ).ToCollection();
	}

	private IEnumerable<WrappingListItem> getActionListItems( IReadOnlyCollection<ActionComponentSetup> actions ) =>
		from action in actions
		let actionComponent = action.GetActionComponent(
			( text, icon ) => new CustomHyperlinkStyle(
				childGetter: destinationUrl => ActionComponentIcon.GetIconAndTextComponents( icon, text.Any() ? text : destinationUrl ) ),
			( text, icon ) => new CustomButtonStyle( children: ActionComponentIcon.GetIconAndTextComponents( icon, text ) ) )
		where actionComponent != null
		select (WrappingListItem)actionComponent.ToComponentListItem( displaySetup: action.DisplaySetup );

	private IReadOnlyCollection<FlowComponent> getContentFootBlock(
		bool isAutoDataUpdater, IReadOnlyCollection<ButtonSetup> contentFootActions, IReadOnlyCollection<FlowComponent> contentFootComponents ) {
		var components = new List<FlowComponent>();
		if( contentFootActions != null ) {
			if( contentFootActions.Any() )
				components.Add(
					new GenericFlowContainer(
						new WrappingList(
							contentFootActions.Select(
								( action, index ) => (WrappingListItem)action.GetActionComponent(
										null,
										( text, icon ) => new StandardButtonStyle( text, buttonSize: ButtonSize.Large, icon: icon ),
										enableSubmitButton: index == 0 )
									.ToComponentListItem( displaySetup: action.DisplaySetup ) ) ).ToCollection(),
						classes: contentFootActionListContainerClass ) );
			else if( isAutoDataUpdater )
				components.Add( new SubmitButton( new StandardButtonStyle( "Update Now" ), postBack: PageBase.Current.DataUpdatePostBack ) );
		}
		else {
			if( isAutoDataUpdater )
				throw new ApplicationException( "AutoDataUpdater is not currently compatible with custom content foot controls." );
			components.AddRange( contentFootComponents );
		}

		return components.Any()
			       ? new GenericFlowContainer( components, classes: contentFootContainerClass ).ToCollection()
			       : Enumerable.Empty<FlowComponent>().Materialize();
	}

	private IReadOnlyCollection<FlowComponent> getGlobalFootContainer() {
		var components = new List<FlowComponent>();

		// This check exists to prevent the display of post back controls. With these controls we sometimes don't have a specific destination page to use for an
		// authorization check, meaning that the system code has no way to prevent their display when there is no intermediate user.
		if( !ConfigurationStatics.IsIntermediateInstallation || AppRequestState.Instance.IntermediateUserExists )
			components.AddRange( EwfUiStatics.AppProvider.GetGlobalFootComponents() );

		var ewlWebSite = new ExternalResource( "http://enterpriseweblibrary.org/" );
		if( ewlWebSite.UserCanAccess && !EwfUiStatics.AppProvider.PoweredByEwlFooterDisabled() )
			components.Add(
				new Paragraph(
					"Powered by the ".ToComponents()
						.Append( new EwfHyperlink( ewlWebSite.ToHyperlinkNewTabBehavior(), new StandardHyperlinkStyle( EwlStatics.EwlName ) ) )
						.Concat(
							" ({0} version)".FormatWith( TimeZoneInfo.ConvertTime( EwlStatics.EwlBuildDateTime, TimeZoneInfo.Local ).ToMonthYearString() ).ToComponents() )
						.Materialize(),
					classes: poweredByEwlFooterClass ) );

		return components.Any()
			       ? new GenericFlowContainer( components, classes: globalFootContainerClass ).ToCollection()
			       : Enumerable.Empty<FlowComponent>().Materialize();
	}

	public UiPageContent Add( IReadOnlyCollection<FlowComponent> components ) {
		content.AddRange( components );
		return this;
	}

	public UiPageContent Add( FlowComponent component ) {
		content.Add( component );
		return this;
	}

	public UiPageContent Add( IReadOnlyCollection<EtherealComponent> components ) {
		basicContent.Add( components );
		return this;
	}

	public UiPageContent Add( EtherealComponent component ) {
		basicContent.Add( component );
		return this;
	}

	protected internal override PageContent GetContent() => basicContent;
}