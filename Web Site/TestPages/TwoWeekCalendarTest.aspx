<%@ Page Language="C#" CodeBehind="TwoWeekCalendarTest.aspx.cs" Inherits="EnterpriseWebLibrary.WebSite.TestPages.TwoWeekCalendarTest" MasterPageFile="~/Ewf/EwfUi.master" %>

<asp:Content runat="server" ContentPlaceHolderID="contentPlace">
	<h1>This weeks MonthViewCalendar</h1>
	<ewf:MonthViewCalendar runat="server" ID="twoWeek" IsTwoWeekCalendar="true" CustomToolTipSkin="WhiteAndNavy" />
	<hr />
	<asp:Panel runat="server" ID="container" />
</asp:Content>
