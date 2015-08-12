namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	public static class RevisionHistoryStatics {
		/// <summary>
		/// EWL use only.
		/// </summary>
		public static RevisionHistoryProvider SystemProvider { get { return (RevisionHistoryProvider)DataAccessStatics.SystemProvider; } }
	}
}