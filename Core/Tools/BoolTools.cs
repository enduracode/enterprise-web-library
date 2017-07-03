namespace EnterpriseWebLibrary {
	public static class BoolTools {
		/// <summary>
		/// Returns "Yes" if this is true, "No" if it is false, and the empty string if it is null.
		/// </summary>
		public static string ToYesOrNo( this bool? b ) {
			return b.HasValue ? b.Value.ToYesOrNo() : "";
		}

		/// <summary>
		/// Returns "Yes" if this is true and "No" otherwise.
		/// </summary>
		public static string ToYesOrNo( this bool b ) {
			return b ? "Yes" : "No";
		}

		/// <summary>
		/// Returns "Yes" if this is true and the empty string otherwise.
		/// </summary>
		public static string ToYesOrEmpty( this bool b ) {
			return b ? "Yes" : "";
		}
	}
}