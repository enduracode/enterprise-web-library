using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.JavaScriptWriting;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Activation behavior for an element that is not a hyperlink or button. Will add rollover behavior to table element(s), unless it is used on a field of an
	/// EWF table or an item of a column primary table. Column hover behavior is not possible with CSS.
	/// </summary>
	public class ElementActivationBehavior {
		// This class name is used by EWF CSS and JavaScript files.
		internal static readonly ElementClass ActivatableClass = new ElementClass( "ewfAc" );

		/// <summary>
		/// Creates a script that redirects to the specified resource. Passing null for resourceInfo will result in no script being added.
		/// </summary>
		public static ElementActivationBehavior CreateRedirectScript( ResourceInfo resource ) {
			return new ElementActivationBehavior( resource: resource );
		}

		/// <summary>
		/// Creates a script that performs a post-back.
		/// </summary>
		/// <param name="postBack">Pass null to use the post-back corresponding to the first of the current data modifications.</param>
		public static ElementActivationBehavior CreatePostBackScript( PostBack postBack = null ) {
			return new ElementActivationBehavior( action: new PostBackFormAction( postBack ?? FormState.Current.PostBack ) );
		}

		/// <summary>
		/// Creates a custom script. A semicolon will be added to the end of the script. Do not pass null for script.
		/// </summary>
		public static ElementActivationBehavior CreateCustomScript( string script ) {
			return new ElementActivationBehavior( script: script );
		}

		internal readonly ElementClassSet Classes;
		internal readonly Func<IReadOnlyCollection<Tuple<string, string>>> AttributeGetter;
		internal readonly bool IncludeIdAttribute;
		internal readonly IReadOnlyCollection<EtherealComponent> EtherealChildren;
		internal readonly Func<string, string> JsInitStatementGetter;
		internal readonly bool IsFocusable;
		internal readonly Action PostBackAdder;

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		private ResourceInfo resource;
		private FormAction action;
		private readonly string script;

		private ElementActivationBehavior( ResourceInfo resource = null, FormAction action = null, string script = "" ) {
			if( action == null && !script.Any() ) {
				HyperlinkBehavior hyperlinkBehavior = resource;

				Classes = hyperlinkBehavior.HasDestination() ? ActivatableClass : ElementClassSet.Empty;
				AttributeGetter = () => hyperlinkBehavior.AttributeGetter( true );
				IncludeIdAttribute = hyperlinkBehavior.IncludesIdAttribute( true );
				EtherealChildren = hyperlinkBehavior.EtherealChildren;
				JsInitStatementGetter = id => hyperlinkBehavior.JsInitStatementGetter( id, true );
				IsFocusable = hyperlinkBehavior.IsFocusable;
				PostBackAdder = hyperlinkBehavior.PostBackAdder;
			}
			else {
				var buttonBehavior = action != null ? (ButtonBehavior)new FormActionBehavior( action ) : new CustomButtonBehavior( () => script + ";" );

				Classes = ActivatableClass;
				AttributeGetter = () => buttonBehavior.GetAttributes().Materialize();
				IncludeIdAttribute = buttonBehavior.IncludesIdAttribute();
				EtherealChildren = buttonBehavior.GetEtherealChildren();
				JsInitStatementGetter = buttonBehavior.GetJsInitStatements;
				IsFocusable = true;
				PostBackAdder = buttonBehavior.AddPostBack;
			}

			// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
			this.resource = resource;
			this.action = action;
			this.script = script;
		}

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		internal void SetUpClickableControl( WebControl clickableControl ) {
			if( resource == null && action == null && script == "" )
				return;

			clickableControl.CssClass = clickableControl.CssClass.ConcatenateWithSpace( ActivatableClass.ClassName );

			if( resource != null && EwfPage.Instance.IsAutoDataUpdater ) {
				action = HyperlinkBehavior.GetHyperlinkPostBackAction( resource );
				resource = null;
			}

			Func<string> scriptGetter;
			if( resource != null )
				scriptGetter = () => "location.href = '" + EwfPage.Instance.GetClientUrl( resource.GetUrl() ) + "'; return false";
			else if( action != null ) {
				action.AddToPageIfNecessary();
				scriptGetter = () => action.GetJsStatements() + " return false";
			}
			else
				scriptGetter = () => script;

			// Defer script generation until after all controls have IDs.
			EwfPage.Instance.PreRender += delegate { clickableControl.AddJavaScriptEventScript( JsWritingMethods.onclick, scriptGetter() ); };
		}
	}
}