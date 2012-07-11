<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ChecklistDemo.aspx.cs" Inherits="EnterpriseWebLibrary.WebSite.TestPages.ChecklistDemo" MasterPageFile="~/Ewf/EwfUi.master" %>

<asp:Content runat="server" ID="content" ContentPlaceHolderID="contentPlace">
	<ewf:BlockCheckBox runat="server" Text="IE Display-linking checklist problem"><ewf:BlockCheckBox runat="server" Text="Checkthis">
		<ewf:Checklist runat="server" ID="checkList3" />
	</ewf:BlockCheckBox></ewf:BlockCheckBox>
	<ewf:Checklist runat="server" ID="checklist" />
	<ewf:Checklist runat="server" ID="checklist2" />
</asp:Content>
