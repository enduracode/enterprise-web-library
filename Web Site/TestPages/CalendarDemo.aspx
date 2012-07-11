<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CalendarDemo.aspx.cs" Inherits="RedStapler.TestWebSite.TestPages.CalendarDemo" MasterPageFile="~/Ewf/EwfUi.master" %>

<asp:Content runat="server" ID="content" ContentPlaceHolderID="contentPlace">
	<ewf:MonthViewCalendar runat="server" ID="calendar" />
</asp:Content>
