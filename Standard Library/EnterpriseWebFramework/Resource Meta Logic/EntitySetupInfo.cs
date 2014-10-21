using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A base set of functionality that can be used to discover information about an entity setup before requesting a resource that uses it.
	/// </summary>
	public abstract class EntitySetupInfo {
		private bool parentResourceLoaded;
		private ResourceInfo parentResource;
		private ReadOnlyCollection<ResourceGroup> resources;
		private readonly Lazy<AlternativeResourceMode> alternativeMode;

		/// <summary>
		/// Creates an entity setup info object.
		/// </summary>
		protected EntitySetupInfo() {
			alternativeMode = new Lazy<AlternativeResourceMode>( createAlternativeMode );
		}

		/// <summary>
		/// Throws an exception if the parameter values or any non URL elements of the current request make the entity setup invalid.
		/// </summary>
		protected virtual void init() {}

		/// <summary>
		/// Creates a resource info object for the parent resource of this entity setup. Returns null if there is no parent resource.
		/// </summary>
		protected abstract ResourceInfo createParentResourceInfo();

		/// <summary>
		/// Creates a list of resource groups for the resources that are part of this entity setup.
		/// </summary>
		protected abstract List<ResourceGroup> createResourceInfos();

		/// <summary>
		/// Gets the resource info object for the parent resource of this entity setup. Returns null if there is no parent resource.
		/// </summary>
		public ResourceInfo ParentResource {
			get {
				if( parentResourceLoaded )
					return parentResource;
				parentResourceLoaded = true;
				return parentResource = createParentResourceInfo();
			}
		}

		/// <summary>
		/// Gets the list of resource info objects for the resources that are part of this entity setup.
		/// </summary>
		public ReadOnlyCollection<ResourceGroup> Resources { get { return resources ?? ( resources = createResourceInfos().AsReadOnly() ); } }

		/// <summary>
		/// Returns the name of the entity setup.
		/// </summary>
		public abstract string EntitySetupName { get; }

		/// <summary>
		/// Returns true if the authenticated user passes entity setup authorization checks.
		/// </summary>
		protected internal virtual bool UserCanAccessEntitySetup { get { return true; } }

		/// <summary>
		/// Gets the log in page to use for resources that are part of this entity setup if the system supports forms authentication.
		/// </summary>
		protected internal virtual PageInfo LogInPage { get { return ParentResource != null ? ParentResource.LogInPage : null; } }

		/// <summary>
		/// Gets the alternative mode for this entity setup or null if it is in normal mode. Do not call this from the createAlternativeMode method of an ancestor;
		/// doing so will result in a stack overflow.
		/// </summary>
		public AlternativeResourceMode AlternativeMode {
			get {
				// It's important to do the parent disabled check first so the entity setup doesn't have to repeat any of it in its disabled check.
				if( ParentResource != null && ParentResource.AlternativeMode is DisabledResourceMode )
					return ParentResource.AlternativeMode;
				return AlternativeModeDirect;
			}
		}

		/// <summary>
		/// Gets the alternative mode for this entity setup without using ancestor logic. Useful when called from the createAlternativeMode method of an ancestor,
		/// e.g. when implementing a parent that should have new content when one or more children have new content. When calling this property take care to meet
		/// any preconditions that would normally be handled by ancestor logic.
		/// </summary>
		public AlternativeResourceMode AlternativeModeDirect { get { return alternativeMode.Value; } }

		/// <summary>
		/// Creates the alternative mode for this entity setup or returns null if it is in normal mode.
		/// </summary>
		protected virtual AlternativeResourceMode createAlternativeMode() {
			return null;
		}

		/// <summary>
		/// Gets the desired security setting for requests to resources that are part of this entity setup.
		/// </summary>
		protected internal virtual ConnectionSecurity ConnectionSecurity {
			get { return ParentResource != null ? ParentResource.ConnectionSecurity : ConnectionSecurity.SecureIfPossible; }
		}
	}
}