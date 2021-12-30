# Using the web framework

Last updated for Enterprise Web Library version 72.


## Setup: creating a system and database

To follow along step by step with this guide, you’ll need our example system and database. First follow the first two sections of the [Getting Started guide](GettingStarted.md): Creating a New System and Adding a Database. Name the system **Bicycle Service Manager** and use a base namespace of `ServiceManager`. Use SQL Server for the database.

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

We’re going to build a simple service-order management system for a bicycle repair shop, consisting of two pages: a list of service orders and a form to create/update a service order. We can use the existing home page (`Home.cs` in the `Website` project) as the list page.

For the form, add a class called `ServiceOrder` to the `Website` project. In `ServiceOrder.cs`, first change the declaration from `public class ServiceOrder` to `partial class ServiceOrder`. Then paste the following lines between the using directives and namespace declaration:

```C#
// EwlPage
// Parameter: int? serviceOrderId
```

The first line tells the framework to generate code for this class, to make it a page, and the second declares a URL query parameter that will be incorporated into this generated code. Speaking of generated code, run `Update-DependentLogic` now to create it.

Now we need to set up a URL pattern for the form page. Open `Home.cs`, add a using directive for `System.Linq`, and replace the implementation of `getChildUrlPatterns` with the following expression:

```C#
RequestDispatchingStatics.GetFrameworkUrlPatterns().Append( ServiceOrder.UrlPatterns.ServiceOrderIdPositiveInt( "create" ) )
```

This adds child URLs to the home page for every existing service order ID (e.g. `/123`) and one more for creating a new service order: `/create`.

There is one more step we must take to make this work. Open `ServiceOrder.cs` and add the following method to the class:

```C#
protected override ResourceBase createParentResource() => new Home();
```

This method specifies the parent of this page, which is used to inherit security settings and for other purposes such as automatic navigational breadcrumbs for users. It also determines the page’s canonical URL.


## Building a simple page

Let’s start our implementation with the list page. For now, since we don’t yet have any service orders in the database, we won’t even add a list; we’ll just add a hyperlink to the new-order form. Open `Home.cs`. Replace the implementation of `getContent` with this expression:

```C#
new UiPageContent().Add( new EwfHyperlink( new ServiceOrder( null ), new ButtonHyperlinkStyle( "New service order", buttonSize: ButtonSize.Large ) ) )
```

Let’s examine this. First of all, notice there is no HTML. We made the deliberate decision to use pure C# instead of an HTML template syntax such as Razor because pages built in this framework typically use very little HTML, directly. They use higher-level components that abstract away most HTML-level details. For example, we’ve configured the `EwfHyperlink` component here to look like a large button without specifying a `class` attribute or anything else that may be required in the HTML to make that happen.

The first argument to the `EwfHyperlink` constructor, `new ServiceOrder( null )`, is a reference to the form page with a null `serviceOrderId`. We’ll use null to represent creating a new order. This page reference obviates the need for a URL and provides additional benefits. It is statically checked by the compiler, ensuring the existence of the page and parameter. And it is run during the rendering of the hyperlink, ensuring a valid parameter value according to the page’s own assertions. You’ll see this in the next section.

The `Add` method on `UiPageContent`, which is chainable, adds components to the page.

Click the `Website` project in the Solution Explorer and then select Start Without Debugging from the Debug menu. You’ll see the list page, with the single large button you added. The button sits within a user interface that is provided by the framework, called the **EWF UI**. You can opt out of this UI (by replacing `UiPageContent` with `BasicPageContent`) but it’s powerful enough to be used even in large enterprise applications.


## Using page initialization logic

Open up `ServiceOrder.cs` (the form page) and paste the following within the class, above `createParentResource`:

```C#
private ServiceOrdersTableRetrieval.Row serviceOrderRow;

protected override void init() {
	if( ServiceOrderId.HasValue )
		serviceOrderRow = ServiceOrdersTableRetrieval.GetRowMatchingId( ServiceOrderId.Value );
}
```

The `init` method is called by the constructor and runs even when the page is not actually being requested, e.g. when we create a hyperlink to the page as we did above.

In our implementation here, we load the query-parameter-specified service order (if the ID is not null) into a field, simultaneously validating the parameter by throwing an exception from `GetRowMatchingId` if the row doesn’t exist. When this code runs during the rendering of another page (e.g. to build a hyperlink as we did above), the exception will not be handled and will result in an error report to the developer. On the other hand, when this code runs during a request for *this* page, the framework will convert the exception into an HTTP 404 status code since there’s no action a developer can take if a user (or crawler) attempts to visit an invalid page.


## Creating a form

Paste the following complete form implementation into the class, below the `createParentResource` method:

```C#
protected override PageContent getContent() {
	var mod = ServiceOrderId.HasValue ? serviceOrderRow.ToModification() : ServiceOrdersModification.CreateForInsert();
	return FormState.ExecuteWithDataModificationsAndDefaultAction(
		PostBack.CreateFull(
				modificationMethod: () => {
					if( !ServiceOrderId.HasValue )
						mod.ServiceOrderId = MainSequence.GetNextValue();
					mod.Execute();
				},
				actionGetter: () => new PostBackAction( ParentResource ) )
			.ToCollection(),
		() => {
			var content = new UiPageContent( contentFootActions: new ButtonSetup( "Submit" ).ToCollection() );

			var formItemList = FormItemList.CreateStack();

			formItemList.AddItem( mod.GetCustomerNameTextControlFormItem( false, value: ServiceOrderId.HasValue ? null : "" ) );
			formItemList.AddItem( mod.GetCustomerEmailEmailAddressControlFormItem( false, value: ServiceOrderId.HasValue ? null : "" ) );
			formItemList.AddItem( mod.GetBicycleDescriptionTextControlFormItem( false, value: ServiceOrderId.HasValue ? null : "" ) );

			formItemList.AddItem(
				mod.GetServiceTypeIdRadioListFormItem(
					RadioListSetup.Create( ServiceTypesTableRetrieval.GetAllRows().Select( i => SelectListItem.Create( (int?)i.ServiceTypeId, i.ServiceTypeName ) ) ),
					value: ServiceOrderId.HasValue ? null : new SpecifiedValue<int?>( null ) ) );

			content.Add( formItemList );

			return content;
		} );
}
```

If you go back to the home page in your browser and click the button, you’ll see the form. Try it out. When you submit, you’ll go back to the list page--but won’t see your new order yet since we haven’t implemented this.

Let’s break down the code above.

First we have the creation of a `ServiceOrdersModification` object:

```C#
var mod = ServiceOrderId.HasValue ? serviceOrderRow.ToModification() : ServiceOrdersModification.CreateForInsert();
```

If we’re updating an order, we set the object up to modify the row we loaded in `init`. For a new order we set it up to do a row insert. Learn more in the [Data Access](DataAccess.md) guide.

Then we have a call to `FormState.ExecuteWithDataModificationsAndDefaultAction`. The first argument is a `PostBack` object and the second is a method (starting with `var content`). There are a couple of important concepts here.

First is the `PostBack` object, which represents a server-side action that executes when the browser submits the form (in this framework there is always one form per page) with an HTTP `POST` request. Only one `PostBack` executes per request. `PostBack` execution has three stages:

1.  Form validation
2.  Modification method
3.  Post-modification action (e.g. navigation)

The **form validation** stage involves executing all validation objects that were added to the `PostBack`, in the order they were added. We don’t directly add validations to a `PostBack`, and this leads us to the second important concept: `FormState.ExecuteWithDataModificationsAndDefaultAction`.

This method takes one or more `DataModification` objects. `DataModification` is a base class of `PostBack` with one other derived type that isn’t important at the moment. The `DataModification`/`PostBack` objects are made available while the passed-in method executes so that whenever a validation is created, it is added to those objects.

You can create validations directly, but they are most commonly created by form-control components (for example text controls or drop-down lists). The validations created by form controls are responsible for taking the posted-back values from the controls, validating them (of course), and preparing them to be persisted. In this page the validations are hidden under the hood but they place the values in the `ServiceOrdersModification` object.

The **modification method** stage of `PostBack` execution runs the specified method:

```C#
if( !ServiceOrderId.HasValue )
	mod.ServiceOrderId = MainSequence.GetNextValue();
mod.Execute();
```

This is where the persistence actually happens. If this is a new service order we create an ID. We then execute the database modification.

The **post-modification action** stage of the `PostBack` determines what happens after data is modified. Sometimes you’ll want the user to stay on the page, with or without extra behavior such as a change in keyboard focus or a file download. But in this case we want to navigate them back to the list page:

```C#
() => new PostBackAction( ParentResource )
```

We don’t refererence the list page directly. Instead we just navigate back to the parent page, which does the same thing since we already designated the list as the parent. But this makes our page more maintainable in the event we change the parent.

Now let’s break down the method we are passing to `FormState.ExecuteWithDataModificationsAndDefaultAction`. The first statement creates a `UiPageContent` object with a built-in button. We could have created an `EwfButton` component ourselves, but by using `ButtonSetup` with the functionality we want, we’re letting the EWF UI decide on the style.

The next statement creates a `FormItemList` component, to which we will add several `FormItem` objects. A form item is an abstract container that includes content (usually a form control), a label, and a validation object. Here, we create the form items using generated methods in the `ServiceOrdersModification` class. For example:

```C#
mod.GetCustomerEmailEmailAddressControlFormItem( false, value: ServiceOrderId.HasValue ? null : "" )
```

This creates a form item containing an email-address form control. The first parameter specifies that it should require a value from the user. The second parameter (`value`) determines the initial value in the control. When updating a service order, we pass `null` which tells the control to get its value from the `ServiceOrdersModification` object. For new orders we specify the empty string.

The form control has full email-address semantics, bringing up the correct keyboard on mobile devices and enforcing a valid email address according to the spec.

You can display form items in several different ways, but the most common way is to put them in a `FormItemList`. This component lays out a list of form items in a format of your choice. Here we’re using a “stack” by calling `FormItemList.CreateStack`. If you wanted to switch to a wrapping list or a grid, for example, you’d just change `CreateStack` to `CreateWrapping` or `CreateGrid`.


## Displaying data in a table

Open `Home.cs`. Add another `UiPageContent.Add` method to the chain, passing in this block of code:

```C#
EwfTable.Create(
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
			setup: EwfTableItemSetup.Create( activationBehavior: ElementActivationBehavior.CreateHyperlink( new ServiceOrder( i.ServiceOrderId ) ) ) ) )
```

You’ll now see a table of existing service orders on the page, in which clicking on a row navigates to the form for that order. Let’s look at the call to `EwfTable.Create`. The `caption` is a title for the table. `fields` configures the columns. In this case we’re using a “12 column grid” system to specify their widths; the `.ToPercentage()` calls are misleading here because the table treats the values as simple proportions and doesn’t require them to add up to 100%.

`headItems` specifies the header row(s). The framework uses the term “item” instead of row to be a bit more abstract—there’s another table component, `ColumnPrimaryTable`, that renders items as columns instead of rows. `EwfTableItem.Create` takes a collection of cells. We typically use the chained `.Append` syntax instead of collection initializers for maintainability reasons. In the future, maybe you’d want to use a helper method to create a chunk of the cells. In that case you’d just add a `.Concat( getCells() )` expression to the chain.

`defaultItemLimit` limits the number of items initially visible, to improve page performance. The user can display more items if desired using buttons built into the table component.

The `AddData` method takes two parameters, a sequence of data rows and a selector function that creates a table item from a data row. The functions will never execute for rows that aren’t drawn due to item limiting.

In the selector function we create an item that includes a setup:

```C#
setup: EwfTableItemSetup.Create( activationBehavior: ElementActivationBehavior.CreateHyperlink( new ServiceOrder( i.ServiceOrderId ) ) )
```

The setup object lets us pass an `ElementActivationBehavior`, which lets us make the table row act like a hyperlink and navigate to the form when clicked.


## Using temporary state during a post-back

What we’ve built above covers the staple features of the framework that you’ll need for almost any application. But let’s go back to `ServiceOrder.cs` and look at some more-advanced features that are just as important when building complex forms.

First we’ll cover temporary state. The form controls that we’re already using store their validated values in the `ServiceOrdersModification` object, with the intention that the values will be persisted in our database. But what if you want to have a form control whose posted-back value shouldn’t be directly persisted, and only used to influence the validation of other controls?

Imagine a checkbox with another form control nested beneath it. When the box is checked, the nested value should be persisted, but when the box is unchecked the nested value should be ignored and `null` should be persisted instead. The true/false state of the checkbox should only be used to determine this behavior.

Let’s build this. In our `ServiceOrders` database table we have a `CustomerBudget` nullable column, which we’ll use to add a budget control to our form, nested beneath a checkbox. We’ll start by stubbing out a helper method for our new logic:

```C#
private void addServiceDetailFormItems( ServiceOrdersModification mod, FormItemList formItemList ) {
}
```

Add a call to this method in `getContent` between the last `formItemList.AddItem` call and `content.Add( formItemList )`, leaving blank lines above and below since we’ll be expanding it later:

```C#
addServiceDetailFormItems( mod, formItemList );
```

Now add this as the first line in our helper method:

```C#
var customerHasBudget = new DataValue<bool>();
```

This is our temporary state. `DataValue` is a simple type that contains a value (a `bool` in this case) and tracks whether the value has been initialized and whether it has changed. It’s a good fit for temporary state because it lets us say, as we do on the line above, that the state is currently uninitialized and will throw an exception if anything attempts to get the value. We won’t have a meaningful value until the checkbox’s validation executes.

We’ll also add this line, directly under the line above:

```C#
Action budgetClearer = null;
```

This will be a method sometimes called during validation that is responsible for storing `null` as the value to be persisted for the nested budget control, in the case that the control is ignored.

Now we’ll complete the job by creating and adding a form item with our checkbox. Add this as the final line in our helper method:

```C#
formItemList.AddItem(
	customerHasBudget.ToFlowCheckbox(
			"Customer has budget".ToComponents(),
			setup: FlowCheckboxSetup.Create(
				nestedContentGetter: () => FormState.ExecuteWithValidationPredicate(
					() => customerHasBudget.Value,
					() => {
						budgetClearer = () => mod.CustomerBudget = null;
						return mod.GetCustomerBudgetNumberControlFormItem(
								label: "Amount ($)".ToComponents()
									.Append( new LineBreak() )
									.Append( new SideComments( "Multiple of $5, minimum $10".ToComponents() ) )
									.Materialize(),
								value: ServiceOrderId.HasValue ? null : new SpecifiedValue<decimal?>( null ),
								allowEmpty: false,
								minValue: 10,
								valueStep: 5 )
							.ToComponentCollection();
					} ) ),
			value: ServiceOrderId.HasValue && serviceOrderRow.CustomerBudget.HasValue,
			additionalValidationMethod: validator => {
				if( !customerHasBudget.Value )
					budgetClearer();
			} )
		.ToFormItem( label: "Budget".ToComponents() ) );
```

We create the checkbox using an extension method on `DataValue<bool>` called `ToFlowCheckbox`, which gives us a checkbox with a validation that puts the posted-back value into the `DataValue`. The first argument is our label. The second creates a setup object with `nestedContentGetter`: a method that executes during the creation of the checkbox, after the checkbox’s own validation is created.

`FormState.ExecuteWithValidationPredicate` executes a method (the second parameter) with the specified predicate method (the first parameter) applied to any validations that are created. The predicate will execute before the validations, and if it returns `false` the validations will be skipped.

The wrapped method first initializes `budgetClearer` and then returns the nested components. The creation of the budget form item here is just like the creation of the earlier form items, but with a few more parameters to get the behavior just right.

The third argument for `ToFlowCheckbox` determines whether the box is initially checked, which will only be the case if we’re updating a service order and a budget exists.

The fourth argument, `additionalValidationMethod`, runs during the checkbox’s validation after the built-in part that populates the `DataValue`. Every form-item-creation method includes this parameter and you’d typically use it to perform additional domain-specific validation for the control, adding your own error messages to `validator`. But in this case we’re using it to run the `budgetClearer` when the box is unchecked.

Try out the form again and see how it works with the new checkbox and nested control!


## Modifying a page on the client side

You may have noticed that our checkbox automatically toggles the visibility of its nested content when clicked. This type of instant, client-side page modification can be important to user experience. Let’s learn how it works by attaching some of our own modifications to the Service Type radio buttons.

First we need a `PageModificationValue` that matches the data type of the item IDs in the radio list. In `getContent`, add this line directly above the `FormItemList` creation:

```C#
var serviceTypeIdPmv = new PageModificationValue<int?>();
```

We then must connect this page-modification value to the form control. Add a second argument to `RadioListSetup.Create`:

```C#
itemIdPageModificationValue: serviceTypeIdPmv
```

Now, for our first page modification based on this value, we’ll hide the budget form item unless General Service is selected; a budget doesn’t make much sense for flat-tire repair. Add a third parameter to `addServiceDetailFormItems`:

```C#
DisplaySetup displaySetup
```

Then forward the parameter to the checkbox by making this the new first argument of the `ToFormItem` call near the end of the method:

```C#
setup: new FormItemSetup( displaySetup: displaySetup )
```

This connects the visibility of the form item to the `DisplaySetup` parameter we just added. Now modify the method call in `getContent` to make this expression the third argument:

```C#
serviceTypeIdPmv.ToCondition( ( (int?)ServiceTypesRows.GeneralService ).ToCollection() ).ToDisplaySetup()
```

This produces a `DisplaySetup` that is based upon a `PageModificationValueCondition` that is true only when the underlying `PageModificationValue` matches `GeneralService`. Now the form item will only be displayed in that case.

That handles the visibility toggle, but since we’re hiding form controls we need to add another validation predicate to prevent errors in the hidden budget control from blocking the post-back. The predicate will determine whether the General Service radio button is selected. That requires knowing whether the Service Type control validated successfully; it could be invalid if the user has not yet selected either radio button. Declare a new boolean right above the `formItemList.AddItem` call for the Service Type:

```C#
var serviceTypeInvalid = false;
```

Now add a third argument to `RadioListSetup.Create`:

```C#
validationErrorNotifier: () => serviceTypeInvalid = true
```

With our new boolean flag, we can now write our validation predicate. Wrap the `addServiceDetailFormItems` call in the following:

```C#
FormState.ExecuteWithValidationPredicate( () => !serviceTypeInvalid && mod.ServiceTypeId == ServiceTypesRows.GeneralService, () => … )
```

Finally, let’s clear the budget when the service type is not General Service. This requires exposing the `budgetClearer` from `addServiceDetailFormItems`. Add a fourth parameter to the method:

```C#
out Action dataClearer
```

And set it with this line at the end of the method:

```C#
dataClearer = budgetClearer;
```

Now, in `getContent`, add this line just below the `serviceTypeInvalid` declaration:

```C#
Action serviceDetailClearer = null;
```

And use it as the fourth argument to `addServiceDetailFormItems`:

```C#
out serviceDetailClearer
```

And finally add an `additionalValidationMethod` argument to the `mod.GetServiceTypeIdRadioListFormItem` call to invoke the `serviceDetailClearer` when the service type is not General Service:

```C#
additionalValidationMethod: validator => {
	if( mod.ServiceTypeId != ServiceTypesRows.GeneralService )
		serviceDetailClearer();
}
```

Let’s make another page modification that changes the color of the form when it’s a flat tire repair, to make it clear to users that this service order is a quick job.

We’ll use CSS to do this, and our first step is to add a style sheet. Click the `Website` project in the Solution Explorer and then click the Show All Files toolbar button. A `StaticFiles` folder will appear. Add a style sheet to the folder, naming it `styles`, and paste this into it:

```CSS
.flatTire {
	background-color: lightyellow;
}
```

Run `Update-DependentLogic` to generate some code for our new style sheet. Open `Providers/StandardPageLogic.cs` and override `GetStyleSheets`:

```C#
protected override List<ResourceInfo> GetStyleSheets() => new List<ResourceInfo> { new StaticFiles.StylesCss() };
```

This will include the style sheet on all pages in the application. Now we’ll conditionally add the `flatTire` class to the `FormItemList`. Paste in the following as the first argument to `FormItemList.CreateStack`:

```C#
generalSetup: new FormItemListSetup(
	classes: serviceTypeIdPmv.ToCondition( ( (int?)ServiceTypesRows.FlatTireRepair ).ToCollection() )
		.ToElementClassSet( ElementClasses.FlatTire ) )
```

Now you’ll see the form turn yellow if you select Flat Tire Repair.


## Modifying a page on the server side

The instant, client-side page modifications above require the server to replicate the changes when it rebuilds the page after a post-back. This pattern works well for simple modifications, such as toggling element display or adding and removing CSS classes. But it’s impractical for larger changes. Imagine adding new form controls or making complex changes to the DOM. Implementing this logic for both server and client and getting it right would be tedious and time consuming.

We’ve created a solution to this, and it involves a couple of new concepts. First is the **intermediate post-back**. This is a post-back that changes parts of the page, but does not fully process the form. It keeps the user on the page and leaves the form values in unchanged regions intact so that a future *full* post-back can properly persist them. Intermediate post-backs can be performed asynchronously (coming soon), enabling the user to continue working while parts of the page update in the background.

An intermediate post-back must declare the page regions that it will change. This is essential for usability: since any post-back requires a round trip to the server that could take one second or more, we prevent the user from interacting with the updating parts of the page until the updates are received and applied. Also, since the framework knows which regions are intentionally being changed, it can detect accidental changes outside those regions that would otherwise result in lost form values for the user.

The other concept we need here is **component state**. In order to change a page, an intermediate post-back needs to modify some data that causes the page to be different when it is rebuilt. In many cases it is not feasible to use the database or other persistent storage since you don’t yet have a completed form. Furthermore, the data may represent some type of transient state that won’t ever be persisted.

You can use component state to store data across intermediate post-backs. Under the hood it’s kept in a hidden field. A piece of component state is also an invisible component and must be placed in the tree of components on the page. This lets state that is associated with a changing region of the page be automatically disregarded on an intermediate post-back.

Let’s illustrate these concepts by implementing an expandable list of service requests on the service-order form. We’ll put this just above the budget control, and have it similarly hidden when General Service is not selected.

Add these three lines at the top of the `addServiceDetailFormItems` method:

```C#
var requests = ServiceOrderId.HasValue
					? ServiceOrderRequestsTableRetrieval.GetRows( new ServiceOrderRequestsTableEqualityConditions.ServiceOrderId( ServiceOrderId.Value ) )
					: Enumerable.Empty<ServiceOrderRequestsTableRetrieval.Row>();
var requestLineCount = ComponentStateItem.Create( "requestLineCount", Math.Max( requests.Count(), 1 ), v => v > 0, false );
var addRequestUpdateRegion = new UpdateRegionSet();
```

The first line retrieves existing requests from the database, if we’re updating a service order. The second creates a piece of component state. And the third creates an `UpdateRegionSet` object that will identify the region of the page that is changing when we add a new request line.

Let’s look at the arguments to `ComponentStateItem.Create`. The first is a unique identifier, which is required because, at this point in page construction, that’s the only way the framework can distinguish state items from each other and immediately provide you with the current value of this item. The second argument is the default value of the state item. The third argument is a predicate method that ensures that the item’s current value is in the expected range. Since component state is stored on the client side between requests, unexpected incoming values are always possible, just as with form controls. The final argument, `false`, specifies that changes to the value of the state item will not be persisted and will never affect what is persisted by the current post-back(s).

Our next step is adding this as the new second parameter to `addServiceDetailFormItems`:

```C#
List<ServiceOrderRequestsModification> requestInserts
```

We’ll use this list to store modification objects for the service requests that we need to insert into the database.

We can now add a form item for our expandable list of requests. Put this line directly beneath the three lines we just added above:

```C#
formItemList.AddItem(
	new StackList(
			Enumerable.Range( 0, requestLineCount.Value.Value )
				.Select(
					i => {
						var insert = ServiceOrderRequestsModification.CreateForInsert();
						requestInserts.Add( insert );
						return insert.GetRequestDescriptionTextControlFormItem( i != 0, value: requests.ElementAtOrDefault( i )?.RequestDescription ?? "" )
							.ToListItem();
					} )
				.Materialize(),
			setup: new ComponentListSetup(
				tailUpdateRegions: new TailUpdateRegion( addRequestUpdateRegion.ToCollection(), 0 ).ToCollection(),
				etherealContent: requestLineCount.ToCollection() ) ).Append<FlowComponent>(
			new EwfButton(
				new StandardButtonStyle( "Add another request" ),
				behavior: new PostBackBehavior(
					postBack: PostBack.CreateIntermediate(
						addRequestUpdateRegion.ToCollection(),
						id: "addRequest",
						modificationMethod: () => requestLineCount.Value.Value += 1 ) ) ) )
		.Materialize()
		.ToFormItem( setup: new FormItemSetup( displaySetup: displaySetup ), label: "Service requests".ToComponents() ) );
```

Let’s break this down. The first half is the creation of a `StackList` that contains N items, where N equals the current value of our `requestLineCount` state item. For each list item, we create a service-request modification object, add it to the `requestInserts` parameter, and create a text control in which the user can type the request. The first argument of `GetRequestDescriptionTextControlFormItem` specifies that it will only require a value from the user if it’s the first line. The second argument prefills the text control with the existing request text from that line if there is any.

The `ComponentListSetup` object passes two important things to the `StackList`. First, a tail update region, which declares that any new items added to the list on an intermediate post-back request will be part of `addRequestUpdateRegion`. Second, the `requestLineCount` state item. This places the state item logically within the list. If the list itself were ever included within an update region of another intermediate post-back, the state item’s value would be silently forgotten in just the same way as form values within the list.

The second half of the code block is the creation of the `EwfButton` that lets the user add new request lines. When clicked, the button will trigger the intermediate post-back specified by `PostBack.CreateIntermediate`. The first argument of this connects the post-back to our update region. The second is a unique ID, which every post-back needs; we didn’t see this with our first post-back earlier in the guide because we just relied on the default value. The modification method of the post-back increments the component state value, which gives the `StackList` one more item when it is rebuilt.

Let’s turn our attention to `getContent` and finish our implementation. Create a list of service-request modification objects just below the declaration of `mod` at the top of the method:

```C#
var serviceRequestInserts = new List<ServiceOrderRequestsModification>();
```

Pass this to `addServiceDetailFormItems` as the second argument. Now our only remaining task is to add logic that appropriately modifies service requests when the form is submitted. Add this block to the modification method of `PostBack.CreateFull`, after `mod.Execute`:

```C#
if( ServiceOrderId.HasValue )
	ServiceOrderRequestsModification.DeleteRows( new ServiceOrderRequestsTableEqualityConditions.ServiceOrderId( ServiceOrderId.Value ) );
foreach( var i in serviceRequestInserts.Where( i => i.RequestDescriptionHasChanged && i.RequestDescription.Any() ) ) {
	i.ServiceOrderRequestId = MainSequence.GetNextValue();
	i.ServiceOrderId = mod.ServiceOrderId;
	i.Execute();
}
```

Here we’re using a “delete and re-insert” pattern: The `if` statement unconditionally deletes all requests (if we’re updating an existing service order) and the `foreach` loop inserts new ones. The `Where` predicate is important. `RequestDescriptionHasChanged` will be false for all modification objects in the list if validation did not take place, i.e. when the General Service radio button is not selected. And `RequestDescription.Any` will be false for any empty line, preventing those from being persisted.

Take a look at the form. It’s getting quite complex and maybe even a bit realistic!


## Using component state with an underlying durable value

Coming soon. We’ll make a display of other service orders from the same customer that updates when the email address is entered or changed.


## Managing keyboard focus

Let’s improve keyboard access a bit. First we’ll put the cursor on the first form control when a user lands on the form. In `getContent`, replace the `content.Add` statement near the end of the method (the one that adds the `FormItemList` to the page) with this:

```C#
content.Add( new FlowAutofocusRegion( AutofocusCondition.InitialRequest(), formItemList.ToCollection() ) );
```

We’ve wrapped `formItemList` in a `FlowAutofocusRegion`. If you visit the form you’ll see that the Customer Name control automatically gets keyboard focus. That’s because it’s the first control in the autofocus region. This region-based design is fully intentional, to improve maintainability: if you later remove the Customer Name control, or add another control above it, the focus will adjust automatically to whatever the new first control is. If you do ever want to designate one specific control for autofocus, you can by just wrapping that one control in an autofocus region.

`AutofocusCondition.InitialRequest` specifies that the region will only be active on initial requests for the page, and not after post-backs. Let’s now make another region to show why you might want different autofocus behavior after a post-back. We’re going to modify the expandable service-request list we created earlier. In `addServiceDetailFormItems`, add this line above the first `formItemList.AddItem` call:

```C#
const string addRequestFocusKey = "addRequest";
```

Now expand the `PostBack.CreateIntermediate` call, which is a bit further down in the method, with this new argument:

```C#
reloadBehaviorGetter: () => new PageReloadBehavior( focusKey: addRequestFocusKey )
```

And finally, find the `new ComponentListSetup` call (a little bit earlier in the method) and add this as the first argument:

```C#
lastItemAutofocusCondition: AutofocusCondition.PostBack( addRequestFocusKey )
```

What we’re doing here is using a **focus key** in the intermediate post-back so that when the list of service-request lines is rebuilt after the post-back, the last line (i.e. the one that was just added) is wrapped in an active autofocus region. Having a focus key is essential because we want the region to only be active for *this* post-back. If we later add other post-backs to the page that concern different parts of the form, we don’t want to give keyboard focus to a service-request line.


## Securing pages

Before we can say our service-order management system is complete, we need to secure it against unauthorized access. First jump over to the [user management guide](UserManagement.md) and follow it all the way through. When you’re back, let’s add one additional role to our database by appending these lines to `Database Updates.sql`:

```SQL
insert into UserRoles values( 3, 'Bicycle mechanic' )
go
```

Run `Update-DependentLogic` to execute that and regenerate code. Now let’s prevent anonymous users from visiting the list of service orders. In `Home.cs` add this property above `getChildUrlPatterns`:

```C#
protected override bool userCanAccessResource => AppTools.User != null;
```

The `AppTools.User` property gives us the currently-authenticated user, or null if authentication hasn’t taken place. Therefore this expression will allow access to the page only if the user has logged in. Try it out by visiting the site. You should see a page titled Select User, which is a special log in page provided by the framework when you are running the system locally for development. It saves you from having to enter a password. If you were visiting a live installation of the system you’d see a real log in page.

Type in whatever email address you used for the first user and you should land on the Service Orders page. Notice the new Logged In As section in the upper-right corner of the page.

Let’s now restrict the creation and updating of service orders such that this can only be done by mechanics and administrators. Open `ServiceOrder.cs` and add this property override above `getContent`:

```C#
protected override bool userCanAccessResource => new[] { UserRolesRows.BicycleMechanic, UserRolesRows.Administrator }.Contains( AppTools.User.Role.RoleId );
```

Notice that we’re not handling a null `AppTools.User`. That’s because, if you recall, this page uses the service-order list as its parent. A child page inherits the parent’s authorization logic and can never be less restrictive. The child’s `userCanAccessResource` property can only add additional restrictions. To access the child, the user must effectively pass `parentConditions && childConditions`. This is recursive all the way up the tree of pages.

There’s one more thing we need to do. Go back to `Home.cs`. Add this as the first statement in `getContent`:

```C#
var newOrderPage = new ServiceOrder( null );
```

Then replace the argument of the first `UiPageContent.Add` call with the following:

```C#
newOrderPage.UserCanAccessResource
	? new EwfHyperlink( newOrderPage, new ButtonHyperlinkStyle( "New service order", buttonSize: ButtonSize.Large ) ).ToCollection()
	: Enumerable.Empty<FlowComponent>().Materialize()
```

And replace the second argument of the `AddData` call for the table with:

```C#
i => {
	var page = new ServiceOrder( i.ServiceOrderId );
	return EwfTableItem.Create(
		i.ServiceOrderId.ToString()
			.ToCell()
			.Append( i.CustomerName.ToCell() )
			.Append( i.BicycleDescription.ToCell() )
			.Append( ServiceTypesTableRetrieval.GetRowMatchingId( i.ServiceTypeId ).ServiceTypeName.ToCell() )
			.Materialize(),
		setup: EwfTableItemSetup.Create(
			activationBehavior: page.UserCanAccessResource ? ElementActivationBehavior.CreateHyperlink( page ) : null ) );
}
```

Together these two changes prevent any linking to the form page if the user does not have access. If you skip this step you’ll receive exceptions when visiting the list page. The framework does this intentionally to prevent you from creating links to unauthorized pages, to spare the user from ever receiving “access denied” errors when navigating.

Let’s see what this looks like for normal users. Visit the home page. Since you’re an admin you’ll still see the links. We now need to create and switch into a normal user, so go to the address bar and add `/ewl` to the end of the URL.

This is the framework’s built-in admin area. One thing you can do here is view and modify the users of the system. Click the User Management tab and then the Create User button on the right. Type an email address, e.g. `standard-user@example.com`, and select the Standard User role. Click OK. Now click the End Impersonation button in the yellow bar at the top of the page.

This time, on the Select User page, enter the email address you used for the standard user. When you click Begin Impersonation, you should now see the service-order list without the New Service Order button, and with no option to click any of the existing service orders.


## Learning more

If you’ve made it this far, congratulations. You’ve developed a piece of enterprise software that adheres to best practices and is of sufficient quality that it could be used in production by a real business. At the moment this is the end of our Guide series but if you’d like to learn more please reach out to us in our [Community Forum](https://community.enterpriseweblibrary.org/).