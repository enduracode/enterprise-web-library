<%@ Page Language="C#" CodeBehind="RegexHelper.aspx.cs" Inherits="EnterpriseWebLibrary.WebSite.TestPages.RegexHelper" MasterPageFile="~/Ewf/EwfUi.master" %>

<asp:Content runat="server" ContentPlaceHolderID="contentPlace">
	<ewf:ControlStack runat="server" IsStandard="true">
		<p>This page can be used to help develop and test regular expressions.</p>
		<ewf:EwfTextBox runat="server" ID="regexBox" />
		<ewf:Checklist runat="server" ID="regexOptions" Caption="Regular Expression Options" NumberOfColumns="5" />
		<ewf:LabeledControl runat="server" Label="Input"><ewf:EwfTextBox runat="server" Rows="6" ID="input" /></ewf:LabeledControl>
		<ewf:BlockCheckBox runat="server" ID="replace" Text="Find - Replace">
			<ewf:LabeledControl runat="server" Label="Replacement Text"><ewf:EwfTextBox runat="server" ID="replacementText" /></ewf:LabeledControl>
			<ewf:LabeledControl runat="server" Label="Output"><ewf:EwfTextBox runat="server" ID="outputText" Rows="13" /></ewf:LabeledControl>
		</ewf:BlockCheckBox><asp:PlaceHolder runat="server" ID="goPlace" />
		<ewf:DynamicTable runat="server" IsStandard="true" ID="output" Caption="Results" />
	</ewf:ControlStack>
</asp:Content>
