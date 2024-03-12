using Humanizer;

// EwlPage

namespace EnterpriseWebLibrary.Website.TestPages;

partial class KeyboardFocus {
	protected override PageContent getContent() {
		const string focusKey = "secondSection";
		return new UiPageContent(
				omitContentBox: true,
				contentFootActions:
				new ButtonSetup(
						"Submit with Full Post-Back",
						behavior:
						new PostBackBehavior(
							postBack: PostBack.CreateFull( id: "full", actionGetter: () => new PostBackAction( new PageReloadBehavior( focusKey: focusKey ) ) ) ) )
					.Append(
						new ButtonSetup(
							"Submit with Intermediate Post-Back",
							behavior:
							new PostBackBehavior(
								postBack: PostBack.CreateIntermediate(
									null,
									id: "intermediate",
									reloadBehaviorGetter: () => new PageReloadBehavior( focusKey: focusKey ) ) ) ) )
					.Materialize() )
			.Add(
				new FlowAutofocusRegion(
					AutofocusCondition.InitialRequest(),
					new Section(
						"First Section",
						FormItemList.CreateStack().AddItems( getFormItems() ).ToCollection(),
						style: SectionStyle.Box,
						postHeadingComponents: new SideComments( "Initially focused".ToComponents() ).ToCollection() ).ToCollection() ) )
			.Add(
				new FlowAutofocusRegion(
					AutofocusCondition.PostBack( focusKey ),
					new Section(
						"Second Section",
						FormItemList.CreateStack().AddItems( getFormItems() ).ToCollection(),
						style: SectionStyle.Box,
						postHeadingComponents: new SideComments( "Focused after submission".ToComponents() ).ToCollection() ).ToCollection() ) );
	}

	private IReadOnlyCollection<FormItem> getFormItems() =>
		Enumerable.Range( 1, 3 )
			.Select(
				i => new DataValue<string>().ToTextControl( true, value: "" ).ToFormItem( label: $"{i.ToOrdinalWords().CapitalizeString()} control".ToComponents() ) )
			.Materialize();
}