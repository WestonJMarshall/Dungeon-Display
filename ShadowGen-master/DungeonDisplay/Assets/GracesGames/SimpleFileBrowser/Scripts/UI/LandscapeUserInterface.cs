using UnityEngine.UI;

using GracesGames.Common.Scripts;
using UnityEngine;

namespace GracesGames.SimpleFileBrowser.Scripts.UI {

    public class LandscapeUserInterface : UserInterface {

        protected override void SetupParents() {
            // Find directories parent to group directory buttons
            DirectoriesParent = GracesGames.Common.Scripts.Utilities.FindGameObjectOrError("Directories");
            // Find files parent to group file buttons
            FilesParent = GracesGames.Common.Scripts.Utilities.FindGameObjectOrError("Files");
            // Set the button height
            SetButtonParentHeight(DirectoriesParent, ItemButtonHeight);
            SetButtonParentHeight(FilesParent, ItemButtonHeight);
            // Set the panel color
            GracesGames.Common.Scripts.Utilities.FindGameObjectOrError("DirectoryPanel").GetComponent<Image>().color = DirectoryPanelColor;
            GracesGames.Common.Scripts.Utilities.FindGameObjectOrError("FilePanel").GetComponent<Image>().color = FilePanelColor;
        }
    }
}

