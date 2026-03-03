using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase.Classification;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public interface ButtonStyle {
	ElementClassSet GetClasses();
	IEnumerable<ElementAttribute> GetAttributes();
	IReadOnlyCollection<FlowComponent>? GetChildren();
	string GetJsInitStatements( string id );
}