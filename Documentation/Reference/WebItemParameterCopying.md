# Web-item parameter copying

When a new resource or entity setup object is created during a web request, it will attempt to initialize its unspecified optional parameters with values from a matching object in the current context. Note that this is not done for logical parents unless they are also URL parents because otherwise the state in question may not be represented in the URL and therefore could change after a user navigates with a hyperlink. Here are the desired behaviors in some common creation scenarios:

* Making a hyperlink: Resource and ancestors get existing parameter values from current page’s logical ancestors.

* Post-back re-creation of destination resource: Resource and ancestors get new parameter values from current page’s logical ancestors.

* Post-back re-creation of current page: Page and ancestors get existing parameter values from current page’s URL ancestors and new parameter values from current page’s logical ancestors.

* Post-back re-creation of nested URL web item: Item and ancestors get existing parameter values from nested item’s URL ancestors and new parameter values from nested item’s logical ancestors, if they exist.

* Post-back re-creation of destination resource from a nested URL: Resource and ancestors get existing parameter values from nested resource’s URL ancestors and new parameter values from nested resource’s logical ancestors, if they exist.

* Post-back re-creation of newly-created nested URL web item: Item and ancestors get new parameter values from current page’s logical ancestors.