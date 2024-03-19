using JetBrains.Annotations;
using NodaTime;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A date-and-time edit control.
/// </summary>
public class DateAndTimeControl: FormControl<FlowComponent> {
	private static readonly ElementClass elementClass = new( "ewfDtc" );

	[ UsedImplicitly ]
	private class CssElementCreator: ControlCssElementCreator {
		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
			new CssElement( "DateAndTimeControl", "div.{0}".FormatWith( elementClass.ClassName ) ).ToCollection();
	}

	public FormControlLabeler Labeler { get; }
	public FlowComponent PageComponent { get; }
	public EwfValidation? Validation { get; }

	/// <summary>
	/// Creates a date-and-time control.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="allowEmpty"></param>
	/// <param name="setup">The setup object for the date-and-time control.</param>
	/// <param name="minValue">The earliest acceptable date. Pass null for one hundred twenty years ago. If you would like to reference the current date when
	/// passing a value, use <see cref="PageBase.FirstRequestTime"/> to keep it stable across requests.</param>
	/// <param name="maxValue">The latest acceptable date. Pass null for five years from now. If you would like to reference the current date when passing a
	/// value, use <see cref="PageBase.FirstRequestTime"/> to keep it stable across requests.</param>
	/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
	public DateAndTimeControl(
		LocalDateTime? value, bool allowEmpty, DateAndTimeControlSetup? setup = null, LocalDate? minValue = null, LocalDate? maxValue = null,
		Action<LocalDateTime?, Validator>? validationMethod = null ) {
		setup ??= DateAndTimeControlSetup.Create();

		var date = new InitializationAwareValue<LocalDate?>();
		var dateControl = new DateControl(
			value.ToNewUnderlyingValue( v => v.Date ),
			allowEmpty,
			setup: setup.IsReadOnly
				       ? DateControlSetup.CreateReadOnly( validationPredicate: setup.ValidationPredicate, validationErrorNotifier: setup.ValidationErrorNotifier )
				       : DateControlSetup.Create(
					       autoFillTokens: setup.AutoFillTokens,
					       action: new SpecifiedValue<FormAction>( setup.Action ),
					       valueChangedAction: setup.ValueChangedAction,
					       pageModificationValue: setup.DatePageModificationValue,
					       validationPredicate: setup.ValidationPredicate,
					       validationErrorNotifier: setup.ValidationErrorNotifier ),
			minValue: minValue,
			maxValue: maxValue,
			validationMethod: validationMethod is null ? null : ( postBackValue, _ ) => date.Value = postBackValue );

		var time = new InitializationAwareValue<LocalTime?>();
		var timeControl = new TimeControl(
			value.ToNewUnderlyingValue( v => v.TimeOfDay ),
			allowEmpty,
			setup: setup.IsReadOnly
				       ? TimeControlSetup.CreateReadOnly( validationPredicate: setup.ValidationPredicate, validationErrorNotifier: setup.ValidationErrorNotifier )
				       : TimeControlSetup.Create(
					       autoFillTokens: setup.AutoFillTokens,
					       action: new SpecifiedValue<FormAction>( setup.Action ),
					       valueChangedAction: setup.ValueChangedAction,
					       pageModificationValue: setup.TimePageModificationValue,
					       validationPredicate: setup.ValidationPredicate,
					       validationErrorNotifier: setup.ValidationErrorNotifier ),
			minuteInterval: setup.MinuteInterval ?? 1,
			validationMethod: validationMethod is null ? null : ( postBackValue, _ ) => time.Value = postBackValue );

		Labeler = dateControl.Labeler;

		PageComponent = new GenericFlowContainer(
			new WrappingList(
				dateControl.ToFormItem( label: "Date".ToComponents() )
					.ToComponentCollection()
					.ToComponentListItem()
					.AppendWrappingListItem( timeControl.ToFormItem( label: "Time".ToComponents() ).ToComponentCollection().ToComponentListItem() ) ).ToCollection(),
			displaySetup: setup.DisplaySetup,
			classes: elementClass.Add( setup.Classes ?? ElementClassSet.Empty ) );

		if( validationMethod is not null )
			Validation = new EwfValidation(
				validator => {
					if( !date.Initialized || !time.Initialized )
						return;
					validationMethod( date.Value + time.Value, validator );
				} );
	}
}