﻿using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.Installation;

namespace EnterpriseWebLibrary;

public static class GlobalStatics {
	public static readonly string[] ConfigurationXsdFileNames = { "Installation Standard", "Machine", "System Development", "System General" };

	private static InstallationSharedConfiguration? installationSharedConfiguration;

	internal static void Init() {
		installationSharedConfiguration = ConfigurationStatics.LoadInstallationSharedConfiguration<InstallationSharedConfiguration>();
	}

	internal static string IntermediateLogInPassword => installationSharedConfiguration!.IntermediateLogInPassword;
	internal static string EmailDefaultFromName => installationSharedConfiguration!.EmailDefaultFromName;
	internal static string EmailDefaultFromAddress => installationSharedConfiguration!.EmailDefaultFromAddress;
}