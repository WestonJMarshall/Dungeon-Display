using UnityEngine;
using System.IO;
using System.Diagnostics;

public class FileExplorerOpenMainMenu : MonoBehaviour
{
    public void OpenAssociatedFolder()
    {
        if (Directory.Exists(@"GameLoadedAssets\Maps\"))
        {
            ProcessStartInfo sf = new ProcessStartInfo(@"GameLoadedAssets\Maps\");
            Process.Start(sf);
        }
    }
}
