using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class ElementClassSet {
		public static readonly ElementClassSet Empty = new ElementClassSet( ImmutableDictionary<string, PageModificationValueCondition>.Empty );

		public static implicit operator ElementClassSet( ElementClass elementClass ) =>
			elementClass != null
				? new ElementClassSet( ( (PageModificationValueCondition)null ).ToCollection().ToImmutableDictionary( i => elementClass.ClassName ) )
				: Empty;

		internal readonly IImmutableDictionary<string, PageModificationValueCondition> ConditionsByClassName;

		internal ElementClassSet( IImmutableDictionary<string, PageModificationValueCondition> conditionsByClassName ) {
			ConditionsByClassName = conditionsByClassName;
		}

		/// <summary>
		/// Adds element classes to this set.
		/// </summary>
		public ElementClassSet Add( ElementClassSet classSet ) {
			if( ConditionsByClassName.Keys.Intersect( classSet.ConditionsByClassName.Keys ).Any() )
				throw new ApplicationException( "At least one class exists in both sets." );
			return new ElementClassSet( ConditionsByClassName.AddRange( classSet.ConditionsByClassName ) );
		}

		/// <summary>
		/// Gets whether this set uses the element IDs that are added.
		/// </summary>
		internal bool UsesElementIds => ConditionsByClassName.Values.Any( i => i != null );

		/// <summary>
		/// Adds an element ID to this set, which enables JavaScript to change the classes on the element.
		/// </summary>
		internal void AddElementId( string id ) {
			foreach( var keyValuePair in ConditionsByClassName.Where( i => i.Value != null ) ) {
				keyValuePair.Value.AddJsModificationStatement(
					expression => "if( {0} ) {1} else {2}".FormatWith(
						expression,
						"{ " + "$( '#{0}' ).addClass( '{1}' );".FormatWith( id, keyValuePair.Key ) + " }",
						"{ " + "$( '#{0}' ).removeClass( '{1}' );".FormatWith( id, keyValuePair.Key ) + " }" ) );
			}
		}

		/// <summary>
		/// Not available until after the page tree has been built.
		/// </summary>
		internal IEnumerable<string> GetClassNames() {
			EwfPage.AssertPageTreeBuilt();
			return from i in ConditionsByClassName where i.Value == null || i.Value.IsTrue select i.Key;
		}
	}

	public static class ElementClassSetExtensionCreators {
		/// <summary>
		/// Adds element classes to this class.
		/// </summary>
		public static ElementClassSet Add( this ElementClass elementClass, ElementClassSet classSet ) {
			return ( (ElementClassSet)elementClass ).Add( classSet );
		}

		/// <summary>
		/// Creates an element-class set that depends on this page-modification-value condition.
		/// </summary>
		public static ElementClassSet ToElementClassSet( this PageModificationValueCondition pageModificationValueCondition, ElementClassSet staticClassSet ) {
			if( staticClassSet.ConditionsByClassName.Values.Any( i => i != null ) )
				throw new ApplicationException( "At least one class already has dynamic behavior." );
			return new ElementClassSet( staticClassSet.ConditionsByClassName.Keys.ToImmutableDictionary( i => i, i => pageModificationValueCondition ) );
		}
	}
}