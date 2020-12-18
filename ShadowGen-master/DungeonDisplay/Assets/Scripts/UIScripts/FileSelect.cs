using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;
using GracesGames.SimpleFileBrowser.Scripts;
using System.Threading.Tasks;

public class FileSelect : MonoBehaviour
{
    private string[] fileEnding;
    private Canvas exitButton;

    [SerializeField]
    private Dropdown typeDropDown = null;
    [SerializeField]
    private InputField pathText = null;
    [SerializeField]
    private GameObject fileBrowserPrefab = null;

    [Space]
    [Header("Panels")]
    [SerializeField]
    private GameObject fileSelectPanel = null;
    [SerializeField]
    private GameObject keySelectPanel = null;
    [SerializeField]
    private GameObject MapSizePanel = null;

    [Space]
    [Header("Warning Panel")]
    [SerializeField]
    private GameObject warningPanel = null;
    [SerializeField]
    private Text warningText = null;
    [SerializeField]
    private Button[] buttons = null;

    public void Awake()
    {
        //Setup dungeon file struct
        DungeonFile.path = "C:\\";
        DungeonFile.fileType = (FileType)typeDropDown.value;
        fileEnding = new string[1];
        fileEnding[0] = "txt";
        DungeonFile.fromSaveFile = false;
        DungeonFile.pixelKey = new List<Color>();
        DungeonFile.donjonKey = new List<string>();
    }

    /// <summary>
    /// Selects a file based on the input fileType
    /// </summary>
    public void SelectFile()
    {
        //Instantiate File Browser
        FileBrowser fileBrowser = Instantiate(fileBrowserPrefab, transform).GetComponent<FileBrowser>();

        //Setup file browser
        if (Directory.Exists(@"GameLoadedAssets\Maps\"))
        {
            if (DungeonFile.fileType == FileType.TSV && Directory.Exists(@"GameLoadedAssets\Maps\TSVMaps\"))
            {
                fileBrowser.SetupFileBrowser(ViewMode.Landscape, @"GameLoadedAssets\Maps\TSVMaps\");
            }
            else if (DungeonFile.fileType == FileType.PixelMap && Directory.Exists(@"GameLoadedAssets\Maps\PixelMaps\"))
            {
                fileBrowser.SetupFileBrowser(ViewMode.Landscape, @"GameLoadedAssets\Maps\PixelMaps\");
            }
            else if (DungeonFile.fileType == FileType.SavedMap && Directory.Exists(@"GameLoadedAssets\Maps\SavedMaps\"))
            {
                fileBrowser.SetupFileBrowser(ViewMode.Landscape, @"GameLoadedAssets\Maps\SavedMaps\");
            }
            else if (DungeonFile.fileType == FileType.FreeFormMap && Directory.Exists(@"GameLoadedAssets\Maps\FreeFormMaps\"))
            {
                fileBrowser.SetupFileBrowser(ViewMode.Landscape, @"GameLoadedAssets\Maps\FreeFormMaps\");
            }
            else
            {
                fileBrowser.SetupFileBrowser(ViewMode.Landscape, @"GameLoadedAssets\Maps\");
            }
        }
        else
        {
            fileBrowser.SetupFileBrowser(ViewMode.Landscape);
        }
        fileBrowser.OnFileSelect += SetPath;
        fileBrowser.OpenFilePanel(fileEnding);
    }

    public void DropdownChanged()
    {
        DungeonFile.fileType = (FileType)typeDropDown.value;

        switch (DungeonFile.fileType)
        {
            case FileType.TSV:
                fileEnding = new string[1];
                fileEnding[0] = "txt";
                DungeonFile.fromSaveFile = false;
                break;
            case FileType.PixelMap:
                fileEnding = new string[1];
                fileEnding[0] = "png";
                DungeonFile.fromSaveFile = false;
                break;
            case FileType.SavedMap:
                fileEnding = new string[1];
                fileEnding[0] = "txt";
                DungeonFile.fromSaveFile = true;
                break;
            case FileType.FreeFormMap:
                fileEnding = new string[1];
                fileEnding[0] = "png";
                DungeonFile.fromSaveFile = false;
                break;
        }
    }

    /// <summary>
    /// Makes sure all input data is valid
    /// </summary>
    public void ValidateSelection()
    {
        //TODO: Make sure file path is valid and file is of the correct type
        if(DungeonFile.path.Length <= 4)
        {
            ShowWarning("File path too short");
            return;
        }

        string ending = DungeonFile.path.Substring(DungeonFile.path.Length - 3);

        if (!DungeonFile.fromSaveFile)
        {
            switch (DungeonFile.fileType)
            {
                case FileType.TSV:
                    if (ending != "txt")
                    {
                        ShowWarning("Incorrect file type txt expected.");
                        return;
                    }
                    if (File.ReadAllLines(DungeonFile.path).Length > 1)
                    {
                        string fLine = File.ReadAllLines(DungeonFile.path)[0];
                        if (fLine.Substring(fLine.Length - 3) == "txt")
                        {
                            ShowWarning("The Selected File is a Save File, Not a TSV File");
                            return;
                        }
                    }
                    else
                    {
                        ShowWarning("Invalid TSV File");
                        return;
                    }
                    if (exitButton != null)
                    {
                        exitButton.sortingOrder = -20;
                    }
                    else
                    {
                        exitButton = GameObject.Find("ExitButtonCanvas").GetComponent<Canvas>();
                        exitButton.sortingOrder = -20;
                    }
                    LoadDungeon();
                    break;
                case FileType.PixelMap:
                    if (ending != "png")
                    {
                        ShowWarning("Incorrect file type png expected.");
                        return;
                    }
                    if (exitButton != null)
                    {
                        exitButton.sortingOrder = -20;
                    }
                    else
                    {
                        exitButton = GameObject.Find("ExitButtonCanvas").GetComponent<Canvas>();
                        exitButton.sortingOrder = -20;
                    }
                    GetComponentInParent<Canvas>().sortingOrder = 10;
                    fileSelectPanel.SetActive(false);
                    keySelectPanel.SetActive(true);
                    ValidateColors();
                    break;
                case FileType.FreeFormMap:
                    if (ending != "png")
                    {
                        ShowWarning("Incorrect file type png expected.");
                        return;
                    }
                    if (exitButton != null)
                    {
                        exitButton.sortingOrder = -20;
                    }
                    else
                    {
                        exitButton = GameObject.Find("ExitButtonCanvas").GetComponent<Canvas>();
                        exitButton.sortingOrder = -20;
                    }
                    GetComponentInParent<Canvas>().sortingOrder = 10;
                    fileSelectPanel.SetActive(false);
                    MapSizePanel.SetActive(true);
                    break;
            }
        }
        else
        {
            if (ending != "txt")
            {
                ShowWarning("Incorrect file type txt expected.");
                return;
            }
            if (File.ReadAllLines(DungeonFile.path).Length > 1)
            {
                string fLine = File.ReadAllLines(DungeonFile.path)[0];
                if (fLine.Substring(fLine.Length - 3) != "txt" && fLine.Substring(fLine.Length - 3) != "png")
                {
                    ShowWarning("Invalid Save File");
                    return;
                }
            }
            else
            {
                ShowWarning("Invalid Save File");
                return;
            }
            if (exitButton != null)
            {
                exitButton.sortingOrder = -20;
            }
            else
            {
                exitButton = GameObject.Find("ExitButtonCanvas").GetComponent<Canvas>();
                exitButton.sortingOrder = -20;
            }
            LoadDungeon();
        }
    }

    /// <summary>
    /// Shows the warning panel with the given warning
    /// </summary>
    /// <param name="warning">The message to display</param>
    public void ShowWarning(string warning)
    {
        warningText.text = warning;
        warningPanel.SetActive(true);
    }

    /// <summary>
    /// Hides the warning panel
    /// </summary>
    public void HideWarning()
    {
        warningPanel.SetActive(false);
    }

    /// <summary>
    /// Selects a color with the color picker and applies it to the button
    /// </summary>
    /// <param name="button">The button that is selecting</param>
    public void SelectColor(Button button)
    {
        ColorBlock block = button.colors;
        block.normalColor = Color.black;
        button.colors = block;
    }

    public void ValidateColors()
    {
        //Check for duplicate colors
        for(int i = 0; i < buttons.Length - 1; i++)
        {
            for(int j = i + 1; j < buttons.Length; j++)
            {
                if(CompareColors(buttons[i].colors.normalColor, buttons[j].colors.normalColor))
                {
                    ShowWarning("Duplicate colors detected, each key needs to be a different color.");
                    return;
                }
            }
        }

        DungeonFile.pixelKey.Clear();

        for(int i = 0; i < buttons.Length; i++)
        {
            DungeonFile.pixelKey.Add(buttons[i].colors.normalColor);
        }

        keySelectPanel.SetActive(false);
        LoadDungeon();
    }

    /// <summary>
    /// Returns true if the two colors are the same
    /// </summary>
    /// <param name="c1">The first color to compare</param>
    /// <param name="c2">The second color to compare</param>
    /// <returns></returns>
    public bool CompareColors(Color c1, Color c2)
    {
        return (c1.r == c2.r && c1.g == c2.g && c1.b == c2.b && c1.a == c2.a);
    }

    /// <summary>
    /// Pass the file and key into the dungeon scene
    /// </summary>
    public void LoadDungeon()
    {
        InfoLogCanvasScript.SendInfoMessage("Loading Dungeon...", UnityEngine.Color.black, 10);
        SceneManager.LoadScene(1);
    }

    #region File Selection

    /// <summary>
    /// Returns the path to a file that the user selects
    /// </summary>
    /// <param name="acceptedEnding">The accepted file endings of the selected file</param>
    /// <returns>The path to the file</returns>
    void SetPath(string path)
    {
        DungeonFile.path = path;
        pathText.text = DungeonFile.path;
        InfoLogCanvasScript.SendInfoMessage("Path Set: " + path, UnityEngine.Color.black, 10);
    }

    #endregion
}
