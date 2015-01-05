Nothing new should go into the Controls folder. Controls should go into the folder of the subsystem that they have to do with.
If they aren't part of a subsystem, they should go in the root of EnterpriseWebFramework.

All new classes in EWF should have a namespace of RedStapler.StandardLibrary.EnterpriseWebFramework, regardless of what folder they are in.

For example, we want all user management classes to be in the same folder instead of split into Controls and non-Controls.