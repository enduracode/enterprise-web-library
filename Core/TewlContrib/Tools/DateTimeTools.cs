namespace EnterpriseWebLibrary.TewlContrib {
	public static class DateTimeTools {
		public static readonly string[] DayMonthYearFormats = { dayMonthYearFormatLz, dayMonthYearFormat };
		public static readonly string[] MonthDayYearFormats = { monthDayYearFormat, "MM/dd/yy" };
		public const string HourAndMinuteFormat = "h:mmt";

		private const string dayMonthYearFormatLz = "dd MMM yyyy";
		private const string dayMonthYearFormat = "d MMM yyyy";
		private const string monthDayYearFormat = "MM/dd/yyyy";
		private const string monthYearFormat = "MMMM yyyy";
	}
}