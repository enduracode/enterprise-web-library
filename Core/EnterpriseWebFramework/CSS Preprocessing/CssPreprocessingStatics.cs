using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Provides access to custom elements.
	/// </summary>
	internal static class CssPreprocessingStatics {
		private static readonly List<CssElement> elements = new List<CssElement>();

		/// <summary>
		/// Loads custom elements from the standard library assembly and any additional assemblies passed.
		/// </summary>
		internal static void Init( params Assembly[] additionalAssemblies ) {
			var assemblies = new[] { Assembly.GetExecutingAssembly() }.Concat( additionalAssemblies );
			elements.AddRange(
				assemblies.SelectMany( i => i.CreateInstancesOfImplementations<ControlCssElementCreator>().SelectMany( creator => creator.CreateCssElements() ) ) );
			var duplicateElementNames = elements.Select( i => i.Name ).GetDuplicates().ToArray();
			if( duplicateElementNames.Any() )
				throw new ApplicationException( "Duplicate elements exist: " + StringTools.ConcatenateWithDelimiter( ", ", duplicateElementNames ) + "." );
		}

		internal static ReadOnlyCollection<CssElement> Elements { get { return elements.AsReadOnly(); } }
	}
}