using UnityEngine.UI;

namespace GracesGames.SimpleFileBrowser.Scripts.UI {

	public class PortraitUserInterface : UserInterface {

		protected override void SetupParents() {
			// Find directories parent to group directory buttons
			DirectoriesParent = GracesGames.Common.Scripts.Utilities.FindGameObjectOrError("Items");
			// Find files parent to group file buttons
			FilesParent = GracesGames.Common.Scripts.Utilities.FindGameObjectOrError("Items");
			// Set the button height
			SetButtonParentHeight(DirectoriesParent, ItemButtonHeight);
			SetButtonParentHeight(FilesParent, ItemButtonHeight);
			// Set the panel color
			GracesGames.Common.Scripts.Utilities.FindGameObjectOrError("ItemPanel").GetComponent<Image>().color = DirectoryPanelColor;
		}
	}
}
