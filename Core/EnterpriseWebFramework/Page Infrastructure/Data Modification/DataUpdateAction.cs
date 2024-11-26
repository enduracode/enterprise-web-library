namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public class DataUpdateAction: DataModificationAction, ValidationList {
	public static implicit operator DataModificationActionsParameter( DataUpdateAction action ) => new( [ action ] );

	internal readonly BasicDataModificationAction Action;

	internal DataUpdateAction( BasicDataModificationAction dataModificationAction ) {
		Action = dataModificationAction;
	}

	void ValidationList.AddValidation( EwfValidation validation ) {
		Action.AddValidation( validation );
	}
}