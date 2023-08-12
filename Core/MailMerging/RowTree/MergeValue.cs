﻿using EnterpriseWebLibrary.MailMerging.FieldImplementation;

namespace EnterpriseWebLibrary.MailMerging.RowTree;

/// <summary>
/// A combination of a merge field and a data row.
/// </summary>
public interface MergeValue {
	/// <summary>
	/// Gets the name of the merge value.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets the name of the merge value for Microsoft Word.
	/// </summary>
	string MsWordName { get; }

	/// <summary>
	/// Gets a description of the value.
	/// </summary>
	string GetDescription();
}

/// <summary>
/// A combination of a merge field and a data row.
/// </summary>
public interface MergeValue<out ValType>: MergeValue {
	/// <summary>
	/// Evaluates the merge field for the row. Throws an exception if true is specified for ensureValueExists and the merge field has no value for the current
	/// row.
	/// </summary>
	ValType? Evaluate( bool ensureValueExists );
}

internal class MergeValue<RowType, ValType>: MergeValue<ValType> where RowType: class {
	private readonly BasicMergeFieldImplementation<RowType, ValType> field;
	private readonly string name;
	private readonly string msWordName;
	private readonly Func<string> descriptionGetter;
	private readonly Func<RowType?> rowGetter;

	internal MergeValue(
		BasicMergeFieldImplementation<RowType, ValType> field, string name, string msWordName, Func<string> descriptionGetter, Func<RowType?> rowGetter ) {
		this.field = field;
		this.name = name;
		this.msWordName = msWordName;
		this.descriptionGetter = descriptionGetter;
		this.rowGetter = rowGetter;
	}

	public string Name => name;
	public string MsWordName => msWordName;

	public string GetDescription() => descriptionGetter();

	ValType? MergeValue<ValType>.Evaluate( bool ensureValueExists ) {
		var row = rowGetter();

		if( field is BasicMergeFieldImplementation<RowType, string> stringField ) {
			var stringValue = row != null ? stringField.Evaluate( row ) : "";
			if( stringValue == null )
				throw new ApplicationException( "String merge fields must never evaluate to null. (" + Name + ")" );
			if( ensureValueExists && stringValue.Length == 0 )
				throw new MailMergingException( "Merge field " + Name + " has no value for the current row." );
			return (ValType)(object)stringValue;
		}

		var value = row != null ? field.Evaluate( row ) : default;
		if( ensureValueExists && Equals( value, default( ValType ) ) )
			throw new MailMergingException( "Merge field " + Name + " has no value for the current row." );
		return value;
	}
}