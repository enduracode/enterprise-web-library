using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class ElementClassSet {
		public static readonly ElementClassSet Empty = new ElementClassSet( ImmutableDictionary<string, Tuple<Action<Func<string, string>>, Func<bool>>>.Empty );

		public static implicit operator ElementClassSet( ElementClass elementClass ) {
			return new ElementClassSet( ( (Tuple<Action<Func<string, string>>, Func<bool>>)null ).ToCollection().ToImmutableDictionary( i => elementClass.ClassName ) );
		}

		internal readonly IImmutableDictionary<string, Tuple<Action<Func<string, string>>, Func<bool>>>
			JsModificationStatementAdderAndInclusionPredicatePairsByClassName;

		internal ElementClassSet(
			IImmutableDictionary<string, Tuple<Action<Func<string, string>>, Func<bool>>> jsModificationStatementAdderAndInclusionPredicatePairsByClassName ) {
			JsModificationStatementAdderAndInclusionPredicatePairsByClassName = jsModificationStatementAdderAndInclusionPredicatePairsByClassName;
		}

		/// <summary>
		/// Adds element classes to this set.
		/// </summary>
		public ElementClassSet Add( ElementClassSet classSet ) {
			if(
				JsModificationStatementAdderAndInclusionPredicatePairsByClassName.Keys.Intersect(
					classSet.JsModificationStatementAdderAndInclusionPredicatePairsByClassName.Keys ).Any() )
				throw new ApplicationException( "At least one class exists in both sets." );
			return
				new ElementClassSet(
					JsModificationStatementAdderAndInclusionPredicatePairsByClassName.AddRange( classSet.JsModificationStatementAdderAndInclusionPredicatePairsByClassName ) );
		}

		/// <summary>
		/// Gets whether this set uses the element IDs that are added.
		/// </summary>
		internal bool UsesElementIds => JsModificationStatementAdderAndInclusionPredicatePairsByClassName.Values.Any( i => i != null );

		/// <summary>
		/// Adds an element ID to this set, which enables JavaScript to change the classes on the element.
		/// </summary>
		internal void AddElementId( string id ) {
			foreach( var keyValuePair in JsModificationStatementAdderAndInclusionPredicatePairsByClassName.Where( i => i.Value != null ) ) {
				keyValuePair.Value.Item1(
					classIncludedExpression =>
					"if( {0} ) {1} else {2}".FormatWith(
						classIncludedExpression,
						"{ " + "$( '#{0}' ).addClass( '{1}' );".FormatWith( id, keyValuePair.Key ) + " }",
						"{ " + "$( '#{0}' ).removeClass( '{1}' );".FormatWith( id, keyValuePair.Key ) + " }" ) );
			}
		}

		/// <summary>
		/// Not available until after the page tree has been built.
		/// </summary>
		internal IEnumerable<string> GetClassNames() {
			EwfPage.AssertPageTreeBuilt();
			return from i in JsModificationStatementAdderAndInclusionPredicatePairsByClassName where i.Value == null || i.Value.Item2() select i.Key;
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
		/// Creates an element-class set that depends on this page-modification value.
		/// </summary>
		public static ElementClassSet ToElementClassSet(
			this PageModificationValue<bool> pageModificationValue, ElementClassSet staticClassSet, bool classesPresentWhenValueSet = true ) {
			if( staticClassSet.JsModificationStatementAdderAndInclusionPredicatePairsByClassName.Values.Any( i => i != null ) )
				throw new ApplicationException( "At least one class already has dynamic behavior." );
			return
				new ElementClassSet(
					staticClassSet.JsModificationStatementAdderAndInclusionPredicatePairsByClassName.Keys.ToImmutableDictionary(
						i => i,
						i =>
						Tuple.Create<Action<Func<string, string>>, Func<bool>>(
							statementGetter =>
							pageModificationValue.AddJsModificationStatement(
								valueExpression => statementGetter( classesPresentWhenValueSet ? valueExpression : "!( {0} )".FormatWith( valueExpression ) ) ),
							() => pageModificationValue.Value ^ !classesPresentWhenValueSet ) ) );
		}
	}
}