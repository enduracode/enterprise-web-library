#nullable disable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public abstract class ComponentStateItem {
	private static Action creationTimeAsserter;
	private static Func<string> elementOrIdentifiedComponentIdGetter;
	private static Func<string, JToken> valueGetter;
	private static Func<IReadOnlyCollection<DataModification>> dataModificationGetter;
	private static Action<string, ComponentStateItem> itemAdder;

	internal static void Init(
		Action creationTimeAsserter, Func<string> elementOrIdentifiedComponentIdGetter, Func<string, JToken> valueGetter,
		Func<IReadOnlyCollection<DataModification>> dataModificationGetter, Action<string, ComponentStateItem> itemAdder ) {
		ComponentStateItem.creationTimeAsserter = creationTimeAsserter;
		ComponentStateItem.elementOrIdentifiedComponentIdGetter = elementOrIdentifiedComponentIdGetter;
		ComponentStateItem.valueGetter = valueGetter;
		ComponentStateItem.dataModificationGetter = dataModificationGetter;
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
	/// <param name="includeInChangeDetection">Pass true to include this state item in change detection for the current data modifications. This is necessary when
	/// a change in the value of this state item affects what will be persisted by the data modifications. For transient state that is used only to support intermediate
	/// post-backs, or non-deterministic state such as a randomly generated string, pass false.</param>
	public static ComponentStateItem<T> Create<T>( string id, T durableValue, Func<T, bool> valueValidator, bool includeInChangeDetection ) {
		creationTimeAsserter();

		id = elementOrIdentifiedComponentIdGetter().AppendDelimiter( "_" ) + id;
		var item = new ComponentStateItem<T>(
			durableValue,
			valueGetter( id ),
			valueValidator,
			includeInChangeDetection ? dataModificationGetter() : Enumerable.Empty<DataModification>().Materialize() );
		itemAdder( id, item );
		return item;
	}

	internal abstract string DurableValueAsString { get; }
	internal abstract bool ValueIsInvalid();
	internal abstract IReadOnlyCollection<DataModification> DataModifications { get; }
	internal abstract bool ValueChanged();
	internal abstract JToken ValueAsJson { get; }
}

public sealed class ComponentStateItem<T>: ComponentStateItem, EtherealComponent {
	private readonly SpecifiedValue<T> durableValue;
	private readonly DataValue<T> value;
	private readonly bool valueIsInvalid;
	private readonly IReadOnlyCollection<DataModification> dataModifications;

	internal ComponentStateItem( T durableValue, JToken value, Func<T, bool> valueValidator, IReadOnlyCollection<DataModification> dataModifications ) {
		if( !valueValidator( durableValue ) )
			throw new ApplicationException( "The specified durable value is invalid according to the specified value validator." );
		if( dataModifications.Any() )
			this.durableValue = new SpecifiedValue<T>( durableValue );

		if( value != null && tryConvertValue( value, out var convertedValue ) && valueValidator( convertedValue ) )
			this.value = new DataValue<T> { Value = convertedValue };
		else {
			this.value = new DataValue<T> { Value = durableValue };
			valueIsInvalid = value != null;
		}

		this.dataModifications = dataModifications;
	}

	private bool tryConvertValue( JToken valueAsJson, out T convertedValue ) {
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
	/// Gets the <see cref="DataValue{T}"/> representing the state.
	/// </summary>
	public DataValue<T> Value => value;

	IReadOnlyCollection<EtherealComponentOrElement> EtherealComponent.GetChildren() => Enumerable.Empty<EtherealComponentOrElement>().Materialize();
	internal override string DurableValueAsString => JsonConvert.SerializeObject( durableValue.Value, Formatting.None );
	internal override bool ValueIsInvalid() => valueIsInvalid;
	internal override IReadOnlyCollection<DataModification> DataModifications => dataModifications;
	internal override bool ValueChanged() => !EwlStatics.AreEqual( value.Value, durableValue.Value );
	internal override JToken ValueAsJson => value.Value == null ? JValue.CreateNull() : JToken.FromObject( value.Value );
}