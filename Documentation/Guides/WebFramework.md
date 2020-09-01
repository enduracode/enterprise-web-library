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


## Adding the pages

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


## Page information classes

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


## Creating the form




## Security (section needs work)

*	The page usees the entity setup’s security that’s in the same folder. It does not matter if the entity setup has the page as a tab. If it’s in the same folder it affects it.
*	You can’t be less-restrictive than your parent’s security.
*	The security is checked for the whole tree. The page and its parent and its parent and its parent...
*	The folder structure does not matter other than the entity setup being in the same folder as the page


## Horizontal/vertical tabs (section needs work; also move elsewhere since uncommon)

Have your page’s `Info` class implement the `TabModeOverrider` interface. Then implement the only method in that interface, `GetTabMode`, and return `TabMode.Horizontal`.