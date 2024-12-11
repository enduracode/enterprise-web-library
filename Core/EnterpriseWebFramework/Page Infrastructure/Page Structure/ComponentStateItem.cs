using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public abstract class ComponentStateItem {
	private static Action creationTimeAsserter = null!;
	private static Func<string> elementOrIdentifiedComponentIdGetter = null!;
	private static Func<string, JToken?> valueGetter = null!;
	private static Func<IReadOnlyCollection<DataModificationAction>> dataModificationActionGetter = null!;
	private static Action<string, ComponentStateItem> itemAdder = null!;

	internal static void Init(
		Action creationTimeAsserter, Func<string> elementOrIdentifiedComponentIdGetter, Func<string, JToken?> valueGetter,
		Func<IReadOnlyCollection<DataModificationAction>> dataModificationActionGetter, Action<string, ComponentStateItem> itemAdder ) {
		ComponentStateItem.creationTimeAsserter = creationTimeAsserter;
		ComponentStateItem.elementOrIdentifiedComponentIdGetter = elementOrIdentifiedComponentIdGetter;
		ComponentStateItem.valueGetter = valueGetter;
		ComponentStateItem.dataModificationActionGetter = dataModificationActionGetter;
		ComponentStateItem.itemAdder = itemAdder;
	}

	/// <summary>
	/// Creates a component-state item, which gives you a hidden-field value that you can access while building the page.
	/// </summary>
	/// <param name="id">The ID of this state item, which must be unique within the page or current ID context. Do not pass null or the empty string.</param>
	/// <param name="durableValue">The current value of this state item in persistent storage. For transient state that is used only to support intermediate
	/// post-backs, or non-deterministic state such as a randomly generated string, pass the default value.</param>
	/// <param name="valueValidator">A predicate that takes a value and returns true if it is valid for this state item. Used primarily to validate post-back
	/// values.</param>
	/// <param name="includeInChangeDetection">Pass true to include this state item in change detection for the current data modification actions. This is
	/// necessary when a change in the value of this state item affects what will be persisted by the actions. For transient state that is used only to support
	/// intermediate post-backs, or non-deterministic state such as a randomly generated string, pass false.</param>
	public static ComponentStateItem<T> Create<T>( string id, T durableValue, Func<T?, bool> valueValidator, bool includeInChangeDetection ) {
		creationTimeAsserter();

		id = elementOrIdentifiedComponentIdGetter().AppendDelimiter( "_" ) + id;
		var item = new ComponentStateItem<T>( durableValue, valueGetter( id ), valueValidator, includeInChangeDetection, dataModificationActionGetter() );
		itemAdder( id, item );
		return item;
	}

	internal abstract string DurableValueAsString { get; }
	internal abstract bool ValueIsInvalid();
	internal abstract bool IncludedInChangeDetection { get; }
	internal abstract IReadOnlyCollection<DataModificationAction> DataModificationActions { get; }
	internal abstract bool ValueChanged();
	internal abstract JToken ValueAsJson { get; }
}

public sealed class ComponentStateItem<T>: ComponentStateItem, AbstractDataValue<T>, EtherealComponent {
	private readonly SpecifiedValue<T>? durableValue;
	private T value;
	private readonly bool valueIsInvalid;
	private readonly bool includedInChangeDetection;
	private readonly IReadOnlyCollection<DataModificationAction> dataModificationActions;

	internal ComponentStateItem(
		T durableValue, JToken? value, Func<T?, bool> valueValidator, bool includeInChangeDetection,
		IReadOnlyCollection<DataModificationAction> dataModificationActions ) {
		if( !valueValidator( durableValue ) )
			throw new ApplicationException( "The specified durable value is invalid according to the specified value validator." );
		if( includeInChangeDetection )
			this.durableValue = new SpecifiedValue<T>( durableValue );

		if( value is not null && tryConvertValue( value, out var convertedValue ) && valueValidator( convertedValue ) )
			this.value = convertedValue!;
		else {
			this.value = durableValue;
			valueIsInvalid = value is not null;
		}

		includedInChangeDetection = includeInChangeDetection;
		this.dataModificationActions = dataModificationActions;
	}

	private bool tryConvertValue( JToken valueAsJson, out T? convertedValue ) {
		try {
			convertedValue = valueAsJson.ToObject<T>();
		}
		catch {
			convertedValue = default;
			return false;
		}
		return true;
	}

	/// <summary>
	/// Gets or sets the value representing the state.
	/// </summary>
	public T Value { get => value; set => this.value = value; }

	bool AbstractDataValue<T>.DataExists => true;
	IReadOnlyCollection<EtherealComponentOrElement> EtherealComponent.GetChildren() => [ ];
	internal override string DurableValueAsString => JsonConvert.SerializeObject( durableValue!.Value, Formatting.None );
	internal override bool ValueIsInvalid() => valueIsInvalid;
	internal override bool IncludedInChangeDetection => includedInChangeDetection;
	internal override IReadOnlyCollection<DataModificationAction> DataModificationActions => dataModificationActions;
	internal override bool ValueChanged() => !EwlStatics.AreEqual( value, durableValue!.Value );
	internal override JToken ValueAsJson => value is null ? JValue.CreateNull() : JToken.FromObject( value );
}