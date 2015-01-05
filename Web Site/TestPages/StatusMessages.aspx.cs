using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.WebSessionState;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class StatusMessages: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis(
				EwfTable.CreateWithItems(
					items:
						getStatusTests()
							.Select( tests => new EwfTableItem( tests.Item1, new PostBackButton( tests.Item2, new ButtonActionControlStyle( "Test" ), usesSubmitBehavior: false ) ) )
							.ToFunctions() ) );
		}

		private IEnumerable<Tuple<string, ActionPostBack>> getStatusTests() {
			yield return
				Tuple.Create(
					"One info message",
					PostBack.CreateFull( id: "oneInfo", firstModificationMethod: () => AddStatusMessage( StatusMessageType.Info, "This is one info message." ) ) );

			yield return
				Tuple.Create(
					"One warning message",
					PostBack.CreateFull( id: "oneWarning", firstModificationMethod: () => { AddStatusMessage( StatusMessageType.Warning, "This is the warning message" ); } ) )
				;

			yield return
				Tuple.Create(
					"Validation error message",
					PostBack.CreateFull(
						id: "valError",
						firstTopValidationMethod: ( dictionary, validator ) => validator.NoteErrorAndAddMessage( "This is the validation error" ) ) );

			yield return
				Tuple.Create(
					"EwfException error message",
					PostBack.CreateFull( id: "exception", firstModificationMethod: () => new DataModificationException( "This is the EwfException." ) ) );

			yield return
				Tuple.Create( "Long-running operation: 2 seconds.", PostBack.CreateFull( id: "longRunning", firstModificationMethod: () => Thread.Sleep( 2000 ) ) );

			yield return
				Tuple.Create(
					"Very Long-running operation: 15 seconds.",
					PostBack.CreateFull( id: "veryLongRunning", firstModificationMethod: () => Thread.Sleep( 15000 ) ) );

			yield return
				Tuple.Create(
					"Five validation error messages",
					PostBack.CreateFull(
						id: "fiveValErrors",
						firstTopValidationMethod: ( dictionary, validator ) => {
							foreach( var i in Enumerable.Range( 0, 5 ) )
								validator.NoteErrorAndAddMessage( "This message is {0} in line.".FormatWith( i ) );
						} ) );

			yield return Tuple.Create(
				"Two info messages",
				PostBack.CreateFull(
					id: "twoInfos",
					firstModificationMethod: () => {
						AddStatusMessage( StatusMessageType.Info, "This is one info message." );
						AddStatusMessage( StatusMessageType.Info, "This is the second message" );
					} ) );

			yield return Tuple.Create(
				"One info message, one warning message",
				PostBack.CreateFull(
					id: "oneInfoOneWarning",
					firstModificationMethod: () => {
						AddStatusMessage( StatusMessageType.Info, "This is the info message." );
						AddStatusMessage( StatusMessageType.Warning, "This is the warning message" );
					} ) );

			yield return
				Tuple.Create(
					"Very long info message",
					PostBack.CreateFull( id: "veryLongInfo", firstModificationMethod: () => { AddStatusMessage( StatusMessageType.Info, longMessage() ); } ) );

			yield return
				Tuple.Create(
					"Very long warning message",
					PostBack.CreateFull( id: "veryLongWarning", firstModificationMethod: () => { AddStatusMessage( StatusMessageType.Warning, longMessage() ); } ) );

			yield return Tuple.Create(
				"Many info messages",
				PostBack.CreateFull(
					id: "manyInfos",
					firstModificationMethod: () => {
						foreach( var i in Enumerable.Range( 0, 8 ) )
							AddStatusMessage( StatusMessageType.Info, "This message is {0} in line.".FormatWith( i ) );
					} ) );

			yield return Tuple.Create(
				"Too many info messages",
				PostBack.CreateFull(
					id: "twoManyInfos",
					firstModificationMethod: () => {
						foreach( var i in Enumerable.Range( 0, 100 ) )
							AddStatusMessage( StatusMessageType.Info, "This message is {0} in line.".FormatWith( i ) );
					} ) );

			yield return
				Tuple.Create(
					"Info, warning, validation error messages.",
					PostBack.CreateFull(
						id: "infoWarningValError",
						firstTopValidationMethod: ( dictionary, validator ) => validator.NoteErrorAndAddMessage( "This is the validation error" ),
						firstModificationMethod: () => {
							AddStatusMessage( StatusMessageType.Info, "This is the info message." );
							AddStatusMessage( StatusMessageType.Warning, "This is the warning message" );
						} ) );
		}

		private string longMessage() {
			return "Cupcake cotton candy tootsie roll chocolate candy chupa chups oat cake. Muffin candy cotton candy chocolate apple pie. " +
			       "Chocolate bear claw biscuit fruitcake marzipan macaroon dragée bonbon. Sweet danish gummi bears topping jelly beans dessert topping unerdwear.com." +
			       " Pastry pastry brownie. Donut chocolate chupa chups danish toffee toffee gingerbread liquorice tiramisu. Cupcake toffee powder cheesecake marzipan. " +
			       "Muffin carrot cake chocolate cake applicake cupcake. Biscuit gingerbread powder sweet roll. Gummies cookie gummies jujubes. Pastry chocolate candy canes " +
			       "liquorice. Candy sesame snaps cheesecake topping jujubes macaroon liquorice.";
		}
	}
}