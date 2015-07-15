using System;

namespace EnterpriseWebLibrary.InstallationSupportUtility {
	/// <summary>
	/// Contains useful information about the execution of an operation. Created by the caller of the operation.
	/// All properties are set by the operation itself.
	/// </summary>
	public class OperationResult {
		/// <summary>
		/// If applicable, the number of bytes transferred during this operation. The operation will set this value if it is applicable.
		/// </summary>
		public long? NumberOfBytesTransferred { get; set; }

		public TimeSpan? TimeSpentWaitingForNetwork { get; set; }

		private string summary = "";

		/// <summary>
		/// The detailed summary of the operation.
		/// </summary>
		public string Summary { get { return summary; } set { summary = value; } }
	}
}