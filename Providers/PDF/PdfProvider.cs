using EnterpriseWebLibrary.ExternalFunctionality;

namespace EnterpriseWebLibrary.Pdf;

public class PdfProvider: ExternalPdfProvider {
	void ExternalPdfProvider.InitStatics( string asposePdfLicensePath ) {
		new Aspose.Pdf.License().SetLicense( asposePdfLicensePath );
	}
}