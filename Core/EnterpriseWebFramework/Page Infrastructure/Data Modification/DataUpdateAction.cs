namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public class DataUpdateAction: DataModification, ValidationList {
	public static implicit operator DataModificationsParameter( DataUpdateAction action ) => new( [ action ] );

	internal readonly BasicDataModification Action;

	internal DataUpdateAction( BasicDataModification dataModificationAction ) {
		Action = dataModificationAction;
	}

	void ValidationList.AddValidation( EwfValidation validation ) {
		Action.AddValidation( validation );
	}
}