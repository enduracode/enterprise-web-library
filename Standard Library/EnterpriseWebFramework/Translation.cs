namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Framework use only.
	/// </summary>
	public static class Translation {
		internal static string DownloadExisting { get { return getCorrectTranslation( "Download existing", "Descargar archivo existente" ); } }
		internal static string NoExistingFile { get { return getCorrectTranslation( "No existing file", "No existe el archivo" ); } }

		/// <summary>
		/// Intended to be used as PleaseUploadAFile + " 'File name'".
		/// </summary>
		internal static string PleaseUploadAFile { get { return getCorrectTranslation( "Please upload a file:", "Por favor, cargue un archivo:" ); } }

		/// <summary>
		/// "Unacceptable file extension. Acceptable file extensions are:".
		/// </summary>
		internal static string UnacceptableFileExtension { get { return getCorrectTranslation( "Unacceptable file extension. Acceptable file extensions are:", "Extensión de archivo no válida. La extensión válida es:" ); } }

		internal static string ApplicationHasBeenUpdatedAndWeCouldNotInterpretAction {
			get {
				return
					getCorrectTranslation(
						"This application has been updated since the last time you saw this page and we couldn't interpret your last action. The latest version of the page has been loaded.",
						"Esta aplicación ha sido actualizado desde la última vez que vio esta página y que no podía interpretar la última acción. La última versión de la página se ha cargado." );
			}
		}

		internal static string AnotherUserHasModifiedPageBad {
			get {
				return getCorrectTranslation( "Another user has modified this page since the last time you saw it. The latest version has been loaded.",
				                              "Otro usuario ha modificado esta página desde la última vez que usted lo vio. La última versión se ha cargado." );
			}
		}

		internal static string AnotherUserHasModifiedPageGoodHtml {
			get {
				return
					getCorrectTranslation( "Another user has modified this page since the last time you saw it.",
					                       "Otro usuario ha modificado esta página desde la última vez que usted lo vio." ) + " " +
					getCorrectTranslation(
						"Please either " + NetTools.BuildBasicLink( "load the latest version", EwfPage.Instance.InfoAsBaseType.GetUrl(), false ) +
						" or repeat your last action to save this version.",
						"Por favor, " + NetTools.BuildBasicLink( "carga la última versión", EwfPage.Instance.InfoAsBaseType.GetUrl(), false ) +
						" o repita la última acción para guardar a esta versión." ) + " " +
					getCorrectTranslation(
						"You can also " + NetTools.BuildBasicLink( "load the latest version in another window", EwfPage.Instance.InfoAsBaseType.GetUrl(), true ) + ".",
						"También puede " + NetTools.BuildBasicLink( "cargar la versión más reciente en otra ventana", EwfPage.Instance.InfoAsBaseType.GetUrl(), true ) + "." );
			}
		}

		internal static string AnotherUserHasModifiedPageAndWeCouldNotInterpretAction {
			get {
				return
					getCorrectTranslation( "Another user has modified this page since the last time you saw it and we couldn't interpret your last action. Please repeat it.",
					                       "Otro usuario ha modificado esta página desde la última vez que lo vio y no pudo interpretar su última acción. Por favor, repita la misma." );
			}
		}

		internal static string PleaseCorrectTheErrorsShownBelow { get { return getCorrectTranslation( "Please correct the errors shown below.", "Por favor, corrija los errores que se muestran a continuación." ); } }

		internal static string YourBrowserHasCookiesDisabled {
			get {
				return getCorrectTranslation( "Your browser has cookies disabled. Cookies must be enabled in order for you to successfully log on.",
				                              "Su navegador tiene deshabilitadas las cookies. Las cookies deben estar habilitadas para que usted pueda iniciar la sesión." );
			}
		}

		/// <summary>
		/// "Your computer's clock is significantly off. This may prevent you from logging in or cause you to be logged out prematurely. The correct time is"
		/// </summary>
		internal static string YourClockIsWrong {
			get {
				return
					getCorrectTranslation(
						"Your computer's clock is significantly off. This may prevent you from logging in or cause you to be logged out prematurely. The correct time is",
						"El reloj de su ordenador está muy retrasada. Esto puede impedir que se puedan conectar o hacer que se cierre la sesión antes de tiempo. La hora exacta es" );
			}
		}

		internal static string ClickHereToReplaceExistingFile { get { return getCorrectTranslation( "Click here to replace the existing file.", "Chasque aquí para substituir el archivo existente." ); } }

		/// <summary>
		/// BasicPage.master use only.
		/// </summary>
		public static string Processing { get { return getCorrectTranslation( "Processing...", "Procesando..." ); } }

		/// <summary>
		/// BasicPage.master use only.
		/// </summary>
		public static string ThisSeemsToBeTakingAWhile {
			get {
				return getCorrectTranslation( "This seems to be taking a while. You may continue waiting or click here to cancel the request.",
				                              "Esta operación parece estar durando bastante tiempo. Usted puede seguir esperando o haga clic aquí para cancelar la solicitud." );
			}
		}

		/// <summary>
		/// AccessDenied.aspx use only.
		/// </summary>
		public static string AccessIsDenied { get { return getCorrectTranslation( "Access is denied.", "Acceso denegado." ); } }

		/// <summary>
		/// AccessDenied.aspx use only.
		/// </summary>
		public static string ClickHereToGoToHomePage { get { return getCorrectTranslation( "Click here to go to the home page.", "Haga clic aquí para ir a la página principal." ); } }

		/// <summary>
		/// PageNotAvailable.aspx use only.
		/// </summary>
		public static string ThePageYouRequestedIsNotAvailable { get { return getCorrectTranslation( "The page you requested is no longer available.", "La página solicitada ya no está disponible." ); } }

		/// <summary>
		/// PageNotAvailable.aspx use only.
		/// </summary>
		public static string YouWillBeSentToTheHomePage {
			get {
				return getCorrectTranslation( "You will be sent to the home page of the site in five seconds.",
				                              "Se le enviará a la página principal del sitio web en cinco segundos." );
			}
		}

		/// <summary>
		/// UnhandledException.aspx use only.
		/// </summary>
		public static string AnErrorHasOccurred { get { return getCorrectTranslation( "An error has occurred in the system.", "Se ha producido un error en el sistema." ); } }

		/// <summary>
		/// PageDisabled.aspx use only.
		/// </summary>
		public static string ThePageYouRequestedIsDisabled { get { return getCorrectTranslation( "The page you requested is disabled.", "La página que usted pidió es lisiada." ); } }

		/// <summary>
		/// Key must be a string constant defined in this class.
		/// </summary>
		private static string getCorrectTranslation( string english, string spanish ) {
			return EwfApp.Instance.UseSpanishLanguage ? spanish : english;
		}
	}
}