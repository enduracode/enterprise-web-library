<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="FormFieldDemo.aspx.cs" Inherits="EnterpriseWebLibrary.WebSite.TestPages.FormFieldDemo" MasterPageFile="~/Ewf/EwfUi.master" %>

<asp:Content runat="server" ID="content" ContentPlaceHolderID="contentPlace">
	<ewf:StaticTable runat="server" IsForm="true">
		<tr runat="server">
			<td style="width: 14%;">UnFiltered drop down</td>
			<td><ewf:EwfListControl runat="server" Type="DropDownList" ID="dropDownList" /></td>
		</tr>
		<tr runat="server">
			<td>Toggle date row</td>
			<td><ewf:ToggleButton runat="server" ID="toggler" Text="Click here to toggle" /></td>
		</tr>
		<tr runat="server" id="dateRow">
			<td>Date</td>
			<td><ewf:DatePicker runat="server" ID="datePicker" /></td>
		</tr>
		<tr runat="server">
			<td>Text box</td>
			<td><ewf:EwfTextBox runat="server" ID="textBox" /></td>
		</tr>
		<tr runat="server">
			<td>Time</td>
			<td><ewf:TimePicker runat="server" ID="timePicker" /></td>
		</tr>
		<tr>
			<td>Numeric amount (right align):</td>
			<td><ewf:HtmlBlockEditor runat="server" /></td>
		</tr>
		<tr runat="server">
			<td>Choice</td>
			<td><ewf:EwfListControl runat="server" ID="choiceList" Type="HorizontalRadioButton" /></td>
		</tr>
		<tr runat="server">
			<td>Parent of dependent drop down</td>
			<td><ewf:EwfListControl runat="server" ID="parentList" /></td>
		</tr>
		<tr runat="server">
			<td>Dependent drop down</td>
			<td><ewf:DependentDropDownList runat="server" ID="dependentList" /></td>
		</tr>
		<tr runat="server">
			<td>Mailto Link</td>
			<td><ewf:MailtoLink runat="server" ID="mailto" Text="Mailto Link" /></td>
		</tr>
		<tr runat="server">
			<td>Image</td>
			<td><asp:Image runat="server" ID="image" /></td>
		</tr>
		<tr runat="server">
			<td>Image</td>
			<td><asp:Image runat="server" ID="image1" /></td>
		</tr>
		<tr runat="server">
			<td>Ewf Date Picker</td>
			<td><ewf:DatePicker runat="server" ID="datepicker1" /></td>
		</tr>
		<tr runat="server">
			<td>Ewf Date Picker1 Value</td>
			<td><asp:Literal runat="server" ID="datepicker1value" /></td>
		</tr>
		<tr runat="server">
			<td>EWf Date picker mirrored</td>
			<td><ewf:DatePicker runat="server" ID="mirroredDatepicker" /></td>
		</tr>
		<tr runat="server">
			<td>Mirrored Date Picker Value</td>
			<td><asp:Literal runat="server" ID="mirroredDatepickerValue" /></td>
		</tr>
		<tr runat="server">
			<td>Free Form Radio List</td>
			<td><asp:PlaceHolder runat="server" ID="freeFormRadioListTest" /></td>
		</tr>
		<tr>
			<td></td>
			<td><asp:PlaceHolder runat="server" ID="buttonPlace" /></td>
		</tr>
	</ewf:StaticTable>
	<p>The values of the controls are: </p>
	<ewf:ControlStack ID="controlValues" runat="server" IsStandard="true" />
	<asp:PlaceHolder runat="server" ID="ph" />
</asp:Content>
