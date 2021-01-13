using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class PageNode {
		private static readonly IReadOnlyCollection<PageNode> noChildren = Enumerable.Empty<PageNode>().Materialize();

		public readonly PageComponent SourceComponent;
		public readonly FormValue FormValue;
		public readonly ComponentStateItem StateItem;
		public readonly IReadOnlyCollection<( string key, UpdateRegionLinker linker )> KeyedUpdateRegionLinkers;
		public readonly AutofocusCondition AutofocusCondition;
		public IReadOnlyCollection<PageNode> Children;
		public readonly IReadOnlyCollection<PageNode> EtherealChildren;
		public readonly Func<FocusabilityCondition> FocusabilityConditionGetter;
		public readonly Action<bool, TextWriter> JsInitStatementWriter;
		public readonly Action<TextWriter> MarkupWriter;

		public PageNode(
			ElementNode elementNode, FormValue formValue, IReadOnlyCollection<PageNode> children, IReadOnlyCollection<PageNode> etherealChildren,
			Func<FocusabilityCondition> focusabilityConditionGetter, Action<bool, TextWriter> jsInitStatementWriter,
			Func<( string name, IEnumerable<Tuple<string, string>> attributes )> tagGetter ) {
			SourceComponent = elementNode;
			FormValue = formValue;
			Children = children;
			EtherealChildren = etherealChildren;
			FocusabilityConditionGetter = focusabilityConditionGetter;
			JsInitStatementWriter = jsInitStatementWriter;

			MarkupWriter = writer => {
				var tag = tagGetter();

				writer.Write( '<' );
				writer.Write( tag.name );
				foreach( var i in tag.attributes ) {
					writer.Write( ' ' );
					writer.Write( i.Item1 );
					writer.Write( '=' );
					writer.Write( '"' );
					writer.Write( HttpUtility.HtmlAttributeEncode( i.Item2 ) );
					writer.Write( '"' );
				}
				writer.Write( '>' );

				if( isVoidElement( tag.name ) )
					return;

				foreach( var i in Children )
					i.MarkupWriter( writer );

				writer.Write( '<' );
				writer.Write( '/' );
				writer.Write( tag.name );
				writer.Write( '>' );
			};
		}

		// https://html.spec.whatwg.org/multipage/syntax.html#void-elements
		private bool isVoidElement( string t ) =>
			t == "area" || t == "base" || t == "br" || t == "col" || t == "embed" || t == "hr" || t == "img" || t == "input" || t == "link" || t == "meta" ||
			t == "param" || t == "source" || t == "track" || t == "wbr";

		public PageNode( TextNode textNode ) {
			SourceComponent = textNode;
			Children = noChildren;
			EtherealChildren = noChildren;
			MarkupWriter = writer => {
				var text = textNode.TextGetter();
				if( text.Length > 0 )
					writer.Write( HttpUtility.HtmlEncode( text ) );
			};
		}

		public PageNode( MarkupBlockNode markupBlockNode ) {
			SourceComponent = markupBlockNode;
			Children = noChildren;
			EtherealChildren = noChildren;
			MarkupWriter = writer => {
				var markup = markupBlockNode.MarkupGetter();
				if( markup.Length > 0 )
					writer.Write( markup );
			};
		}

		public PageNode( FlowComponent flowComponent, IReadOnlyCollection<PageNode> children ) {
			SourceComponent = flowComponent;
			AutofocusCondition = ( flowComponent as FlowAutofocusRegion )?.Condition;
			Children = children;
			EtherealChildren = noChildren;
			MarkupWriter = writer => {
				foreach( var i in children )
					i.MarkupWriter( writer );
			};
		}

		public PageNode( EtherealComponent etherealComponent, IReadOnlyCollection<PageNode> children ) {
			SourceComponent = etherealComponent;
			StateItem = etherealComponent as ComponentStateItem;
			AutofocusCondition = ( etherealComponent as EtherealAutofocusRegion )?.Condition;
			Children = children;
			EtherealChildren = noChildren;
			MarkupWriter = writer => {
				foreach( var i in children )
					i.MarkupWriter( writer );
			};
		}

		public PageNode(
			PageComponent sourceComponent, string id, IReadOnlyCollection<UpdateRegionLinker> updateRegionLinkers, IReadOnlyCollection<PageNode> children = null ) {
			SourceComponent = sourceComponent;
			KeyedUpdateRegionLinkers = updateRegionLinkers.Select( i => ( id + i.KeySuffix, i ) ).Materialize();
			Children = children;
			EtherealChildren = noChildren;
			MarkupWriter = writer => {
				foreach( var i in children )
					i.MarkupWriter( writer );
			};
		}

		public IEnumerable<PageNode> AllChildren => Children.Concat( EtherealChildren );
	}
}