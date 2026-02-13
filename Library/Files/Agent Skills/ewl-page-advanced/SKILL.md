---
name: ewl-page-advanced
description: Advanced EWL page patterns including forms, PostBack, validation, client-side modification, and component state
---

## Creating a form with PostBack

Forms use `FormState.ExecuteWithActions` to connect form controls to a
`PostBack`. PostBack execution has three stages: form validation, modification
method, and post-modification action.

```csharp
protected override PageContent getContent() {
	var mod = ServiceOrderId.HasValue
		? serviceOrderRow.ToModification()
		: ServiceOrdersModification.CreateForInsert();

	return FormState.ExecuteWithActions(
		PostBack.CreateFull(
			modificationMethod: () => {
				if( !ServiceOrderId.HasValue )
					mod.ServiceOrderId = MainSequence.GetNextValue();
				mod.Execute();
			},
			actionGetter: () => new PostBackAction( ParentResource ) ),
		() => {
			var content = new UiPageContent( contentFootActions: new ButtonSetup( "Submit" ) );
			var formItemList = FormItemList.CreateStack()
				.AddItem( mod.GetCustomerNameFormItem( false ) )
				.AddItem( mod.GetCustomerEmailFormItem( false ) );
			content.Add( formItemList );
			return content;
		} );
}
```

## Form items from modifications

Modification objects generate form controls with built-in validation. The first
boolean parameter typically controls whether the field is optional.

```csharp
mod.GetCustomerNameFormItem( false )
mod.GetCustomerEmailFormItem( false )
mod.GetServiceTypeIdRadioListFormItem(
	RadioListSetup.Create( items.Select( i => SelectListItem.Create( (int?)i.Id, i.Name ) ) ) )
mod.GetNotesFormItem( true, controlSetup: TextControlSetup.Create( numberOfRows: 4 ) )
mod.GetBudgetFormItem( label: "Amount".ToComponents(), allowEmpty: false, minValue: 10, valueStep: 5 )
```

## Validation predicates

Use `FormState.ExecuteWithValidationPredicate` to conditionally skip validation
for hidden or irrelevant controls:

```csharp
FormState.ExecuteWithValidationPredicate(
	() => customerHasBudget.Value,
	() => mod.GetBudgetFormItem( false ).ToComponentCollection() );
```

## Temporary state with DataValue

`DataValue<T>` tracks a temporary value that is not directly persisted:

```csharp
var customerHasBudget = new DataValue<bool>( ServiceOrderId.HasValue, () => serviceOrderRow.Budget.HasValue );
```

Use with checkboxes via `ToFlowCheckbox`:

```csharp
customerHasBudget.ToFlowCheckbox(
	"Customer has budget".ToComponents(),
	setup: FlowCheckboxSetup.Create( nestedContentGetter: () => /* nested controls */ ),
	additionalValidationMethod: validator => {
		if( !customerHasBudget.Value )
			budgetClearer!();
	} )
```

## Client-side page modification

`PageModificationValue<T>` enables instant client-side changes based on form
control values:

```csharp
var serviceTypeIdPmv = new PageModificationValue<int?>();

// Connect to a radio list
mod.GetServiceTypeIdRadioListFormItem(
	RadioListSetup.Create( items, itemIdPageModificationValue: serviceTypeIdPmv ) );

// Toggle visibility based on value
var displaySetup = serviceTypeIdPmv
	.ToCondition( ( (int?)ServiceTypesRows.GeneralService ).ToCollection() )
	.ToDisplaySetup();

// Toggle CSS classes based on value
var classes = serviceTypeIdPmv
	.ToCondition( ( (int?)ServiceTypesRows.FlatTireRepair ).ToCollection() )
	.ToElementClassSet( ElementClasses.FlatTire );
```

## Intermediate post-backs and component state

Intermediate post-backs update parts of a page without fully processing the
form. They require declaring which regions change.

```csharp
var requestLineCount = ComponentStateItem.Create( "requestLineCount", initialValue, v => v > 0, false );
var updateRegion = new UpdateRegionSet();

// Button triggers intermediate post-back
new EwfButton(
	new StandardButtonStyle( "Add line" ),
	behavior: new PostBackBehavior(
		postBack: PostBack.CreateIntermediate(
			updateRegion,
			id: "addLine",
			modificationMethod: () => requestLineCount.Value += 1 ) ) );
```

`ComponentStateItem` stores data in a hidden field across intermediate
post-backs. Place it within the component tree so it is automatically
discarded when its containing region is rebuilt by another post-back.

Use `tailUpdateRegions` in `ComponentListSetup` to declare that new items
added to a list belong to a specific update region.

## Managing keyboard focus

Wrap components in `FlowAutofocusRegion` to control focus:

```csharp
// Focus first control on initial request
content.Add( new FlowAutofocusRegion( AutofocusCondition.InitialRequest(), formItemList.ToCollection() ) );

// Focus after a specific post-back using a focus key
PostBack.CreateIntermediate( region, id: "add", reloadBehaviorGetter: () => new PageReloadBehavior( focusKey: "add" ) );
// In ComponentListSetup:
lastItemAutofocusCondition: AutofocusCondition.PostBack( "add" )
```

## Auto-saving edit pages

For edit pages that save automatically (no explicit Submit button):

```csharp
var content = new UiPageContent(
	dataUpdateModificationMethod: () => { foreach( var m in modMethods ) m(); },
	isAutoDataUpdater: true );
```
