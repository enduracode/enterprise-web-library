using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.WebSessionState;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class StatusMessages: EwfPage {
		public partial class Info {
			protected override void init() {}
		}

		protected override void loadData() {
			ph.AddControlsReturnThis(
				EwfTable.CreateWithItems(
					items:
						getStatusTests()
						.Select(
							tests =>
							new EwfTableItem( tests.Item1.ToCell(),
							                  new PostBackButton( tests.Item2, null, new ButtonActionControlStyle( "Test" ), usesSubmitBehavior: false ).ToCell() ) )
						.ToFunctions() ) );
		}

		private static IEnumerable<Tuple<string, DataModification>> getStatusTests() {
			yield return
				Tuple.Create( "One info message",
				              new DataModification( firstModificationMethod: () => AddStatusMessage( StatusMessageType.Info, "This is one info message." ) ) );

			yield return
				Tuple.Create( "One warning message",
				              new DataModification( firstModificationMethod: () => { AddStatusMessage( StatusMessageType.Warning, "This is the warning message" ); } ) );

			yield return
				Tuple.Create( "Validation error message",
				              new DataModification(
					              firstTopValidationMethod: ( dictionary, validator ) => validator.NoteErrorAndAddMessage( "This is the validation error" ) ) );

			yield return
				Tuple.Create( "EwfException error message", new DataModification( firstModificationMethod: () => new EwfException( "This is the EwfException." ) ) );

			yield return Tuple.Create( "Long-running operation: 2 seconds.", new DataModification( firstModificationMethod: () => Thread.Sleep( 2000 ) ) );

			yield return Tuple.Create( "Very Long-running operation: 15 seconds.", new DataModification( firstModificationMethod: () => Thread.Sleep( 15000 ) ) );

			yield return Tuple.Create( "Five validation error messages",
			                           new DataModification( firstTopValidationMethod: ( dictionary, validator ) => {
				                           foreach( var i in Enumerable.Range( 0, 5 ) )
					                           validator.NoteErrorAndAddMessage( "This message is {0} in line.".FormatWith( i ) );
			                           } ) );

			yield return Tuple.Create( "Two info messages",
			                           new DataModification( firstModificationMethod: () => {
				                           AddStatusMessage( StatusMessageType.Info, "This is one info message." );
				                           AddStatusMessage( StatusMessageType.Info, "This is the second message" );
			                           } ) );

			yield return Tuple.Create( "One info message, one warning message",
			                           new DataModification( firstModificationMethod: () => {
				                           AddStatusMessage( StatusMessageType.Info, "This is the info message." );
				                           AddStatusMessage( StatusMessageType.Warning, "This is the warning message" );
			                           } ) );

			yield return Tuple.Create( "Several info messages, Several warning messages",
			                           new DataModification( firstModificationMethod: () => {
				                           AddStatusMessage( StatusMessageType.Info, "This is the info message." );
				                           AddStatusMessage( StatusMessageType.Info, "This is the second info message." );
				                           AddStatusMessage( StatusMessageType.Info, "This is the third info message." );

				                           AddStatusMessage( StatusMessageType.Warning, "This is the warning message" );
				                           AddStatusMessage( StatusMessageType.Warning, "This is second warning message" );
				                           AddStatusMessage( StatusMessageType.Warning, "This is third warning message" );
			                           } ) );


			yield return
				Tuple.Create( "Very long info message",
				              new DataModification( firstModificationMethod: () => { AddStatusMessage( StatusMessageType.Info, longMessage() ); } ) );

			yield return
				Tuple.Create( "Very long warning message",
				              new DataModification( firstModificationMethod: () => { AddStatusMessage( StatusMessageType.Warning, longMessage() ); } ) );

			yield return Tuple.Create( "Many info messages",
			                           new DataModification( firstModificationMethod: () => {
				                           foreach( var i in Enumerable.Range( 0, 8 ) )
					                           AddStatusMessage( StatusMessageType.Info, "This message is {0} in line.".FormatWith( i ) );
			                           } ) );

			yield return Tuple.Create( "Too many info messages",
			                           new DataModification( firstModificationMethod: () => {
				                           foreach( var i in Enumerable.Range( 0, 100 ) )
					                           AddStatusMessage( StatusMessageType.Info, "This message is {0} in line.".FormatWith( i ) );
			                           } ) );

			yield return
				Tuple.Create( "Info, warning, validation error messages.",
				              new DataModification(
					              firstTopValidationMethod: ( dictionary, validator ) => validator.NoteErrorAndAddMessage( "This is the validation error" ),
					              firstModificationMethod: () => {
						              AddStatusMessage( StatusMessageType.Info, "This is the info message." );
						              AddStatusMessage( StatusMessageType.Warning, "This is the warning message" );
					              } ) );
		}

		private static string longMessage() {
			return "Cupcake cotton candy tootsie roll chocolate candy chupa chups oat cake. Muffin candy cotton candy chocolate apple pie. " +
			       "Chocolate bear claw biscuit fruitcake marzipan macaroon dragée bonbon. Sweet danish gummi bears topping jelly beans dessert topping unerdwear.com." +
			       " Pastry pastry brownie. Donut chocolate chupa chups danish toffee toffee gingerbread liquorice tiramisu. Cupcake toffee powder cheesecake marzipan. " +
			       "Muffin carrot cake chocolate cake applicake cupcake. Biscuit gingerbread powder sweet roll. Gummies cookie gummies jujubes. Pastry chocolate candy canes " +
			       "liquorice. Candy sesame snaps cheesecake topping jujubes macaroon liquorice.";
		}
	}
}