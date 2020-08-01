using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class StatusMessages: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis(
				EwfTable.Create()
					.AddData(
						getStatusTests(),
						tests => new EwfTableItem(
							tests.Item1.ToCell(),
							new EwfButton( new StandardButtonStyle( "Test" ), behavior: new PostBackBehavior( postBack: tests.Item2 ) ).ToCell() ) )
					.ToCollection()
					.GetControls() );
		}

		private IEnumerable<Tuple<string, ActionPostBack>> getStatusTests() {
			yield return Tuple.Create(
				"One info message",
				PostBack.CreateFull( id: "oneInfo", firstModificationMethod: () => AddStatusMessage( StatusMessageType.Info, "This is one info message." ) ) );

			yield return Tuple.Create(
				"One warning message",
				PostBack.CreateFull(
					id: "oneWarning",
					firstModificationMethod: () => { AddStatusMessage( StatusMessageType.Warning, "This is the warning message" ); } ) );

			yield return Tuple.Create(
				"Modification error message",
				PostBack.CreateFull( id: "valError", firstModificationMethod: () => throw new ApplicationException( "This is the validation error" ) ) );

			yield return Tuple.Create(
				"EwfException error message",
				PostBack.CreateFull( id: "exception", firstModificationMethod: () => throw new DataModificationException( "This is the EwfException." ) ) );

			yield return Tuple.Create(
				"Long-running operation: 2 seconds.",
				PostBack.CreateFull( id: "longRunning", firstModificationMethod: () => Thread.Sleep( 2000 ) ) );

			yield return Tuple.Create(
				"Very Long-running operation: 15 seconds.",
				PostBack.CreateFull( id: "veryLongRunning", firstModificationMethod: () => Thread.Sleep( 15000 ) ) );

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

			yield return Tuple.Create(
				"Several info messages, Several warning messages",
				PostBack.CreateFull(
					id: "10",
					firstModificationMethod: () => {
						AddStatusMessage( StatusMessageType.Info, "This is the info message." );
						AddStatusMessage( StatusMessageType.Info, "This is the second info message." );
						AddStatusMessage( StatusMessageType.Info, "This is the third info message." );

						AddStatusMessage( StatusMessageType.Warning, "This is the warning message" );
						AddStatusMessage( StatusMessageType.Warning, "This is second warning message" );
						AddStatusMessage( StatusMessageType.Warning, "This is third warning message" );
					} ) );

			yield return Tuple.Create(
				"Very long info message",
				PostBack.CreateFull( id: "veryLongInfo", firstModificationMethod: () => { AddStatusMessage( StatusMessageType.Info, longMessage() ); } ) );

			yield return Tuple.Create(
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