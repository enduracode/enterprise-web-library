using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase.Classification;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.GeneralContentModels.Flow;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

[ PublicAPI ]
public class FilterPageContent: PageContent {
	private readonly UiPageContent content;

	/// <summary>
	/// Creates a page content object with two box-style sections, one for filters and the other for results.
	/// </summary>
	/// <param name="filterContentGetter">A method that returns the content for the filter section. Executes with an intermediate post-back that has the result
	/// section as an update region. The current data-modification actions are also included in the execution. If you have a button, we recommend labeling it
	/// “Update results”.</param>
	/// <param name="resultContentGetter">A method that returns the content for the result section.</param>
	/// <param name="bodyClasses"></param>
	/// <param name="pageActions">The page actions. Any hyperlink with a destination to which the user cannot navigate (due to authorization logic) will be
	/// automatically hidden by the framework.</param>
	/// <param name="disableInitialResultLoading">Pass true to defer the loading of results until a filtering post-back executes.</param>
	/// <param name="dataUpdateModificationMethod">The modification method for the page’s data-update modification.</param>
	/// <param name="isAutoDataUpdater">Pass true to force a post-back when a hyperlink is clicked.</param>
	/// <param name="pageLoadPostBack">A post-back that will be triggered automatically by the browser when the page is finished loading. If this is not null, the
	/// framework will hide all content on the page and show a loading icon instead.</param>
	public FilterPageContent(
		Func<IReadOnlyCollection<FlowComponent>> filterContentGetter, Func<IReadOnlyCollection<FlowComponent>> resultContentGetter,
		ElementClassSet? bodyClasses = null, ActionComponentSetupsParameter? pageActions = null, bool disableInitialResultLoading = false,
		Action? dataUpdateModificationMethod = null, bool isAutoDataUpdater = false, ActionPostBack? pageLoadPostBack = null ) {
		content = new UiPageContent(
			bodyClasses: bodyClasses,
			pageActions: pageActions,
			omitContentBox: true,
			dataUpdateModificationMethod: dataUpdateModificationMethod,
			isAutoDataUpdater: isAutoDataUpdater,
			pageLoadPostBack: pageLoadPostBack );

		var loadResults = ComponentStateItem.Create( "loadResults", !disableInitialResultLoading, _ => true, false );
		if( disableInitialResultLoading )
			content.Add( loadResults );

		var updateRegionSet = new UpdateRegionSet();
		const string focusKey = "filter";
		content.Add(
			FormState.ExecuteWithActions(
				PostBack.CreateIntermediate(
						updateRegionSet,
						modificationMethod: () => loadResults.Value = true,
						reloadBehaviorGetter: () => new PageReloadBehavior( focusKey: focusKey ) )
					.Add( FormState.Current.DataModificationActions ),
				() => new FlowAutofocusRegion(
					AutofocusCondition.InitialRequest().Or( AutofocusCondition.PostBack( focusKey ) ),
					new Section( "Filters", filterContentGetter(), style: SectionStyle.Box ).ToCollection() ) ) );

		var resultContent = resultContentGetter();
		content.Add(
			new FlowIdContainer(
				loadResults.Value ? new Section( resultContent.Any() ? resultContent : "No results".ToComponents(), style: SectionStyle.Box ).ToCollection() : [ ],
				updateRegionSets: updateRegionSet ) );
	}

	protected internal override PageContent GetContent() => content;
}