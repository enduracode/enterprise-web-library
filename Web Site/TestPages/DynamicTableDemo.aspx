<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="DynamicTableDemo.aspx.cs" Inherits="EnterpriseWebLibrary.WebSite.TestPages.DynamicTableDemo"
	MasterPageFile="~/Ewf/EwfUi.master" %>

<asp:Content runat="server" ID="content" ContentPlaceHolderID="contentPlace">
	<ewf:DynamicTable runat="server" ID="table" PostBackIdBase="1" Caption="Table One - Nothing Special" SubCaption="This is the sub caption" />
	<ewf:DynamicTable runat="server" ID="table2" PostBackIdBase="2" Caption="Table Two - Selected Rows Action" />
	<ewf:DynamicTable runat="server" ID="table3" PostBackIdBase="3" Caption="Table Three - Clickable Rows with row tooltip" />
	<ewf:DynamicTable runat="server" ID="table4" PostBackIdBase="4" Caption="Table Four - Mixed, Selected Rows Action and Clickable Rows with row tool tip control" />
	<ewf:DynamicTable runat="server" ID="table5" PostBackIdBase="5" Caption="Table Five - Reordable" SubCaption="Not actually functioal" />
	<ewf:DynamicTable runat="server" ID="table6" PostBackIdBase="6" Caption="Table Six - Mixed, Single-Celled" SubCaption="Honk if you love unNeccesary caPitalization" />
	<ewf:DynamicTable runat="server" ID="table7" PostBackIdBase="7" Caption="Table Seven - Reordable, Clickable Rows" SubCaption="Not actually functioal" />
	<ewf:DynamicTable runat="server" ID="table8" PostBackIdBase="8" Caption="Table Eight - Reordable, Clickable Rows, Selected Rows Action" SubCaption="Not actually functioal" />
	<ewf:DynamicTable runat="server" ID="table9" PostBackIdBase="9" Caption="Table Nine - Rowspans" />
</asp:Content>
