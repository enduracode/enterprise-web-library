﻿#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Activation behavior for an element that is not a hyperlink or button. Will add rollover behavior to table element(s), unless it is used on a field of an
	/// EWF table or an item of a column primary table. Column hover behavior is not possible with CSS.
	/// </summary>
	public class ElementActivationBehavior {
		// This class name is used by EWF CSS and JavaScript files.
		internal static readonly ElementClass ActivatableClass = new ElementClass( "ewfAc" );

		internal static FlowComponent GetActivatableElement(
			string elementName, ElementClassSet classes, IReadOnlyCollection<ElementAttribute> attributes, ElementActivationBehavior activationBehavior,
			IReadOnlyCollection<FlowComponent> children, IReadOnlyCollection<EtherealComponent> etherealChildren ) =>
			new ElementComponent(
				context => {
					activationBehavior?.PostBackAdder();
					return new ElementData(
						() => new ElementLocalData(
							elementName,
							new FocusabilityCondition( activationBehavior?.IsFocusable == true ),
							isFocused => new ElementFocusDependentData(
								attributes: attributes.Concat( activationBehavior != null ? activationBehavior.AttributeGetter() : Enumerable.Empty<ElementAttribute>() )
									.Concat(
										activationBehavior?.IsFocusable == true
											? new[] { new ElementAttribute( "tabindex", "0" ), new ElementAttribute( "role", "button" ) }
											: Enumerable.Empty<ElementAttribute>() ),
								includeIdAttribute: activationBehavior?.IncludesIdAttribute() == true || isFocused,
								jsInitStatements: ( activationBehavior != null ? activationBehavior.JsInitStatementGetter( context.Id ) : "" ).AppendDelimiter(
									// This list of keys is duplicated in the JavaScript file.
									" $( '#{0}' ).keypress( function( e ) {{ if( e.key === ' ' || e.key === 'Enter' ) {{ e.preventDefault(); $( this ).click(); }} }} );"
										.FormatWith( context.Id ) )
								.ConcatenateWithSpace( isFocused ? "document.getElementById( '{0}' ).focus();".FormatWith( context.Id ) : "" ) ) ),
						classes: classes.Add( activationBehavior != null ? activationBehavior.Classes : ElementClassSet.Empty ),
						children: children,
						etherealChildren: ( activationBehavior?.EtherealChildren ?? Enumerable.Empty<EtherealComponent>() ).Concat( etherealChildren ).Materialize() );
				} );

		/// <summary>
		/// Creates hyperlink behavior. EnduraCode goal 2450 will add a JavaScript predicate parameter to this method.
		/// </summary>
		/// <param name="hyperlinkBehavior">The behavior. Pass a <see cref="ResourceInfo"/> to navigate to the resource in the default way, or call
		/// <see cref="HyperlinkBehaviorExtensionCreators.ToHyperlinkNewTabBehavior(ResourceInfo, bool)"/> or
		/// <see cref="HyperlinkBehaviorExtensionCreators.ToHyperlinkModalBoxBehavior(ResourceInfo, bool, BrowsingContextSetup)"/>. For a mailto link, call
		/// <see cref="HyperlinkBehaviorExtensionCreators.ToHyperlinkBehavior(Email.EmailAddress, string, string, string, string)"/>.</param>
		public static ElementActivationBehavior CreateHyperlink( HyperlinkBehavior hyperlinkBehavior ) => new ElementActivationBehavior( hyperlinkBehavior );

		/// <summary>
		/// Creates button behavior. EnduraCode goal 2450 will add a JavaScript predicate parameter to this method.
		/// </summary>
		/// <param name="buttonBehavior">The behavior. Pass null to use the form default action.</param>
		public static ElementActivationBehavior CreateButton( ButtonBehavior buttonBehavior = null ) =>
			new ElementActivationBehavior( buttonBehavior ?? new FormActionBehavior( FormState.Current.DefaultAction ) );

		[ Obsolete( "Guaranteed through 15 April 2021." ) ]
		public static ElementActivationBehavior CreateRedirectScript( ResourceInfo resource ) {
			return new ElementActivationBehavior( resource: resource );
		}

		[ Obsolete( "Guaranteed through 15 April 2021." ) ]
		public static ElementActivationBehavior CreatePostBackScript( PostBack postBack = null ) {
			return new ElementActivationBehavior( action: new PostBackFormAction( postBack ?? FormState.Current.PostBack ) );
		}

		[ Obsolete( "Guaranteed through 15 April 2021." ) ]
		public static ElementActivationBehavior CreateCustomScript( string script ) {
			return new ElementActivationBehavior( script: script );
		}

		internal readonly ElementClassSet Classes;
		internal readonly Func<IReadOnlyCollection<ElementAttribute>> AttributeGetter;
		internal readonly Func<bool> IncludesIdAttribute;
		internal readonly IReadOnlyCollection<EtherealComponent> EtherealChildren;
		internal readonly Func<string, string> JsInitStatementGetter;
		internal readonly bool IsFocusable;
		internal readonly Action PostBackAdder;

		private ElementActivationBehavior( HyperlinkBehavior hyperlinkBehavior ) {
			Classes = hyperlinkBehavior.HasDestination ? ActivatableClass : ElementClassSet.Empty;
			AttributeGetter = () => hyperlinkBehavior.AttributeGetter( true );
			IncludesIdAttribute = () => hyperlinkBehavior.IncludesIdAttribute( true );
			EtherealChildren = hyperlinkBehavior.EtherealChildren;
			JsInitStatementGetter = id => hyperlinkBehavior.JsInitStatementGetter( id, true );
			IsFocusable = hyperlinkBehavior.IsFocusable;
			PostBackAdder = hyperlinkBehavior.PostBackAdder;
		}

		private ElementActivationBehavior( ButtonBehavior buttonBehavior ) {
			Classes = ActivatableClass;
			AttributeGetter = () => buttonBehavior.GetAttributes().Materialize();
			IncludesIdAttribute = buttonBehavior.IncludesIdAttribute;
			EtherealChildren = buttonBehavior.GetEtherealChildren();
			JsInitStatementGetter = buttonBehavior.GetJsInitStatements;
			IsFocusable = true;
			PostBackAdder = buttonBehavior.AddPostBack;
		}

		[ Obsolete( "Guaranteed through 15 April 2021." ) ]
		private ElementActivationBehavior( ResourceInfo resource = null, FormAction action = null, string script = "" ) {
			if( action == null && !script.Any() ) {
				HyperlinkBehavior hyperlinkBehavior = resource;

				Classes = hyperlinkBehavior.HasDestination ? ActivatableClass : ElementClassSet.Empty;
				AttributeGetter = () => hyperlinkBehavior.AttributeGetter( true );
				IncludesIdAttribute = () => hyperlinkBehavior.IncludesIdAttribute( true );
				EtherealChildren = hyperlinkBehavior.EtherealChildren;
				JsInitStatementGetter = id => hyperlinkBehavior.JsInitStatementGetter( id, true );
				IsFocusable = hyperlinkBehavior.IsFocusable;
				PostBackAdder = hyperlinkBehavior.PostBackAdder;
			}
			else {
				var buttonBehavior = action != null ? (ButtonBehavior)new FormActionBehavior( action ) : new CustomButtonBehavior( () => script + ";" );

				Classes = ActivatableClass;
				AttributeGetter = () => buttonBehavior.GetAttributes().Materialize();
				IncludesIdAttribute = buttonBehavior.IncludesIdAttribute;
				EtherealChildren = buttonBehavior.GetEtherealChildren();
				JsInitStatementGetter = buttonBehavior.GetJsInitStatements;
				IsFocusable = true;
				PostBackAdder = buttonBehavior.AddPostBack;
			}
		}
	}
}