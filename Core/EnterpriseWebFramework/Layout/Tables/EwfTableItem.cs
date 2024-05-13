using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// An item in a table.
/// </summary>
[ PublicAPI ]
public class EwfTableItem: EwfTableItem<int> {
	/// <summary>
	/// Creates a table item.
	/// </summary>
	/// <param name="cells">The cells in this item.</param>
	/// <param name="setup">The setup object for the item.</param>
	public static EwfTableItem Create( IReadOnlyCollection<EwfTableCell> cells, EwfTableItemSetup? setup = null ) => new( setup, cells );

	/// <summary>
	/// Creates a table item.
	/// </summary>
	/// <param name="cells">The cells in this item.</param>
	public static EwfTableItem Create( params EwfTableCell[] cells ) => new( null, cells );

	/// <summary>
	/// Creates a table item.
	/// </summary>
	/// <param name="setup">The setup object for the item.</param>
	/// <param name="cells">The cells in this item.</param>
	public static EwfTableItem Create( EwfTableItemSetup setup, params EwfTableCell[] cells ) => new( setup, cells );

	/// <summary>
	/// Creates a table item with a specified ID type.
	/// </summary>
	/// <param name="cells">The cells in this item.</param>
	/// <param name="setup">The setup object for the item.</param>
	public static EwfTableItem<IdType> CreateWithIdType<IdType>( IReadOnlyCollection<EwfTableCell> cells, EwfTableItemSetup<IdType>? setup = null ) =>
		new( setup, cells );

	/// <summary>
	/// Creates a table item with a specified ID type.
	/// </summary>
	/// <param name="cells">The cells in this item.</param>
	public static EwfTableItem<IdType> CreateWithIdType<IdType>( params EwfTableCell[] cells ) => new( null, cells );

	/// <summary>
	/// Creates a table item with a specified ID type.
	/// </summary>
	/// <param name="setup">The setup object for the item.</param>
	/// <param name="cells">The cells in this item.</param>
	public static EwfTableItem<IdType> CreateWithIdType<IdType>( EwfTableItemSetup<IdType>? setup, params EwfTableCell[] cells ) => new( setup, cells );

	private EwfTableItem( EwfTableItemSetup? setup, IReadOnlyCollection<EwfTableCell> cells ): base( setup, cells ) {}
}

/// <summary>
/// An item in a table.
/// </summary>
public class EwfTableItem<IdType> {
	internal readonly EwfTableItemSetup<IdType> Setup;
	internal readonly IReadOnlyCollection<EwfTableCell> Cells;

	internal EwfTableItem( EwfTableItemSetup<IdType>? setup, IReadOnlyCollection<EwfTableCell> cells ) {
		Setup = setup ?? EwfTableItemSetup.CreateWithIdType<IdType>();

		if( !cells.Any() )
			throw new ApplicationException( "Cell collection must have at least one item." );
		Cells = cells;
	}
}