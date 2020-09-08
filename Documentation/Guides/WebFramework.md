# Using the web framework


## Setup: creating a system and database

To follow along step by step with this guide, you’ll need our example system and database.

First follow the first two sections of the [Getting Started guide](GettingStarted.md): Creating a New System and Adding a Database. Name the system **Bicycle Service Manager** and use a base namespace of `ServiceManager`. Use SQL Server for the database.

Create a `Library/Configuration/Database Updates.sql` file, paste [this script](WebFrameworkSupplements/DatabaseScript.sql) into it, and save.

Then open `Library/Configuration/Development.xml` and replace the empty `<database>` element with this block:

```XML
<database>
	<rowConstantTables>
		<table tableName="ServiceTypes" nameColumn="ServiceTypeName" valueColumn="ServiceTypeId" />
	</rowConstantTables>
	<SmallTables>
		<Table>ServiceTypes</Table>
	</SmallTables>
</database>
```

 Run `Update-DependentLogic`. This will give you the schema, reference data, and data-access layer needed for the rest of this guide. See the [Database Migration](DatabaseMigration.md) and [Data Access](DataAccess.md) guides to learn how all of this works.


## Adding pages

We’re going to build a simple service-order management system for a bicycle repair shop, consisting of two pages: a list of service orders and a form to create/update a service order.

Add a new item to the `Website` project and select the **EWF UI Page** template. Name the page `ServiceOrders`. This will be the list page.

For the form, add another page in the same way naming it `ServiceOrder` (no “s” at the end). Open up `ServiceOrder.aspx.cs` and paste the following line between the using directives and namespace declaration:

```C#
// Parameter: int? serviceOrderId
```

This line declares a URL query parameter that will be incorporated into the generated code for the page. Speaking of generated code, run `Update-DependentLogic` now to generate the code for both pages we just added. You’ll see the purpose of this a bit later.


## Building a simple page

Let’s start our implementation with the list page. For now, since we don’t yet have any service orders in the database, we won’t even add a list; we’ll just add a hyperlink to the new-order form. Open `ServiceOrders.aspx.cs`. Add the following code to `loadData`:

```C#
ph.AddControlsReturnThis(
	new EwfHyperlink( new ServiceOrder.Info( null ), new ButtonHyperlinkStyle( "New service order", buttonSize: ButtonSize.Large ) )
		.ToCollection().GetControls()
);
```

Let’s examine this. First of all, notice there is no HTML. We made the deliberate decision to use pure C# instead of an HTML template syntax such as Razor because pages built in this framework typically use very little HTML, directly. They use higher-level components that abstract away most HTML-level details. For example, we’ve configured the `EwfHyperlink` component here to look like a large button without specifying a `class` attribute or anything else that may be required in the HTML to make that happen.

The `ph.AddControlsReturnThis` method, in conjunction with `.ToCollection().GetControls()`, adds a component to the page. Don’t pay much attention to these details as they’re left over from the framework’s Web Forms heritage and will soon be replaced with a simpler and more functional way of adding items to a page.

The first argument to the `EwfHyperlink` constructor, `new ServiceOrder.Info( null )`, is a reference to the form page with a null `serviceOrderId`. We’ll use null to represent creating a new order. This page reference obviates the need for a URL and provides additional benefits. It is statically checked by the compiler, ensuring the existence of the page and parameter. And it is run during the rendering of the hyperlink, ensuring a valid parameter value according to the page’s own assertions. You’ll see this in the next section.

Click `ServiceOrders.aspx` in the Solution Explorer and then select Start Without Debugging from the Debug menu. You’ll see the list page, with the single large button you added. The button sits within a user interface that is provided by the framework, called the **EWF UI**. You can opt out of this UI but it’s powerful enough to be used even in large enterprise applications.


## Using page information classes

Open up `ServiceOrder.aspx.cs` (the form page) and paste the following above `loadData`:

```C#
partial class Info {
	internal ServiceOrdersTableRetrieval.Row ServiceOrder { get; private set; }

	protected override void init() {
		if( ServiceOrderId.HasValue )
			ServiceOrder = ServiceOrdersTableRetrieval.GetRowMatchingId( ServiceOrderId.Value );
	}

	protected override ResourceInfo createParentResourceInfo() => new ServiceOrders.Info();
}
```

This code adds functionality to the `Info` class that is automatically generated for the page. `Info` contains logic for the page that is useful even when the page is not actually being requested, e.g. when we create a hyperlink to the page as we did above. `Info` objects can be directly created and are also created automatically when their page is requested.

The `init` method is called by the constructor. In our implementation here, we load the query-parameter-specified service order (if the ID is not null) into a property, simultaneously validating the parameter by throwing an exception from `GetRowMatchingId` if the row doesn’t exist. When this code runs during the rendering of another page (e.g. to build a hyperlink as we did above), the exception will not be handled and will result in an error report to the developer. On the other hand, when this code runs during a request for *this* page, the framework will convert the exception into an HTTP 404 status code since there’s no action a developer can take if a user (or crawler) attempts to visit an invalid page.

The `createParentResourceInfo` method specifies the parent of this page, which is used to inherit security settings and for other purposes such as automatic navigational breadcrumbs for users.


## Creating a form

Paste the following complete form implementation into `loadData`:

```C#
var mod = info.ServiceOrderId.HasValue ? info.ServiceOrder.ToModification() : ServiceOrdersModification.CreateForInsert();
FormState.ExecuteWithDataModificationsAndDefaultAction(
	PostBack.CreateFull(
			firstModificationMethod: () => {
				if( !info.ServiceOrderId.HasValue )
					mod.ServiceOrderId = MainSequence.GetNextValue();
				mod.Execute();
			},
			actionGetter: () => new PostBackAction( info.ParentResource ) )
		.ToCollection(),
	() => {
		ph.AddControlsReturnThis(
			FormItemList.CreateStack(
					items: mod.GetCustomerNameTextControlFormItem( false, value: info.ServiceOrderId.HasValue ? null : "" )
						.Append( mod.GetCustomerEmailEmailAddressControlFormItem( false, value: info.ServiceOrderId.HasValue ? null : "" ) )
						.Append( mod.GetBicycleDescriptionTextControlFormItem( false, value: info.ServiceOrderId.HasValue ? null : "" ) )
						.Append(
							mod.GetServiceTypeIdRadioListFormItem(
								RadioListSetup.Create(
									ServiceTypesTableRetrieval.GetAllRows().Select( i => SelectListItem.Create( (int?)i.ServiceTypeId, i.ServiceTypeName ) ) ),
								value: info.ServiceOrderId.HasValue ? null : new SpecifiedValue<int?>( null ) ) )
						.Materialize() )
				.ToCollection()
				.GetControls() );

		EwfUiStatics.SetContentFootActions( new ButtonSetup( "Submit" ).ToCollection() );
	} );
```

If you go back to `ServiceOrders.aspx` in your browser and click the button, you’ll see the form. Try it out. When you submit, you’ll go back to the list page--but won’t see your new order yet since we haven’t implemented this.

Let’s break down the code above.

First we have the creation of a `ServiceOrdersModification` object:

```C#
var mod = info.ServiceOrderId.HasValue ? info.ServiceOrder.ToModification() : ServiceOrdersModification.CreateForInsert();
```

If we’re updating an order, we set the object up to modify the row we loaded in `Info.init`. For a new order we set it up to do a row insert. Learn more in the [Data Access](DataAccess.md) guide.

Then we have a call to `FormState.ExecuteWithDataModificationsAndDefaultAction`. The first argument is a `PostBack` object and the second is a method (starting with `ph.AddControlsReturnThis`). There are a couple of important concepts here.

First is the `PostBack` object, which represents a server-side action that executes when the browser submits the form (in this framework there is always one form per page) with an HTTP `POST` request. Only one `PostBack` executes per request. `PostBack` execution has three stages:

1.  Form validation
2.  Modification method
3.  Post-modification action (e.g. navigation)

The **form validation** stage involves executing all validation objects that were added to the `PostBack`, in the order they were added. We don’t directly add validations to a `PostBack`, and this leads us to the second important concept: `FormState.ExecuteWithDataModificationsAndDefaultAction`.

This method takes one or more `DataModification` objects. `DataModification` is a base class of `PostBack` with one other derived type that isn’t important at the moment. The `DataModification`/`PostBack` objects are made available while the passed-in method executes so that whenever a validation is created, it is added to those objects.

You can create validations directly, but they are most commonly created by form-control components (for example text controls or drop-down lists). The validations created by form controls are responsible for taking the posted-back values from the controls, validating them (of course), and preparing them to be persisted. In this page the validations are hidden under the hood but they place the values in the `ServiceOrdersModification` object.

The **modification method** stage of `PostBack` execution runs the specified method:

```C#
if( !info.ServiceOrderId.HasValue )
	mod.ServiceOrderId = MainSequence.GetNextValue();
mod.Execute();
```

This is where the persistence actually happens. If this is a new service order we create an ID. We then execute the database modification.

The **post-modification action** stage of the `PostBack` determines what happens after data is modified. Sometimes you’ll want the user to stay on the page, with or without extra behavior such as a change in keyboard focus or a file download. But in this case we want to navigate them back to the list page:

```C#
() => new PostBackAction( info.ParentResource )
```

We don’t refererence the list page directly. Instead we just navigate back to the parent page, which does the same thing since we already designated the list as the parent. But this makes our page more maintainable in the event we change the parent.

Now let’s break down the method we are passing to `FormState.ExecuteWithDataModificationsAndDefaultAction`. The first statement spans several lines to create a `FormItemList` component that contains several `FormItem` objects. A form item is an abstract container that includes content (usually a form control), a label, and a validation object. Here, we create the form items using generated methods in the `ServiceOrdersModification` class. For example:

```C#
mod.GetCustomerEmailEmailAddressControlFormItem( false, value: info.ServiceOrderId.HasValue ? null : "" )
```

This creates a form item containing an email-address form control. The first parameter specifies that it should require a value from the user. The second parameter (`value`) determines the initial value in the control. When updating a service order, we pass `null` which tells the control to get its value from the `ServiceOrdersModification` object. For new orders we specify the empty string.

The form control has full email-address semantics, bringing up the correct keyboard on mobile devices and enforcing a valid email address according to the spec.

You can display form items in several different ways, but the most common way is to put them in a `FormItemList`. This component lays out a list of form items in a format of your choice. Here we’re using a “stack”:

```C#
FormItemList.CreateStack(
	items: mod.GetCustomerNameTextControlFormItem( false, value: info.ServiceOrderId.HasValue ? null : "" )
		.Append( mod.GetCustomerEmailEmailAddressControlFormItem( false, value: info.ServiceOrderId.HasValue ? null : "" ) )
		.Append( mod.GetBicycleDescriptionTextControlFormItem( false, value: info.ServiceOrderId.HasValue ? null : "" ) )
		.Append(
			mod.GetServiceTypeIdRadioListFormItem(
				RadioListSetup.Create(
					ServiceTypesTableRetrieval.GetAllRows().Select( i => SelectListItem.Create( (int?)i.ServiceTypeId, i.ServiceTypeName ) ) ),
				value: info.ServiceOrderId.HasValue ? null : new SpecifiedValue<int?>( null ) ) )
		.Materialize() )
```

If you wanted to switch to a wrapping list or a grid, for example, you’d just change `CreateStack` to `CreateWrapping` or `CreateGrid`.

The other statement in the method adds a button to the page:

```C#
EwfUiStatics.SetContentFootActions( new ButtonSetup( "Submit" ).ToCollection() );
```

Instead of creating an `EwfButton` component ourselves, we create a `ButtonSetup` with the functionality we want, and let the EWF UI decide on the style.


## Displaying data in a table

Open `ServiceOrders.aspx.cs`. Add this block of code to `loadData`:

```C#
ph.AddControlsReturnThis(
	EwfTable
		.Create(
			caption: "Existing service orders",
			fields: new EwfTableField( size: 1.ToPercentage() ).Append( new EwfTableField( size: 3.ToPercentage() ) )
				.Append( new EwfTableField( size: 6.ToPercentage() ) )
				.Append( new EwfTableField( size: 2.ToPercentage() ) )
				.Materialize(),
			headItems: EwfTableItem.Create(
					"ID".ToCell().Append( "Customer".ToCell() ).Append( "Bicycle".ToCell() ).Append( "Service type".ToCell() ).Materialize() )
				.ToCollection(),
			defaultItemLimit: DataRowLimit.Fifty )
		.AddData(
			ServiceOrdersTableRetrieval.GetRows(),
			i => EwfTableItem.Create(
				i.ServiceOrderId.ToString()
					.ToCell()
					.Append( i.CustomerName.ToCell() )
					.Append( i.BicycleDescription.ToCell() )
					.Append( ServiceTypesTableRetrieval.GetRowMatchingId( i.ServiceTypeId ).ServiceTypeName.ToCell() )
					.Materialize(),
				setup: EwfTableItemSetup.Create( activationBehavior: ElementActivationBehavior.CreateHyperlink( new ServiceOrder.Info( i.ServiceOrderId ) ) ) ) )
		.ToCollection()
		.GetControls() );
```

You’ll now see a table of existing service orders on the page, in which clicking on a row navigates to the form for that order. Let’s look at the call to `EwfTable.Create`. The `caption` is a title for the table. `fields` configures the columns. In this case we’re using a “12 column grid” system to specify their widths; the `.ToPercentage()` calls are misleading here because the table treats the values as simple proportions and doesn’t require them to add up to 100%.

`headItems` specifies the header row(s). The framework uses the term “item” instead of row to be a bit more abstract—there’s another table component, `ColumnPrimaryTable`, that renders items as columns instead of rows. `EwfTableItem.Create` takes a collection of cells. We typically use the chained `.Append` syntax instead of collection initializers for maintainability reasons. In the future, maybe you’d want to use a helper method to create a chunk of the cells. In that case you’d just add a `.Concat( getCells() )` expression to the chain.

`defaultItemLimit` limits the number of items initially visible, to improve page performance. The user can display more items if desired using buttons built into the table component.

The `AddData` method takes two parameters, a sequence of data rows and a selector function that creates a table item from a data row. The functions will never execute for rows that aren’t drawn due to item limiting.

In the selector function we create an item that includes a setup:

```C#
setup: EwfTableItemSetup.Create( activationBehavior: ElementActivationBehavior.CreateHyperlink( new ServiceOrder.Info( i.ServiceOrderId ) ) )
```

The setup object lets us pass an `ElementActivationBehavior`, which lets us make the table row act like a hyperlink and navigate to the form when clicked.


## Using temporary state during a post-back

What we’ve built above covers the staple features of the framework that you’ll need for almost any application. But let’s go back to `ServiceOrder.aspx.cs` and look at some more-advanced features that are just as important when building complex forms.

First we’ll cover temporary state. The form controls that we’re already using store their validated values in the `ServiceOrdersModification` object, with the intention that the values will be persisted in our database. But what if you want to have a form control whose posted-back value shouldn’t be directly persisted, and only used to influence the validation of other controls?

Imagine a checkbox with another form control nested beneath it. When the box is checked, the nested value should be persisted, but when the box is unchecked the nested value should be ignored and `null` should be persisted instead. The true/false state of the checkbox should only be used to determine this behavior.

Let’s build this. In our `ServiceOrders` database table we have a `CustomerBudget` nullable column, which we’ll use to add a budget control to our form, nested beneath a checkbox. We’ll start by adding this as the first line in the method we are passing to `FormState.ExecuteWithDataModificationsAndDefaultAction`:

```C#
var customerHasBudget = new DataValue<bool>();
```

This is our temporary state. `DataValue` is a simple type that contains a value (a `bool` in this case) and tracks whether the value has been initialized and whether it has changed. It’s a good fit for temporary state because it lets us say, as we do on the line above, that the state is currently uninitialized and will throw an exception if anything attempts to get the value. We won’t have a meaningful value until the checkbox’s validation executes.

We’ll also add this line, directly under the line above:

```C#
Action budgetClearer = null;
```

This will be a method called during validation that is responsible for storing `null` as the value to be persisted for the nested budget control, in the case that the control is ignored.

Now we’ll make a helper method that creates the nested components and initializes `budgetClearer` (called `dataClearer` in this scope):

```C#
private IReadOnlyCollection<FlowComponent> getBudgetComponents( ServiceOrdersModification mod, out Action dataClearer ) {
	dataClearer = () => mod.CustomerBudget = null;
	return mod.GetCustomerBudgetNumberControlFormItem(
			label: "Amount ($)".ToComponents().Append( new LineBreak() ).Append( new SideComments( "Multiple of $5, minimum $10".ToComponents() ) ).Materialize(),
			value: info.ServiceOrderId.HasValue ? null : new SpecifiedValue<decimal?>( null ),
			allowEmpty: false,
			minValue: 10,
			valueStep: 5 )
		.ToComponentCollection();
}
```

The creation of the budget form item here is just like the creation of the earlier form items, but with a few more parameters to get the behavior just right.

Finally, we’ll put together all the pieces above by adding a checkbox to our chain of `Append` calls for the `FormItemList`. Put this right before the `.Materialize()` at the end:

```C#
.Append(
	customerHasBudget.ToFlowCheckbox(
			"Customer has budget".ToComponents(),
			setup: FlowCheckboxSetup.Create(
				nestedContentGetter: () => FormState.ExecuteWithValidationPredicate(
					() => customerHasBudget.Value,
					() => getBudgetComponents( mod, out budgetClearer ) ) ),
			value: info.ServiceOrderId.HasValue && info.ServiceOrder.CustomerBudget.HasValue,
			additionalValidationMethod: validator => {
				if( !customerHasBudget.Value )
					budgetClearer();
			} )
		.ToFormItem( label: "Budget".ToComponents() ) )
```

We create the checkbox using an extension method on `DataValue<bool>` called `ToFlowCheckbox`, which gives us a checkbox with a validation that puts the posted-back value into the `DataValue`. The first argument is our label. The second creates a setup object with `nestedContentGetter`: a method that executes during the creation of the checkbox, after the checkbox’s own validation is created.

`FormState.ExecuteWithValidationPredicate` executes a method (the second parameter) with the specified predicate method (the first parameter) applied to any validations that are created. The predicate will execute before the validations, and if it returns `false` the validations will be skipped. That’s the reason, for example, we can specify `allowEmpty: false` for the budget control without having it ever block the post-back when the box is unchecked.

The third argument for `ToFlowCheckbox` determines whether the box is initially checked, which will only be the case if we’re updating a service order and a budget exists.

The fourth argument, `additionalValidationMethod`, runs during the checkbox’s validation after the built-in part that populates the `DataValue`. Every form-item-creation method includes this parameter and you’d typically use it to perform additional domain-specific validation for the control, adding your own error messages to `validator`. But in this case we’re using it to run the `budgetClearer` when the box is unchecked.

Try out the form again and see how it works with the new checkbox and nested control!


## Modifying a page on the client side


## Modifying a page on the server side


## Using component state with an underlying durable value


## Managing keyboard focus


## Securing pages (section needs work)

*	The page usees the entity setup’s security that’s in the same folder. It does not matter if the entity setup has the page as a tab. If it’s in the same folder it affects it.
*	You can’t be less-restrictive than your parent’s security.
*	The security is checked for the whole tree. The page and its parent and its parent and its parent...
*	The folder structure does not matter other than the entity setup being in the same folder as the page


## Horizontal/vertical tabs (section needs work; also move elsewhere since uncommon)

Have your page’s `Info` class implement the `TabModeOverrider` interface. Then implement the only method in that interface, `GetTabMode`, and return `TabMode.Horizontal`.