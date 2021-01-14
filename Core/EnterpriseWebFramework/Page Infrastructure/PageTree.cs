using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class PageTree {
		private class IdGenerator {
			private readonly string idBase;
			private int number;

			public IdGenerator( string idBase ) {
				this.idBase = idBase;
				number = 1;
			}

			public IdGenerator():
				// Prefix generated IDs with double underscore to avoid collisions with specified client-side IDs on the page.
				this( "_" ) {}

			public string GenerateId() => idBase + "_" + number++;

			public string GenerateLocallySpecifiedId( string localId ) =>
				// Prefix specified local IDs with double underscore to avoid collisions with numbered IDs.
				idBase + "__" + localId;
		}

		private readonly PageNode rootNode;
		public readonly List<PageNode> AllNodes;
		private PageNode etherealContainerNode;

		private readonly Action<string> idSetter;
		private readonly Func<string, ErrorSourceSet, ImmutableDictionary<EwfValidation, IReadOnlyCollection<string>>> modificationErrorGetter;
		private readonly FlowComponent etherealContainer;
		private readonly FlowComponent jsInitElement;
		private readonly StringBuilder elementJsInitStatements;

		private int nodeCount;
		private int etherealComponentCount;

		public PageTree(
			PageComponent rootComponent, Action<string> idSetter,
			Func<string, ErrorSourceSet, ImmutableDictionary<EwfValidation, IReadOnlyCollection<string>>> modificationErrorGetter, FlowComponent etherealContainer,
			FlowComponent jsInitElement, StringBuilder elementJsInitStatements ) {
			this.idSetter = idSetter;
			this.modificationErrorGetter = modificationErrorGetter;
			this.etherealContainer = etherealContainer;
			this.jsInitElement = jsInitElement;
			this.elementJsInitStatements = elementJsInitStatements;

			rootNode = buildNode( rootComponent, new IdGenerator(), false, false );

			AllNodes = new List<PageNode>( nodeCount );
			addTreeToAllNodes( rootNode );
		}

		private PageNode buildNode( PageComponent component, IdGenerator idGenerator, bool inEtherealContainer, bool inJsInitElement ) {
			nodeCount += 1;

			IReadOnlyCollection<PageNode> buildChildren( IEnumerable<PageComponent> children, IdGenerator g ) =>
				children.Select( i => buildNode( i, g, inEtherealContainer, inJsInitElement ) ).Materialize();

			if( component is ElementNode elementNode ) {
				FormState.Current.SetForNextElement();

				var id = idGenerator.GenerateId();
				idSetter( id );

				var data = elementNode.ElementDataGetter( new ElementContext( id ) );
				ElementNodeLocalData localData = null;
				ElementNodeFocusDependentData focusDependentData = null;
				var node = new PageNode(
					elementNode,
					elementNode.FormValue,
					buildChildren( data.Children, idGenerator ),
					buildChildren( data.EtherealChildren, idGenerator ),
					() => {
						// Defer attribute creation for the JavaScript initialization element.
						if( inJsInitElement )
							return new FocusabilityCondition( false );

						localData = data.LocalDataGetter();
						return localData.FocusabilityCondition;
					},
					( isFocused, writer ) => {
						// Defer attribute creation for the JavaScript initialization element.
						if( inJsInitElement )
							return;

						focusDependentData = localData.FocusDependentDataGetter( isFocused );
						writer.Write( focusDependentData.JsInitStatements );
					},
					() => {
						if( inJsInitElement ) {
							localData = data.LocalDataGetter();
							focusDependentData = localData.FocusDependentDataGetter( false );
						}

						var attributes = focusDependentData.Attributes;
						if( focusDependentData.IncludeIdAttribute )
							attributes = attributes.Append( new ElementAttribute( "id", data.ClientSideIdOverride.Any() ? data.ClientSideIdOverride : id ) );
						return ( localData.ElementName, attributes );
					} );

				if( inEtherealContainer )
					etherealContainerNode = node;
				etherealComponentCount += node.EtherealChildren.Count;

				return node;
			}
			if( component is TextNode textNode )
				return new PageNode( textNode );
			if( component is MarkupBlockNode markupBlockNode )
				return new PageNode( markupBlockNode );

			if( component is FlowComponent flowComponent ) {
				if( component == etherealContainer )
					inEtherealContainer = true;
				if( component == jsInitElement )
					inJsInitElement = true;
				return new PageNode( flowComponent, buildChildren( flowComponent.GetChildren(), idGenerator ) );
			}
			if( component is EtherealComponent etherealComponent )
				return new PageNode( etherealComponent, buildChildren( etherealComponent.GetChildren(), idGenerator ) );

			PageNode buildIdentifiedComponentNode<ChildType>(
				IdentifiedComponentData<ChildType> data, Func<ModificationErrorDictionary, IEnumerable<PageComponent>> childGetter ) where ChildType: PageComponent {
				var id = string.IsNullOrEmpty( data.Id ) ? idGenerator.GenerateId() : idGenerator.GenerateLocallySpecifiedId( data.Id );
				idSetter( id );

				return new PageNode(
					component,
					id,
					data.UpdateRegionLinkers,
					buildChildren(
						childGetter(
							new ModificationErrorDictionary(
								modificationErrorGetter( id, data.ErrorSources ),
								data.ErrorSources.IncludeGeneralErrors
									? AppRequestState.Instance.EwfPageRequestState.GeneralModificationErrors
									: ImmutableArray<TrustedHtmlString>.Empty ) ),
						data.Id == null ? idGenerator : new IdGenerator( id ) ) );
			}
			if( component is IdentifiedFlowComponent identifiedFlowComponent ) {
				var data = identifiedFlowComponent.ComponentDataGetter();
				return buildIdentifiedComponentNode( data, data.ChildGetter );
			}
			if( component is IdentifiedEtherealComponent identifiedEtherealComponent ) {
				var data = identifiedEtherealComponent.ComponentDataGetter();
				return buildIdentifiedComponentNode( data, data.ChildGetter );
			}

			throw new UnexpectedValueException( "component", component );
		}

		private void addTreeToAllNodes( PageNode node ) {
			AllNodes.Add( node );
			foreach( var child in node.AllChildren )
				addTreeToAllNodes( child );
		}

		public IReadOnlyCollection<PageNode> GetStaticRegionNodes( IEnumerable<( PageNode node, IEnumerable<PageComponent> components )> updateRegions ) {
			if( updateRegions == null )
				return AllNodes;

			var nodes = new List<PageNode>( nodeCount );

			var updateRegionComponentsByNode = updateRegions.SelectMany( i => i.components, ( region, component ) => ( region.node, component ) )
				.ToLookup( i => i.node, i => i.component );
			void addNodes( PageNode node, ImmutableHashSet<PageComponent> updateRegionComponents ) {
				updateRegionComponents = updateRegionComponents.Union( updateRegionComponentsByNode[ node ] );
				if( updateRegionComponents.Contains( node.SourceComponent ) )
					return;

				nodes.Add( node );
				foreach( var child in node.AllChildren )
					addNodes( child, updateRegionComponents );
			}
			addNodes( rootNode, ImmutableHashSet<PageComponent>.Empty );

			return nodes;
		}

		public void PrepareForRendering( bool modificationErrorsOccurred, Func<FocusabilityCondition, bool> isFocusablePredicate ) {
			var etherealChildren = new List<PageNode>( etherealComponentCount );

			var activeAutofocusRegionsExist = false;
			var elementFocused = false;

			void prepareForRendering( PageNode node, bool inActiveAutofocusRegion, TextWriter jsInitStatementWriter ) {
				etherealChildren.AddRange( node.EtherealChildren );

				if( !inActiveAutofocusRegion && node.AutofocusCondition?.IsTrue( AppRequestState.Instance.EwfPageRequestState.FocusKey ) == true ) {
					inActiveAutofocusRegion = true;
					activeAutofocusRegionsExist = true;
				}

				var focusabilityCondition = node.FocusabilityConditionGetter?.Invoke();

				var isFocused = !elementFocused && inActiveAutofocusRegion && focusabilityCondition != null &&
				                isFocusablePredicate( node.FocusabilityConditionGetter() );
				if( isFocused )
					elementFocused = true;

				node.JsInitStatementWriter?.Invoke( isFocused, jsInitStatementWriter );

				foreach( var i in node.AllChildren )
					prepareForRendering( i, inActiveAutofocusRegion, jsInitStatementWriter );
			}

			using( var jsInitStatementWriter = new StringWriter( elementJsInitStatements ) )
				prepareForRendering( rootNode, modificationErrorsOccurred, jsInitStatementWriter );

			if( !modificationErrorsOccurred && activeAutofocusRegionsExist && !elementFocused )
				throw new ApplicationException( "The active autofocus regions do not contain any focusable elements." );

			etherealContainerNode.Children = etherealChildren;
		}

		public void WriteMarkup( TextWriter writer ) {
			writer.Write( "<!DOCTYPE html>" );
			rootNode.MarkupWriter( writer );
		}
	}
}