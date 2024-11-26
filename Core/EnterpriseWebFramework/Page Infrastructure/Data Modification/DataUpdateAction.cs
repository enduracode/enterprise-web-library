namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public class DataUpdateAction: DataModificationAction, ValidationList {
	public static implicit operator DataModificationActionsParameter( DataUpdateAction action ) => new( [ action ] );

	internal readonly BasicDataModification Action;

	internal DataUpdateAction( BasicDataModification dataModificationAction ) {
		Action = dataModificationAction;
	}

	void ValidationList.AddValidation( EwfValidation validation ) {
		Action.AddValidation( validation );
	}
}