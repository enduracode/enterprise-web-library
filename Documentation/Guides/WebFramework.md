# Web framework


## Security (section needs work)

*	The page usees the entity setup’s security that’s in the same folder. It does not matter if the entity setup has the page as a tab. If it’s in the same folder it affects it.
*	You can’t be less-restrictive than your parent’s security.
*	The security is checked for the whole tree. The page and its parent and its parent and its parent...
*	The folder structure does not matter other than the entity setup being in the same folder as the page


## Horizontal/vertical tabs (section needs work)

Have your page’s `Info` class implement the `TabModeOverrider` interface. Then implement the only method in that interface, `GetTabMode`, and return `TabMode.Horizontal`.