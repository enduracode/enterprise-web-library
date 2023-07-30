#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	///  The configuration for an item in a table. Options specified on individual cells take precedence over equivalent options specified here.
	/// </summary>
	public class EwfTableItemSetup: EwfTableItemSetup<int> {
		/// <summary>
		/// Creates an item setup object.
		/// </summary>
		/// <param name="classes">The classes. When used on a column, sets the classes on every cell since most styles don't work on col elements.</param>
		/// <param name="size">The height or width. For an EWF table, this is the row height. For a column primary table, this is the column width. If you specify
		/// percentage widths for some or all columns in a table, these values need not add up to 100; they will be automatically scaled if necessary. The automatic
		/// scaling will not happen if there are any columns without a specified width.</param>
		/// <param name="textAlignment">The text alignment of the cells in this item.</param>
		/// <param name="verticalAlignment">The vertical alignment of the cells in this item.</param>
		/// <param name="activationBehavior">The activation behavior.</param>
		/// <param name="id">A value that uniquely identifies this item in table-level and group-level item actions. If you specify this, the item will have a
		/// checkbox that enables it to be included in the actions.</param>
		/// <param name="rankId">The rank ID for this item, which is used for item reordering.</param>
		public static EwfTableItemSetup Create(
			ElementClassSet classes = null, CssLength size = null, TextAlignment textAlignment = TextAlignment.NotSpecified,
			TableCellVerticalAlignment verticalAlignment = TableCellVerticalAlignment.NotSpecified, ElementActivationBehavior activationBehavior = null,
			SpecifiedValue<int> id = null, int? rankId = null ) =>
			new EwfTableItemSetup( classes, size, textAlignment, verticalAlignment, activationBehavior, id, rankId );

		/// <summary>
		/// Creates an item setup object with a specified ID type.
		/// </summary>
		/// <param name="classes">The classes. When used on a column, sets the classes on every cell since most styles don't work on col elements.</param>
		/// <param name="size">The height or width. For an EWF table, this is the row height. For a column primary table, this is the column width. If you specify
		/// percentage widths for some or all columns in a table, these values need not add up to 100; they will be automatically scaled if necessary. The automatic
		/// scaling will not happen if there are any columns without a specified width.</param>
		/// <param name="textAlignment">The text alignment of the cells in this item.</param>
		/// <param name="verticalAlignment">The vertical alignment of the cells in this item.</param>
		/// <param name="activationBehavior">The activation behavior.</param>
		/// <param name="id">A value that uniquely identifies this item in table-level and group-level item actions. If you specify this, the item will have a
		/// checkbox that enables it to be included in the actions.</param>
		/// <param name="rankId">The rank ID for this item, which is used for item reordering.</param>
		public static EwfTableItemSetup<IdType> CreateWithIdType<IdType>(
			ElementClassSet classes = null, CssLength size = null, TextAlignment textAlignment = TextAlignment.NotSpecified,
			TableCellVerticalAlignment verticalAlignment = TableCellVerticalAlignment.NotSpecified, ElementActivationBehavior activationBehavior = null,
			SpecifiedValue<IdType> id = null, int? rankId = null ) =>
			new EwfTableItemSetup<IdType>( classes, size, textAlignment, verticalAlignment, activationBehavior, id, rankId );

		private EwfTableItemSetup(
			ElementClassSet classes, CssLength size, TextAlignment textAlignment, TableCellVerticalAlignment verticalAlignment,
			ElementActivationBehavior activationBehavior, SpecifiedValue<int> id, int? rankId ): base(
			classes,
			size,
			textAlignment,
			verticalAlignment,
			activationBehavior,
			id,
			rankId ) {}
	}

	/// <summary>
	///  The configuration for an item in a table. Options specified on individual cells take precedence over equivalent options specified here.
	/// </summary>
	public class EwfTableItemSetup<IdType> {
		internal readonly EwfTableFieldOrItemSetup FieldOrItemSetup;
		internal readonly SpecifiedValue<IdType> Id;
		internal readonly int? RankId;

		internal EwfTableItemSetup(
			ElementClassSet classes, CssLength size, TextAlignment textAlignment, TableCellVerticalAlignment verticalAlignment,
			ElementActivationBehavior activationBehavior, SpecifiedValue<IdType> id, int? rankId ) {
			FieldOrItemSetup = new EwfTableFieldOrItemSetup( classes, size, textAlignment, verticalAlignment, activationBehavior );
			Id = id;
			RankId = rankId;
		}
	}
}