using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// Helpful System.Reflection.Assembly methods.
	/// </summary>
	public static class AssemblyTools {
		/// <summary>
		/// Creates an instance of each type in the assembly that implements the specified interface.
		/// </summary>
		public static IEnumerable<InterfaceType> CreateInstancesOfImplementations<InterfaceType>( this Assembly assembly ) {
			return assembly.getImplementations<InterfaceType>().Select( i => (InterfaceType)Activator.CreateInstance( i ) );
		}

		/// <summary>
		/// Builds a dictionary containing all singletons in the specified assembly that implement the specified interface type. All singletons must implement an
		/// Instance property.
		/// </summary>
		public static Dictionary<TKey, TInterface> BuildSingletonDictionary<TInterface, TKey>( Assembly assembly, Func<TInterface, TKey> getIdMethod ) {
			var singletons = new Dictionary<TKey, TInterface>();
			foreach( var type in assembly.getImplementations<TInterface>() ) {
				var instanceProperty = type.GetProperty( "Instance" );
				var singleton = (TInterface)instanceProperty.GetValue( null, null );
				singletons.Add( getIdMethod( singleton ), singleton );
			}
			return singletons;
		}

		private static IEnumerable<Type> getImplementations<InterfaceType>( this Assembly assembly ) {
			return assembly.GetTypes().Where( i => typeof( InterfaceType ).IsAssignableFrom( i ) && !i.IsInterface );
		}
	}
}