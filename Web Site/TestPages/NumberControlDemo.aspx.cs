using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class NumberControlDemo: EwfPage {
		partial class Info {
			public override string ResourceName => "Number Control";
		}

		protected override void loadData() {
			ph.AddControlsReturnThis(
				FormState.ExecuteWithDataModificationsAndDefaultAction(
						PostBack.CreateFull().ToCollection(),
						() => FormItemList.CreateStack(
							generalSetup: new FormItemListSetup( buttonSetup: new ButtonSetup( "Submit" ) ),
							items: getControls().Select( ( getter, i ) => getter( ( i + 1 ).ToString() ) ).Materialize() ) )
					.ToCollection<FlowComponent>()
					.Append(
						new Section(
							"Independent Controls",
							FormItemList.CreateStack( items: getIndependentControls().Select( ( getter, i ) => getter( "I-" + ( i + 1 ).ToString() ) ).Materialize() )
								.ToCollection() ) )
					.GetControls() );
		}

		private IReadOnlyCollection<Func<string, FormItem>> getControls() =>
			new[]
				{
					get( "Standard", null ), get( "[1,2] with .25 step", null, minValue: 1, maxValue: 2, valueStep: .25m ),
					get( "Placeholder", NumberControlSetup.Create( placeholder: "Type here" ) ),
					get( "Birthday year auto-fill", NumberControlSetup.Create( autoFillTokens: "bday-year" ) ),
					get( "Auto-complete", NumberControlSetup.CreateAutoComplete( TestService.GetInfo() ) ), id => {
						var pb = PostBack.CreateIntermediate( null, id: id );
						return FormState.ExecuteWithDataModificationsAndDefaultAction(
							FormState.Current.DataModifications.Append( pb ),
							() => get( "Separate value-changed action", NumberControlSetup.Create( valueChangedAction: new PostBackFormAction( pb ) ) )( id ) );
					},
					new Func<Func<string, FormItem>>(
						() => {
							var pmv = new PageModificationValue<decimal?>();
							return get( "Page modification", NumberControlSetup.Create( pageModificationValue: pmv ), pageModificationValue: pmv );
						} )(),
					get( "Read-only", NumberControlSetup.CreateReadOnly() ), getImprecise( "Imprecise", null ),
					getImprecise( "Imprecise [1,2] with .25 step", null, minValue: 1, maxValue: 2, valueStep: .25m ), id => {
						var pb = PostBack.CreateIntermediate( null, id: id );
						return FormState.ExecuteWithDataModificationsAndDefaultAction(
							FormState.Current.DataModifications.Append( pb ),
							() => getImprecise(
								"Imprecise with separate value-changed action",
								ImpreciseNumberControlSetup.Create( valueChangedAction: new PostBackFormAction( pb ) ) )( id ) );
					},
					new Func<Func<string, FormItem>>(
						() => {
							var pmv = new PageModificationValue<decimal>();
							return getImprecise(
								"Imprecise with page modification",
								ImpreciseNumberControlSetup.Create( pageModificationValue: pmv ),
								pageModificationValue: pmv );
						} )(),
					getImprecise( "Imprecise read-only", ImpreciseNumberControlSetup.CreateReadOnly() )
				};

		private IReadOnlyCollection<Func<string, FormItem>> getIndependentControls() =>
			new Func<string, FormItem>[]
				{
					id => {
						var pb = PostBack.CreateFull( id: id );
						return FormState.ExecuteWithDataModificationsAndDefaultAction( pb.ToCollection(), () => get( "Standard", null )( id ) );
					},
					id => {
						var pb = PostBack.CreateFull( id: id );
						return FormState.ExecuteWithDataModificationsAndDefaultAction(
							pb.ToCollection(),
							() => get(
								"Auto-complete, triggers action when item selected",
								NumberControlSetup.CreateAutoComplete( TestService.GetInfo(), triggersActionWhenItemSelected: true ) )( id ) );
					},
					id => {
						var pb = PostBack.CreateFull( id: id );
						return FormState.ExecuteWithDataModificationsAndDefaultAction(
							pb.ToCollection(),
							() => get(
								"Auto-complete, triggers action when item selected or value changed",
								NumberControlSetup.CreateAutoComplete(
									TestService.GetInfo(),
									triggersActionWhenItemSelected: true,
									valueChangedAction: new PostBackFormAction( pb ) ) )( id ) );
					}
				};

		private Func<string, FormItem>
			get(
				string label, NumberControlSetup setup, PageModificationValue<decimal?> pageModificationValue = null, decimal? minValue = null,
				decimal? maxValue = null, decimal? valueStep = null ) =>
			id => new NumberControl(
				null,
				true,
				setup: setup,
				minValue: minValue,
				maxValue: maxValue,
				valueStep: valueStep,
				validationMethod: ( postBackValue, validator ) => AddStatusMessage( StatusMessageType.Info, "{0}: {1}".FormatWith( id, postBackValue ) ) ).ToFormItem(
				label: "{0}. {1}".FormatWith( id, label )
					.ToComponents()
					.Concat(
						pageModificationValue != null
							? new LineBreak().ToCollection<PhrasingComponent>()
								.Append(
									new SideComments(
										"Value: ".ToComponents()
											.Concat(
												pageModificationValue.ToGenericPhrasingContainer(
													v => v?.Normalize().ToString() ?? "",
													valueExpression => "{0}.toString()".FormatWith( valueExpression ) ) )
											.Materialize() ) )
							: Enumerable.Empty<PhrasingComponent>() )
					.Materialize() );

		private Func<string, FormItem>
			getImprecise(
				string label, ImpreciseNumberControlSetup setup, PageModificationValue<decimal> pageModificationValue = null, decimal? minValue = null,
				decimal? maxValue = null, decimal? valueStep = null ) =>
			id => new ImpreciseNumberControl(
				.25m,
				minValue ?? 0,
				maxValue ?? 1,
				setup: setup,
				valueStep: valueStep,
				validationMethod: ( postBackValue, validator ) => AddStatusMessage( StatusMessageType.Info, "{0}: {1}".FormatWith( id, postBackValue ) ) ).ToFormItem(
				label: "{0}. {1}".FormatWith( id, label )
					.ToComponents()
					.Concat(
						pageModificationValue != null
							? new LineBreak().ToCollection<PhrasingComponent>()
								.Append(
									new SideComments(
										"Value: ".ToComponents()
											.Concat(
												pageModificationValue.ToGenericPhrasingContainer(
													v => v.Normalize().ToString(),
													valueExpression => "{0}.toString()".FormatWith( valueExpression ) ) )
											.Materialize() ) )
							: Enumerable.Empty<PhrasingComponent>() )
					.Materialize() );
	}
}