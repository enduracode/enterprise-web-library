using System;
using EnterpriseWebLibrary.InputValidation;
using Newtonsoft.Json.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public abstract class ComponentStateItem {
		private static Action creationTimeAsserter;
		private static Func<string> elementOrIdentifiedComponentIdGetter;
		private static Func<string, JToken> valueGetter;
		private static Action<string, ComponentStateItem> itemAdder;

		internal static void Init(
			Action creationTimeAsserter, Func<string> elementOrIdentifiedComponentIdGetter, Func<string, JToken> valueGetter,
			Action<string, ComponentStateItem> itemAdder ) {
			ComponentStateItem.creationTimeAsserter = creationTimeAsserter;
			ComponentStateItem.elementOrIdentifiedComponentIdGetter = elementOrIdentifiedComponentIdGetter;
			ComponentStateItem.valueGetter = valueGetter;
			ComponentStateItem.itemAdder = itemAdder;
		}

		/// <summary>
		/// Creates a component-state item, which gives you a hidden-field value that you can access while building the page.
		/// </summary>
		/// <param name="id">The ID of this state item, which must be unique within the page or current ID context. Do not pass null or the empty string.</param>
		/// <param name="durableValue">The current value of this state item in persistent storage. For transient state that is used only to support intermediate
		/// post-backs, pass the default value.</param>
		/// <param name="valueValidator">A predicate that takes a value and returns true if it is valid for this state item. Used primarily to validate post-back
		/// values.</param>
		/// <param name="durableValueUpdateValidationMethod">The validation method, which you should use to update the durable value, if this is necessary. Pass
		/// null for transient state that is used only to support intermediate post-backs.</param>
		public static ComponentStateItem<T> Create<T>(
			string id, T durableValue, Func<T, bool> valueValidator, Action<T, Validator> durableValueUpdateValidationMethod = null ) {
			creationTimeAsserter();

			id = elementOrIdentifiedComponentIdGetter().AppendDelimiter( "_" ) + id;
			var item = new ComponentStateItem<T>( durableValue, valueGetter( id ), valueValidator, durableValueUpdateValidationMethod );
			itemAdder( id, item );
			return item;
		}

		internal abstract object DurableValue { get; }
		internal abstract bool ValueIsInvalid();
		internal abstract JToken ValueAsJson { get; }
	}

	public sealed class ComponentStateItem<T>: ComponentStateItem {
		private readonly T durableValue;
		private readonly DataValue<T> value;
		private readonly bool valueIsInvalid;
		private readonly EwfValidation durableValueUpdateValidation;

		internal ComponentStateItem( T durableValue, JToken value, Func<T, bool> valueValidator, Action<T, Validator> durableValueUpdateValidationMethod ) {
			if( !valueValidator( durableValue ) )
				throw new ApplicationException( "The specified durable value is invalid according to the specified value validator." );
			this.durableValue = durableValue;

			if( value != null && tryConvertValue( value, out var convertedValue ) && valueValidator( convertedValue ) )
				this.value = new DataValue<T> { Value = convertedValue };
			else {
				this.value = new DataValue<T> { Value = durableValue };
				valueIsInvalid = value != null;
			}

			if( durableValueUpdateValidationMethod != null )
				durableValueUpdateValidation = new EwfValidation( validator => durableValueUpdateValidationMethod( this.value.Value, validator ) );
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

		/// <summary>
		/// Gets the durable-value-update validation, or null if there isn’t one.
		/// </summary>
		public EwfValidation DurableValueUpdateValidation => durableValueUpdateValidation;

		internal override object DurableValue => durableValue;
		internal override bool ValueIsInvalid() => valueIsInvalid;
		internal override JToken ValueAsJson => JToken.FromObject( value.Value );
	}
}