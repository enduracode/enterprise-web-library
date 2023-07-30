#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using JetBrains.Annotations;
using Tewl.InputValidation;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A checkbox that supports nested components.
	/// </summary>
	public class FlowCheckbox: FormControl<FlowComponent> {
		private static readonly ElementClass unhighlightedClass = new ElementClass( "ewfFcu" );
		private static readonly ElementClass highlightedClass = new ElementClass( "ewfFch" );
		private static readonly ElementClass nestedContentClass = new ElementClass( "ewfFcc" );

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
				new[]
					{
						new CssElement( "FlowCheckboxAllStates", new[] { unhighlightedClass, highlightedClass }.Select( getSelector ).ToArray() ),
						new CssElement( "FlowCheckboxUnhighlightedState", getSelector( unhighlightedClass ) ),
						new CssElement( "FlowCheckboxHighlightedState", getSelector( highlightedClass ) ),
						new CssElement( "FlowCheckboxNestedContentContainer", "div.{0}".FormatWith( nestedContentClass.ClassName ) )
					};

			private string getSelector( ElementClass elementClass ) => "div.{0}".FormatWith( elementClass.ClassName );
		}

		public FlowComponent PageComponent { get; }
		public EwfValidation Validation { get; }

		/// <summary>
		/// Creates a checkbox.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="label">The checkbox label. Do not pass null. Pass an empty collection for no label.</param>
		/// <param name="setup">The setup object for the flow checkbox.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using the checkbox for page modification.</param>
		public FlowCheckbox(
			bool value, IReadOnlyCollection<PhrasingComponent> label, FlowCheckboxSetup setup = null,
			Action<PostBackValue<bool>, Validator> validationMethod = null ) {
			setup = setup ?? FlowCheckboxSetup.Create();

			var checkbox = new Checkbox( value, label, setup: setup.CheckboxSetup, validationMethod: validationMethod );

			PageComponent = getComponent(
				setup.DisplaySetup,
				setup.Classes,
				setup.CheckboxSetup.PageModificationValue,
				checkbox,
				setup.HighlightedWhenChecked,
				setup.NestedContentGetter,
				setup.NestedContentAlwaysDisplayed );

			Validation = checkbox.Validation;
		}

		/// <summary>
		/// Creates a radio button.
		/// </summary>
		internal FlowCheckbox( FlowRadioButtonSetup setup, Checkbox checkbox ) {
			PageComponent = getComponent(
				setup.DisplaySetup,
				setup.Classes,
				setup.RadioButtonSetup.PageModificationValue,
				checkbox,
				setup.HighlightedWhenSelected,
				setup.NestedContentGetter,
				setup.NestedContentAlwaysDisplayed );

			Validation = checkbox.Validation;
		}

		private FlowComponent getComponent(
			DisplaySetup displaySetup, ElementClassSet classes, PageModificationValue<bool> pageModificationValue, Checkbox checkbox, bool highlightedWhenChecked,
			Func<IReadOnlyCollection<FlowComponent>> nestedContentGetter, bool nestedContentAlwaysDisplayed ) {
			var nestedContent = nestedContentGetter?.Invoke() ?? Enumerable.Empty<FlowComponent>().Materialize();
			return new GenericFlowContainer(
				checkbox.PageComponent.ToCollection()
					.Concat(
						nestedContent.Any()
							? new GenericFlowContainer(
								nestedContent,
								displaySetup: nestedContentAlwaysDisplayed ? null : pageModificationValue.ToCondition().ToDisplaySetup(),
								classes: nestedContentClass ).ToCollection()
							: Enumerable.Empty<FlowComponent>() )
					.Materialize(),
				displaySetup: displaySetup,
				classes: ( highlightedWhenChecked
					           ? pageModificationValue.ToCondition( isTrueWhenValueSet: false )
						           .ToElementClassSet( unhighlightedClass )
						           .Add( pageModificationValue.ToCondition().ToElementClassSet( highlightedClass ) )
					           : unhighlightedClass ).Add( classes ?? ElementClassSet.Empty ) );
		}

		FormControlLabeler FormControl<FlowComponent>.Labeler => null;
	}
}