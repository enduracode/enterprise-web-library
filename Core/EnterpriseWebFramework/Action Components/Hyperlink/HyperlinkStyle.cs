using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase.Classification;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public interface HyperlinkStyle {
	ElementClassSet GetClasses();
	IReadOnlyCollection<FlowComponent> GetChildren( string destinationUrl );
	string GetJsInitStatements( string id );
}