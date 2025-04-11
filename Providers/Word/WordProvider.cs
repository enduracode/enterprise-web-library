using EnterpriseWebLibrary.ExternalFunctionality;

namespace EnterpriseWebLibrary.Word;

public class WordProvider: ExternalWordProvider {
	void ExternalWordProvider.InitStatics( string asposeWordsLicensePath ) {
		new Aspose.Words.License().SetLicense( asposeWordsLicensePath );
	}
}