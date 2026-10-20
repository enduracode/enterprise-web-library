// This file is vibe coded. Do not edit manually; use an AI agent to make changes.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using Tewl.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations;

class ConfigureWorkstation: Operation {
	private static readonly Operation instance = new ConfigureWorkstation();

	private const string provisioningScript = """
	                                          $ErrorActionPreference = 'Stop'
	                                          Set-StrictMode -Version Latest
	                                          $ProgressPreference = 'SilentlyContinue'
	                                          [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
	                                          $data = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($inputData)) | ConvertFrom-Json
	                                          function Check-Exit([string] $step) { if ($LASTEXITCODE -ne 0) { throw "$step failed (exit $LASTEXITCODE)." } }
	                                          function Protect-Directory([string] $path, [string] $sid, [string] $rights) {
	                                              $null = New-Item -ItemType Directory -Path $path -Force
	                                              $acl = New-Object Security.AccessControl.DirectorySecurity
	                                              $acl.SetAccessRuleProtection($true, $false)
	                                              foreach ($entry in @(@('S-1-5-18', 'FullControl'), @('S-1-5-32-544', 'FullControl'), @($sid, $rights))) {
	                                                  $identity = New-Object Security.Principal.SecurityIdentifier($entry[0])
	                                                  $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($identity, $entry[1], 'ContainerInherit,ObjectInherit', 'None', 'Allow'))
	                                              }
	                                              Set-Acl -LiteralPath $path -AclObject $acl
	                                          }
	                                          $work = Join-Path $env:ProgramData ('EWL-Workstation-' + [guid]::NewGuid().ToString('N'))
	                                          $password = $null
	                                          $startedTask = $false
	                                          $probeId = $null
	                                          $complete = $false
	                                          try {
	                                              Protect-Directory $work 'S-1-5-32-544' 'FullControl'
	                                              $ocFolder = Join-Path $env:ProgramFiles 'OpenCode'
	                                              $oc = Join-Path $ocFolder 'opencode.exe'
	                                              if (!(Test-Path -LiteralPath $oc)) {
	                                                  if ((Test-Path -LiteralPath $ocFolder) -and @(Get-ChildItem -LiteralPath $ocFolder -Force).Count) {
	                                                      throw 'OpenCode directory is nonempty but has no executable. Inspect it before retrying.'
	                                                  }
	                                                  Write-Host 'Installing the official shared OpenCode standalone release...'
	                                                  $release = Invoke-RestMethod 'https://opencode.ai/update/api/latest/cli/opencode'
	                                                  if (!$release.active) { throw 'No active OpenCode release.' }
	                                                  $arm = $env:PROCESSOR_ARCHITECTURE -eq 'ARM64' -or $env:PROCESSOR_ARCHITEW6432 -eq 'ARM64'
	                                                  $name = if ($arm) { 'opencode-windows-arm64.zip' } else { 'opencode-windows-x64-baseline.zip' }
	                                                  $asset = $release.metadata.files.$name
	                                                  if (([uri]$asset.url).Scheme -ne 'https' -or ([uri]$asset.url).Host -ne 'opencode.ai') { throw 'Unexpected release URL.' }
	                                                  $zip = Join-Path $work 'opencode.zip'
	                                                  Invoke-WebRequest $asset.url -OutFile $zip -UseBasicParsing
	                                                  if ((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash -ne $asset.sha256) { throw 'OpenCode checksum mismatch.' }
	                                                  $null = New-Item -ItemType Directory -Path $ocFolder -Force
	                                                  Expand-Archive -LiteralPath $zip -DestinationPath $ocFolder
	                                              }
	                                              & $oc --version
	                                              Check-Exit 'OpenCode version check'
	                                              $pwsh = Join-Path $env:ProgramFiles 'PowerShell\7\pwsh.exe'
	                                              if (!(Test-Path -LiteralPath $pwsh)) {
	                                                  Write-Host 'Installing shared PowerShell 7...'
	                                                  $release = Invoke-RestMethod 'https://api.github.com/repos/PowerShell/PowerShell/releases/latest'
	                                                  $arm = $env:PROCESSOR_ARCHITECTURE -eq 'ARM64' -or $env:PROCESSOR_ARCHITEW6432 -eq 'ARM64'
	                                                  $suffix = if ($arm) { '-win-arm64.msi' } else { '-win-x64.msi' }
	                                                  $assets = @($release.assets | Where-Object { $_.name.EndsWith($suffix) })
	                                                  if ($assets.Count -ne 1) { throw 'Could not identify a unique PowerShell MSI.' }
	                                                  $msi = Join-Path $work 'powershell.msi'
	                                                  Invoke-WebRequest $assets[0].browser_download_url -OutFile $msi -UseBasicParsing
	                                                  $signature = Get-AuthenticodeSignature -LiteralPath $msi
	                                                  if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation,') { throw 'Invalid PowerShell MSI signature.' }
	                                                  $installer = Start-Process msiexec.exe -ArgumentList @('/i', ('"' + $msi + '"'), '/qn', '/norestart', 'ADD_PATH=1') -Wait -PassThru
	                                                  if ($installer.ExitCode -notin @(0, 3010)) { throw "PowerShell installation failed: $($installer.ExitCode)." }
	                                              }
	                                              & $pwsh --version
	                                              Check-Exit 'PowerShell version check'

	                                              $account = Get-LocalUser -Name agents -ErrorAction SilentlyContinue
	                                              $password = Read-Host 'Password for the local agents account (new password if creating it)' -AsSecureString
	                                              if (!$password.Length) { throw 'A nonblank password is required.' }
	                                              if (!$account) {
	                                                  $account = New-LocalUser -Name agents -FullName Agents -Description 'Dedicated standard account for development agents' -Password $password
	                                              }
	                                              if (!$account.Enabled) { throw 'The agents account is disabled. Review it before retrying.' }
	                                              $admins = Get-LocalGroup -SID 'S-1-5-32-544'
	                                              if (Get-LocalGroupMember $admins | Where-Object { $_.SID -eq $account.SID }) { throw 'The agents account must not be an administrator.' }
	                                              $users = Get-LocalGroup -SID 'S-1-5-32-545'
	                                              if (!(Get-LocalGroupMember $users | Where-Object { $_.SID -eq $account.SID })) { Add-LocalGroupMember $users -Member $account }
	                                              Write-Host "Account password expiry: $($account.PasswordExpires). Keep the scheduled task credential current when changing the password."
	                                              $credential = [pscredential]::new(($env:COMPUTERNAME + '\agents'), $password)
	                                              $task = Get-ScheduledTask -TaskName 'OpenCode Server' -ErrorAction SilentlyContinue
	                                              if ($task -and $task.State -eq 'Running') { throw 'OpenCode Server is already running. Stop it before rerunning configuration.' }
	                                              if ($task -and ($task.Principal.UserId -notin @($credential.UserName, $account.SID.Value) -or
	                                                  @($task.Actions).Count -ne 1 -or $task.Actions[0].Execute -ne $oc -or
	                                                  $task.Actions[0].Arguments -ne 'serve --service --hostname 127.0.0.1 --port 45278')) {
	                                                  throw 'An unrelated OpenCode Server task exists. Inspect it before retrying.'
	                                              }
	                                              if (Get-NetTCPConnection -LocalPort 45278 -State Listen -ErrorAction SilentlyContinue) { throw 'Port 45278 is already in use.' }
	                                              $repositoryAcl = Get-Acl -LiteralPath $data.Repository
	                                              $repositoryAcl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($account.SID, 'Modify', 'ContainerInherit,ObjectInherit', 'None', 'Allow'))
	                                              Set-Acl -LiteralPath $data.Repository -AclObject $repositoryAcl
	                                              Protect-Directory $work $account.SID.Value 'ReadAndExecute'
	                                              $setup = Join-Path $work 'account.ps1'
	                                              $accountScript = @'
	                                          param([string] $InputData)
	                                          $ErrorActionPreference = 'Stop'
	                                          try {
	                                              $data = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($InputData)) | ConvertFrom-Json
	                                              foreach ($variable in @(Get-ChildItem Env: | Where-Object { $_.Name -like 'OPENCODE_*' -or $_.Name -like 'XDG_*' })) { Remove-Item ('Env:' + $variable.Name) }
	                                              if ([Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Dedicated account has effective administrator membership.' }
	                                              foreach ($browser in @('AppData\Local\Google\Chrome\User Data', 'AppData\Local\Microsoft\Edge\User Data', 'AppData\Roaming\Mozilla\Firefox\Profiles')) {
	                                                  $path = Join-Path $data.PersonalProfile $browser
	                                                  try { $null = Get-ChildItem -LiteralPath $path -ErrorAction Stop; throw "Personal browser profile is accessible: $path" }
	                                                  catch [System.UnauthorizedAccessException] { }
	                                                  catch [System.Management.Automation.ItemNotFoundException] { }
	                                              }
	                                              $oc = Join-Path $env:ProgramFiles 'OpenCode\opencode.exe'
	                                              $configDirectory = (& $oc debug paths config).Trim()
	                                              if ($LASTEXITCODE -ne 0 -or $configDirectory -ne (Join-Path $env:USERPROFILE '.config\opencode')) { throw 'Unexpected dedicated configuration path.' }
	                                              $stream = $null
	                                              try { $stream = [IO.File]::Open($oc, 'Open', 'Write', 'ReadWrite') }
	                                              catch [UnauthorizedAccessException] { }
	                                              if ($stream) { $stream.Dispose(); throw 'Dedicated account can modify shared OpenCode.' }
	                                              foreach ($command in @(@('service', 'set', 'hostname', '127.0.0.1'), @('service', 'set', 'port', '45278'), @('service', 'set', 'env', 'OPENCODE_DISABLE_AUTOUPDATE', '1'))) {
	                                                  & $oc @command
	                                                  if ($LASTEXITCODE -ne 0) { throw 'Service configuration failed.' }
	                                              }
	                                              $null = & $oc service get password
	                                              if ($LASTEXITCODE -ne 0) { throw 'Service authentication initialization failed.' }
	                                              $null = New-Item -ItemType Directory -Path $configDirectory -Force
	                                              $json = Join-Path $configDirectory 'opencode.json'
	                                              $jsonc = Join-Path $configDirectory 'opencode.jsonc'
	                                              if ((Test-Path $json) -and (Test-Path $jsonc)) { throw 'Both global configuration files exist. Reconcile them before retrying.' }
	                                              $configFile = if (Test-Path $json) { $json } else { $jsonc }
	                                              if (Test-Path $configFile) {
	                                                  $config = Get-Content -LiteralPath $configFile -Raw | ConvertFrom-Json -AsHashtable
	                                                  Copy-Item -LiteralPath $configFile -Destination ($configFile + '.' + [guid]::NewGuid().ToString('N') + '.bak')
	                                              } else { $config = @{ '$schema' = 'https://opencode.ai/config.json' } }
	                                              $config.shell = Join-Path $env:ProgramFiles 'PowerShell\7\pwsh.exe'
	                                              $config.update = 'disable'
	                                              $config | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $configFile -Encoding utf8
	                                              $rulesFile = Join-Path $configDirectory 'AGENTS.md'
	                                              $rule = 'Use dedicated automation browser profiles. Do not connect to an existing personal browser or reuse personal authentication state.'
	                                              $existing = if (Test-Path $rulesFile) { Get-Content $rulesFile -Raw } else { '' }
	                                              if (!$existing.Contains($rule)) { Add-Content -LiteralPath $rulesFile -Value ("`n" + $rule) -Encoding utf8 }
	                                              $rule = 'Agents must not initiate OpenCode installation or upgrades. The shared installation is administrator-managed; only the human performs updates.'
	                                              if (!$existing.Contains($rule)) { Add-Content -LiteralPath $rulesFile -Value ("`n" + $rule) -Encoding utf8 }
	                                              $temp = Join-Path $env:LOCALAPPDATA 'Temp\opencode'
	                                              $null = New-Item -ItemType Directory -Path $temp -Force
	                                              # A unique file proves actual repository write access; the parent verifies personal access too.
	                                              $probe = Join-Path $data.Repository ('.ewl-workstation-' + $data.ProbeId)
	                                              Set-Content -LiteralPath $probe -Value 'Dedicated account access verified.'
	                                          } catch {
	                                              Write-Host ('Dedicated-account setup failed: ' + $_.Exception.Message) -ForegroundColor Red
	                                              $null = Read-Host 'Press Enter to close this window'
	                                              exit 1
	                                          }
	                                          '@
	                                              Set-Content -LiteralPath $setup -Value $accountScript -Encoding UTF8
	                                              $probeId = [guid]::NewGuid().ToString('N')
	                                              $accountData = @{ PersonalProfile = $data.PersonalProfile; Repository = $data.Repository; ProbeId = $probeId } | ConvertTo-Json -Compress
	                                              $encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($accountData))
	                                              $child = Start-Process $pwsh -Credential $credential -LoadUserProfile -WorkingDirectory $work -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $setup + '"'), $encoded) -Wait -PassThru
	                                              if ($child.ExitCode -ne 0) { throw 'Dedicated-account configuration failed; inspect the setup shell error.' }
	                                              $profile = (Get-ItemProperty ('HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\' + $account.SID.Value)).ProfileImagePath
	                                              $serviceFile = Join-Path $profile '.config\opencode\service.json'
	                                              $service = Get-Content -LiteralPath $serviceFile -Raw | ConvertFrom-Json
	                                              if (!$service.password) { throw 'No service password was provisioned.' }
	                                              $action = New-ScheduledTaskAction -Execute $oc -Argument 'serve --service --hostname 127.0.0.1 --port 45278' -WorkingDirectory $profile
	                                              $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([timespan]::Zero) -MultipleInstances IgnoreNew
	                                              $trigger = New-ScheduledTaskTrigger -AtStartup
	                                              $null = Register-ScheduledTask -TaskName 'OpenCode Server' -Action $action -Trigger $trigger -Settings $settings -User $credential.UserName -Password $credential.GetNetworkCredential().Password -RunLevel Limited -Force
	                                              Start-ScheduledTask -TaskName 'OpenCode Server'
	                                              $startedTask = $true
	                                              $authorization = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('opencode:' + $service.password))
	                                              $ready = $false
	                                              for ($attempt = 0; $attempt -lt 30; $attempt++) {
	                                                  try { $null = Invoke-RestMethod 'http://127.0.0.1:45278/api/info' -Headers @{ Authorization = $authorization } -TimeoutSec 2; $ready = $true; break }
	                                                  catch { Start-Sleep -Seconds 1 }
	                                              }
	                                              if (!$ready) { throw 'Server did not become ready. Inspect the dedicated account server log and scheduled task status.' }
	                                              $listeners = @(Get-NetTCPConnection -LocalPort 45278 -State Listen)
	                                              if (!$listeners.Count) { throw 'Server listener disappeared during verification.' }
	                                              foreach ($listener in $listeners) {
	                                                  if ($listener.LocalAddress -notin @('127.0.0.1', '::1')) { throw 'Server is listening outside loopback.' }
	                                                  $process = Get-CimInstance Win32_Process -Filter "ProcessId=$($listener.OwningProcess)"
	                                                  $owner = Invoke-CimMethod -InputObject $process -MethodName GetOwnerSid
	                                                  if ($owner.Sid -ne $account.SID.Value -or $process.SessionId -ne 0) { throw 'Server identity/session isolation verification failed.' }
	                                              }
	                                              foreach ($headers in @(@{}, @{ Authorization = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('opencode:invalid-' + [guid]::NewGuid())) })) {
	                                                  $status = 0
	                                                  try { $null = Invoke-WebRequest 'http://127.0.0.1:45278/api/info' -Headers $headers -UseBasicParsing -TimeoutSec 5 }
	                                                  catch { if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode } else { throw } }
	                                                  if ($status -notin @(401, 403)) { throw 'Server did not reject missing/incorrect credentials.' }
	                                              }
	                                              Protect-Directory $data.ResultDirectory $data.PersonalSid 'FullControl'
	                                              @{ Password = $service.password; Executable = $oc; Probe = (Join-Path $data.Repository ('.ewl-workstation-' + $probeId)) } |
	                                                  ConvertTo-Json | Set-Content -LiteralPath (Join-Path $data.ResultDirectory 'connection.json') -Encoding UTF8
	                                              $complete = $true
	                                              Write-Host 'Administrative provisioning complete.'
	                                          } catch {
	                                              if ($startedTask) { Stop-ScheduledTask -TaskName 'OpenCode Server' -ErrorAction Continue }
	                                              Write-Host ('Workstation provisioning failed: ' + $_.Exception.Message) -ForegroundColor Red
	                                              $null = Read-Host 'Press Enter to close this window'
	                                              exit 1
	                                          } finally {
	                                              if ($password) { $password.Dispose() }
	                                              if (!$complete -and $probeId) { Remove-Item -LiteralPath (Join-Path $data.Repository ('.ewl-workstation-' + $probeId)) -Force -ErrorAction SilentlyContinue }
	                                              if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
	                                          }
	                                          """;

	public static Operation Instance => instance;

	[ SupportedOSPlatform( "windows" ) ]
	private static bool isAdministrator() => new WindowsPrincipal( WindowsIdentity.GetCurrent() ).IsInRole( WindowsBuiltInRole.Administrator );

	private static void runPowerShell( string script, string payload ) {
		var path = EwlStatics.CombinePaths( Environment.GetFolderPath( Environment.SpecialFolder.System ), "WindowsPowerShell", "v1.0", "powershell.exe" );
		var scriptPath = EwlStatics.CombinePaths( Path.GetTempPath(), "ewl-workstation-" + Guid.NewGuid().ToString( "N" ) + ".ps1" );
		// A file avoids Windows’ command-line length limit. This elevated account’s private temp contains no credentials.
		File.WriteAllText( scriptPath, "$inputData = '" + payload + "'" + Newline + script, new UTF8Encoding( true ) );
		var start = new ProcessStartInfo( path ) { UseShellExecute = false };
		foreach( var argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", scriptPath } )
			start.ArgumentList.Add( argument );
		try {
			using var process = Process.Start( start )!;
			process.WaitForExit();
			if( process.ExitCode != 0 )
				throw new Exception( "Administrative setup failed. Review the provisioning window’s error and retry." );
		}
		finally {
			IoMethods.DeleteFile( scriptPath );
		}
	}

	private ConfigureWorkstation() {}

	bool Operation.IsValid( Installation installation ) => installation is DevelopmentInstallation;

	void Operation.Execute( Installation installation, IReadOnlyList<string> arguments, OperationResult operationResult ) {
		try {
			if( !OperatingSystem.IsWindows() || !Environment.Is64BitProcess )
				throw new Exception( "Workstation configuration requires 64-bit Windows and a human-run interactive terminal." );
			if( arguments is [ "--provision", _ ] ) {
				if( !isAdministrator() )
					throw new Exception( "Administrative provisioning requires elevation." );
				runPowerShell( provisioningScript, arguments[ 1 ] );
				return;
			}
			if( arguments.Count != 0 )
				throw new Exception( "Usage: ewl configure-workstation (run from the EwlBill checkout)." );
			if( isAdministrator() )
				throw new Exception( "Start configure-workstation from a non-elevated terminal in your personal account. It requests elevation when needed." );
			if( Environment.UserName.Equals( "agents", StringComparison.OrdinalIgnoreCase ) || Console.IsInputRedirected )
				throw new Exception( "This operation must be initiated interactively by the developer, not an agent." );
			foreach( var name in new[]
				        {
					        "OPENCODE_CONFIG", "OPENCODE_CONFIG_DIR", "OPENCODE_CONFIG_CONTENT", "OPENCODE_CLI_CONFIG_CONTENT", "XDG_CONFIG_HOME", "XDG_DATA_HOME",
					        "XDG_STATE_HOME"
				        } )
				if( !string.IsNullOrEmpty( Environment.GetEnvironmentVariable( name ) ) )
					throw new Exception( $"Clear the {name} override before configuring the workstation’s standard account-owned paths." );

			var repository = Environment.CurrentDirectory;
			var skill = EwlStatics.CombinePaths( repository, "Library", "Files", "Agent Skills", "ewl-opencode-machine-setup", "SKILL.md" );
			if( !File.Exists( skill ) )
				throw new Exception( "Run this operation from the EwlBill checkout containing the machine-setup skill." );
			Console.WriteLine(
				"This installs shared OpenCode and PowerShell 7 if missing, configures the local agents account and startup task, and connects your client." );
			Console.Write( "Continue with workstation configuration? [y/N] " );
			if( !string.Equals( Console.ReadLine(), "y", StringComparison.OrdinalIgnoreCase ) )
				return;

			var resultDirectory = EwlStatics.CombinePaths( Path.GetTempPath(), "ewl-workstation-" + Guid.NewGuid().ToString( "N" ) );
			var payload = Convert.ToBase64String(
				JsonSerializer.SerializeToUtf8Bytes(
					new
						{
							Repository = repository,
							PersonalProfile = Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
							PersonalSid = WindowsIdentity.GetCurrent().User!.Value,
							ResultDirectory = resultDirectory
						} ) );
			try {
				// Relaunch the actual host, supporting both apphost/tool execution and dotnet <assembly>.
				var start = new ProcessStartInfo( Environment.ProcessPath! ) { UseShellExecute = true, Verb = "runas", WorkingDirectory = repository };
				if( Path.GetFileNameWithoutExtension( Environment.ProcessPath! ).Equals( "dotnet", StringComparison.OrdinalIgnoreCase ) )
					start.ArgumentList.Add( Assembly.GetExecutingAssembly().Location );
				foreach( var argument in new[] { "configure-workstation", "--provision", payload } )
					start.ArgumentList.Add( argument );
				using( var process = Process.Start( start )! ) {
					process.WaitForExit();
					if( process.ExitCode != 0 )
						throw new Exception( "Workstation provisioning did not complete. Correct the reported problem and rerun; completed steps are retained." );
				}

				var result =
					JsonSerializer.Deserialize<Dictionary<string, string>>( File.ReadAllText( EwlStatics.CombinePaths( resultDirectory, "connection.json" ) ) )!;
				File.AppendAllText( result[ "Probe" ], "Personal account access verified." );
				IoMethods.DeleteFile( result[ "Probe" ] );
				var configDirectory = EwlStatics.CombinePaths( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".config", "opencode" );
				Directory.CreateDirectory( configDirectory );
				var jsonPath = EwlStatics.CombinePaths( configDirectory, "opencode.json" );
				var jsoncPath = EwlStatics.CombinePaths( configDirectory, "opencode.jsonc" );
				if( File.Exists( jsonPath ) && File.Exists( jsoncPath ) )
					throw new Exception( "Both personal global configuration files exist. Reconcile them before retrying client setup." );
				var configPath = File.Exists( jsonPath ) ? jsonPath : jsoncPath;
				var config = File.Exists( configPath )
					             ? JsonNode.Parse(
						             File.ReadAllText( configPath ),
						             documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip } )!.AsObject()
					             : new JsonObject { [ "$schema" ] = "https://opencode.ai/config.json" };
				if( File.Exists( configPath ) )
					File.Copy( configPath, configPath + "." + Guid.NewGuid().ToString( "N" ) + ".bak" );
				config[ "update" ] = "notify";
				File.WriteAllText( configPath, config.ToJsonString( new JsonSerializerOptions { WriteIndented = true } ) );
				Environment.SetEnvironmentVariable( "OPENCODE_PASSWORD", result[ "Password" ], EnvironmentVariableTarget.User );
				Environment.SetEnvironmentVariable( "OPENCODE_PASSWORD", result[ "Password" ] );
				var installDirectory = Path.GetDirectoryName( result[ "Executable" ] )!;
				var userPath = Environment.GetEnvironmentVariable( "PATH", EnvironmentVariableTarget.User ) ?? "";
				if( !userPath.Split( ';' ).Contains( installDirectory, StringComparer.OrdinalIgnoreCase ) )
					Environment.SetEnvironmentVariable( "PATH", userPath.TrimEnd( ';' ) + ";" + installDirectory, EnvironmentVariableTarget.User );
				Console.WriteLine( "Server verification passed. Provider sign-in and model selection may be needed in the client before setup can continue." );
				Console.WriteLine(
					"For future launches: wt -F -d . opencode --server http://localhost:45278. Restart existing IDE/terminal processes to refresh their environment." );
				var terminal = new ProcessStartInfo( "wt.exe" ) { UseShellExecute = false };
				foreach( var argument in new[]
					        {
						        "-w", "new", "-F", "-d", repository, result[ "Executable" ], "--server", "http://127.0.0.1:45278", "--prompt",
						        $"Read the machine-setup skill at {skill}. Complete this new workstation’s user-level OpenCode configuration and global agent rules under the dedicated account. " +
						        "DU has provisioned the account, shared tools, filesystem access, authenticated server task, and personal client connection. Verify those results, then follow the skill for remaining dependencies and development access. Do not install or upgrade OpenCode."
					        } )
					terminal.ArgumentList.Add( argument );
				Process.Start( terminal )!.Dispose();
			}
			finally {
				IoMethods.DeleteFolder( resultDirectory );
			}
		}
		catch( Exception e ) {
			throw new UserCorrectableException( "Workstation configuration failed: " + e.Message, e );
		}
	}
}