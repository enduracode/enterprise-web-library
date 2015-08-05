# Making an EWF-UI web site

Add a `Providers` folder to the web project. Inside, create an `EwfUiProvider` class that looks like this:

```C#
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;

namespace Fusion.DigitalNow.WebSite.Providers {
	internal class EwfUiProvider: AppEwfUiProvider {}
}
```