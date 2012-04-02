using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using Selenium;

namespace RedStapler.StandardLibrary.WebTestingFramework {
	/// <summary>
	/// Used to run web tests against a system.
	/// </summary>
	public class WebTester {
		/// <summary>
		/// Installs testing software if necessary, initializes the test browser and executes all tests for the system.
		/// Returns a non-zero code if a failure was encountered when running a test. It is possible to have a zero return code when there is a failure if the
		/// failure occurs before the tests begin to run, for example.
		/// </summary>
		public static int RunAllWebTestsForSystem() {
			WebTester webTester = null;
			try {
				OneTimeInstall.InstallSeleniumServiceIfNecessary();

				// NOTE: Moving selenium initialization here instead of setupBrowser will simplify things and make it so we don't have to create the WebTester reference too early.
				// We also won't need to hold a selenium member variable.

				Console.WriteLine( "Executing web tests..." );
				webTester = new WebTester();

				// Only do this if the intermediate log on didn't fail.
				if( Environment.ExitCode == 0 ) {
					foreach( var testClass in
						Assembly.GetCallingAssembly().GetTypes().Where( t => t.GetCustomAttributes( typeof( TestFixtureAttribute ), true ).Any() ).OrderBy( tc => tc.Name ) )
						webTester.executeTest( testClass );
					Console.WriteLine( "Web tests complete." );
				}
			}
			catch( Exception e ) {
				// NOTE: After we eliminate environment.exit code setting, try to wrap this method in standard exception handling instead.
				AppTools.EmailAndLogError( e );
			}
			finally {
				if( webTester != null )
					webTester.teardown();
			}

			return Environment.ExitCode;
		}

		private ISelenium selenium;

		private WebTester() {
			setupBrowser();
		}

		// NOTE: We probably don't need the SetUp and TearDown and attributes because we don't use nUnit to actually execute them.
		[ SetUp ]
		private void setupBrowser() {
			// NOTE: These base URLs only work for systems who happen to have the same integration and localhost suffix (For example, localhost/RlePersonnel and integration.redstapler.biz/RlePersonnel).
			// Systems like MITCalendar will not work until we come up with a better solution.
			var baseUrl = "https://integration.redstapler.biz/";
			if( AppTools.IsDevelopmentInstallation ) {
				if( AppTools.InstallationConfiguration.WebApplications.Any( wa => wa.SupportsSecureConnections ) )
					baseUrl = "https://localhost/";
				else
					baseUrl = "http://localhost/";
			}

			selenium = new DefaultSelenium( "localhost" /*location of Selenium server*/, 4444, @"*firefox3 C:\Program Files (x86)\Mozilla Firefox\firefox.exe", baseUrl );
			selenium.Start();


			if( AppTools.IsIntermediateInstallation ) {
				executeSeleniumBlock( "Intermediate log on",
				                      delegate {
				                      	// NOTE: We need to go to the specific URL here instead of relying on a redirect, or Selenium will time out or otherwise fail (it sucks at following redirects).
				                      	selenium.Open( "/" + AppTools.InstallationConfiguration.SystemShortName + "/Ewf/IntermediateLogIn.aspx?ReturnUrl=" );
				                      	// NOTE: Does not work for MIT Calendar, etc.
				                      	selenium.Type( "ctl00_ctl00_main_contentPlace_password_theTextBox", AppTools.SystemProvider.IntermediateLogInPassword );
				                      	// NOTE: Move g8Summit to machine configuration file.
				                      	SubmitForm( selenium );
				                      	selenium.WaitForPageToLoad( "30000" );
				                      } );
			}
			if( UserManagementStatics.UserManagementEnabled && UserManagementStatics.SystemProvider is FormsAuthCapableUserManagementProvider ) {
				executeSeleniumBlock( "Forms log on",
				                      delegate {
				                      	// NOTE: System-name approach suffers from same problem as above.
				                      	selenium.Open( "/" + AppTools.InstallationConfiguration.SystemShortName + "/Ewf/UserManagement/LogIn.aspx?ReturnUrl=" );

				                      	// NOTE: I don't think we need waits after opens.
				                      	selenium.WaitForPageToLoad( "30000" );
				                      	Assert.IsTrue( selenium.GetTitle().EndsWith( "Log In" ) );
				                      	// NOTE: For RSIS, we need the ability to pass a different email address and a different password for testing.
				                      	selenium.Type( "ctl00_ctl00_main_contentPlace_emailAddress_theTextBox", AppTools.SystemProvider.FormsLogInEmail );
				                      	selenium.Type( "ctl00_ctl00_main_contentPlace_password_theTextBox", AppTools.SystemProvider.FormsLogInPassword );
				                      	SubmitForm( selenium );
				                      	selenium.WaitForPageToLoad( "30000" );
				                      } );
			}
		}

		/// <summary>
		/// Submits the form. Equivalent to hitting the ENTER key.
		/// </summary>
		public static void SubmitForm( ISelenium selenium ) {
			selenium.Submit( "aspnetForm" );
		}

		/// <summary>
		/// This method runs the test, handles any errors and returns true if there is an error.
		/// </summary>
		private void executeTest( Type testClass ) {
			executeSeleniumBlock( testClass.Name,
			                      delegate {
			                      	try {
			                      		testClass.GetMethods().Where( m => m.GetCustomAttributes( typeof( TestAttribute ), true ).Length > 0 ).Single().Invoke( null,
			                      		                                                                                                                        new object[]
			                      		                                                                                                                        	{ selenium } );
			                      	}
			                      	catch( TargetInvocationException e ) {
			                      		throw e.InnerException;
			                      	}
			                      } );
		}

		/// <summary>
		/// This method runs the test, handles any errors and returns true if there is an error.
		/// </summary>
		private void executeSeleniumBlock( string name, Action method ) {
			try {
				method();
				Console.WriteLine( name + ": Passed" );
			}
			catch( Exception e ) {
				// NOTE: We probably don't need to set the exit code. We probably just need to return a non-zero code in the main method.
				Environment.ExitCode = 1;
				Console.Error.WriteLine( name + ": FAILED" );
				if( e is AssertionException || e is SeleniumException )
					handleKnownTestException( e );
				else {
					Console.Error.WriteLine( "Unexpected type of exception: " + e.GetType().Name );
					throw;
				}
			}
		}

		private void handleKnownTestException( Exception e ) {
			Console.Error.Write( "Details: " );
			if( e.Message.Length > 0 ) {
				Console.Error.WriteLine( e.Message );
				if( Regex.IsMatch( e.Message, "ERROR: Element .* not found" ) ) {
					Console.Error.WriteLine( "Location:" + selenium.GetLocation() );
					Console.Error.WriteLine( "Page title: " + selenium.GetTitle() );
					Console.Error.WriteLine( "Fields are:" );
					foreach( var field in selenium.GetAllFields() )
						Console.Error.WriteLine( field );
				}
			}
			else if( e is AssertionException )
				Console.Error.WriteLine( "Assertion failed. No message provided." );
			else {
				Console.Error.WriteLine(
					"No useful exception message. Try to improve exception handling in WebTester once underlying problem is discovered. Main exception ToString: " + e );
			}
		}

		/// <summary>
		/// This method stops the selenium browser and disposes the selenium server process.
		/// </summary>
		[ TearDown ]
		private void teardown() {
			if( selenium != null )
				selenium.Stop();
		}
	}
}
