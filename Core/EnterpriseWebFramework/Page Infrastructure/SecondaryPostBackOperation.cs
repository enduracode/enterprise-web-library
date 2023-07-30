﻿#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	// ModifyDataAndPerformAction will be used when we implement goal 588.
	// ValidateChangesOnly will be used when we implement goal 478.
	internal enum SecondaryPostBackOperation {
		NoOperation,
		ModifyDataAndPerformAction,
		Validate,
		ValidateChangesOnly
	}
}