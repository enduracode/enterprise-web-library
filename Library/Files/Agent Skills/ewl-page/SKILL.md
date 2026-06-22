---
name: ewl-page
description: Create and configure EWL pages with parameters, URL routing, parent hierarchy, and access control
---

## Creating a page

Every page is a `partial class` with a `// EwlPage` comment directive above
the class declaration. The Development Utility generates the other partial.

```csharp
// EwlPage
namespace MySystem.Website;

partial class Home {
	protected override string getResourceName() => "";

	protected override IEnumerable<UrlPattern> getChildUrlPatterns() =>
		RequestDispatchingStatics.GetFrameworkUrlPatterns( WebApplicationNames.Website );

	protected override PageContent getContent() =>
		new UiPageContent().Add( new Paragraph( "Hello!".ToComponents() ) );
}
```

Use `// EwlResource` instead of `// EwlPage` for non-page resources such as
file downloads, CSS, or robots.txt.

Run `sync` after adding or changing any page class.

## Page parameters

Declare parameters via comments above the class. The DU generates constructor
parameters, URL encoding/decoding, and typed `UrlPatterns` methods.

```csharp
// EwlPage
// Parameter: int serviceOrderId
```

Optional parameters use `// OptionalParameter:` and can have defaults:

```csharp
// EwlPage
// Parameter: int organizationId
// OptionalParameter: int? roomId
// OptionalParameter: int calendarViewId
```

Provide defaults via the generated `specifyParameterDefaults` partial method:

```csharp
static partial void specifyParameterDefaults( OptionalParameterSpecifier specifier, Parameters parameters ) {
	specifier.CalendarViewId = CalendarViewsRows.MonthView;
}
```

## Parent pages and URL routing

Every page (except the root) must specify a parent:

```csharp
protected override ResourceParent createParent() => new Home();
```

Parent pages declare child URL patterns in `getChildUrlPatterns`:

```csharp
protected override IEnumerable<UrlPattern> getChildUrlPatterns() =>
	RequestDispatchingStatics.GetFrameworkUrlPatterns( WebApplicationNames.Website )
		.Append( ServiceOrder.UrlPatterns.ServiceOrderIdPositiveInt( "create" ) );
```

This creates child URLs like `/123` for each existing ID and `/create` for
creating new records.

## Entity setups

Entity setups group related pages under a shared parent with navigation tabs:

```csharp
// EwlPage
// Parameter: int organizationId

partial class EntitySetup {
	protected override ResourceParent createParent() => new Home();

	protected override IEnumerable<ResourceGroup> createListedResources() =>
		new ResourceGroup( new Details( this ), new Rooms( this ), new Users( this ) ).ToCollection();
}
```

Child pages take the entity setup as a constructor parameter and use it as
their parent.

## Page content

`UiPageContent` provides the standard EWL UI chrome (sidebar, content area).
Use `BasicPageContent` for pages without the EWL UI shell. The `Add` method
is chainable:

```csharp
protected override PageContent getContent() =>
	new UiPageContent()
		.Add( formItemList )
		.Add( table );
```

## Securing pages

Override `userCanAccess` to restrict access:

```csharp
protected override bool userCanAccess => SystemUser.Current is not null;
```

Authorization is hierarchical: child pages inherit the parent's restrictions
and can only add additional ones. A child's `userCanAccess` never loosens the
parent's. To access a child, the user must pass
`parentConditions && childConditions`.

When linking to a page, always check `UserCanAccess` to avoid linking to
pages the user cannot reach:

```csharp
var page = new ServiceOrder( id );
if( page.UserCanAccess )
	// create hyperlink
```

The framework throws an exception if you create a link to an unauthorized
page, to prevent users from ever seeing "access denied" errors.

## Request dispatching

Each web application must have a `RequestDispatching` provider:

```csharp
partial class RequestDispatching {
	protected override IEnumerable<BaseUrlPattern> GetBaseUrlPatterns() =>
		Home.UrlPatterns.BaseUrlPattern().ToCollection();

	public override UrlHandler GetFrameworkUrlParent() => new Home();
}
```
