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


## Using temporary state during a post-back


## Modifying a page on the client side


## Modifying a page on the server side


## Using component state


## Security (section needs work)

*	The page usees the entity setup’s security that’s in the same folder. It does not matter if the entity setup has the page as a tab. If it’s in the same folder it affects it.
*	You can’t be less-restrictive than your parent’s security.
*	The security is checked for the whole tree. The page and its parent and its parent and its parent...
*	The folder structure does not matter other than the entity setup being in the same folder as the page


## Horizontal/vertical tabs (section needs work; also move elsewhere since uncommon)

Have your page’s `Info` class implement the `TabModeOverrider` interface. Then implement the only method in that interface, `GetTabMode`, and return `TabMode.Horizontal`.