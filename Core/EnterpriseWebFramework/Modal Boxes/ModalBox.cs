using System.Collections.Generic;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A modal box.
	/// </summary>
	public class ModalBox: EtherealComponent {
		internal class CssElementCreator: ControlCssElementCreator {
			internal static readonly ElementClass Class = new ElementClass( "ewfModal" );

			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return new[]
					{
						new CssElement( "ModalBox", "dialog.{0}".FormatWith( Class.ClassName ) ),
						new CssElement( "ModalBoxBackdrop", "dialog.{0}::backdrop".FormatWith( Class.ClassName ), "dialog.{0} + .backdrop".FormatWith( Class.ClassName ) )
					};
			}
		}

		private readonly IReadOnlyCollection<EtherealComponent> children;

		/// <summary>
		/// Creates a modal box.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="content"></param>
		/// <param name="open"></param>
		public ModalBox( ModalBoxId id, IEnumerable<FlowComponent> content, bool open = false ) {
			children = new ElementComponent(
				context => {
					id.ElementId.AddId( context.Id );
					return
						new ElementData(
							() =>
							new ElementLocalData(
								"dialog",
								includeIdAttribute: true,
								jsInitStatements: open ? "document.getElementById( '{0}' ).showModal();".FormatWith( context.Id ) : "" ),
							classes: CssElementCreator.Class,
							children: content );
				} ).ToCollection();
		}

		IEnumerable<EtherealComponentOrElement> EtherealComponent.GetChildren() {
			return children;
		}
	}
}