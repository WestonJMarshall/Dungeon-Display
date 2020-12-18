using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Drawing;
using System.Diagnostics;

public class TileSetSelectCanvasScript : MonoBehaviour
{
    public int pageNumber = 1;
    public int maxPages = 1;
    public Text pageNumberText;
    public GameObject tileSetButtonPrefab;
    public GameObject selectionPanel;
    public GameObject hoveredButton;
    public List<string> tileSetPaths = new List<string>();

    public void InitializeTileSetCanvas()
    {
        Manager.Instance.inspector.CloseInspector();
        Manager.Instance.CloseLightMenu();
        Manager.Instance.CloseShadowMenu();

        //Remove previous buttons
        List<Button> lb = new List<Button>();
        lb.AddRange(selectionPanel.GetComponentsInChildren<Button>());
        List<GameObject> lg = new List<GameObject>();
        foreach (Button b in lb) { Destroy(b.gameObject); }

        //Find icons and setup list of icons
        RetrieveTileSetPaths();

        //Display Pages
        pageNumber = 1;
        maxPages = (tileSetPaths.Count / 4) + 1;
        if(tileSetPaths.Count % 4 == 0) { maxPages--; }
        pageNumberText.text = "Page: " + pageNumber + "/" + maxPages;

        //Create iconButtonPrefabs for as many icons that can fit on a page, assign them sprites and attatch their 'on click'

        for (int i = 0; i < 4 && i < tileSetPaths.Count; i++)
        {
            GameObject button = Instantiate(tileSetButtonPrefab, selectionPanel.transform);
            button.name = "Icon Selection Button";
            button.GetComponent<TileSetSelectionButton>().path = tileSetPaths[i];
            //Take only the ending of the string
            button.GetComponentInChildren<Text>().text = tileSetPaths[i].Substring(tileSetPaths[i].LastIndexOf('\\') + 1);
            button.GetComponent<Button>().onClick.AddListener(TileSetButtonClicked);
        }
    }

    public void IncrementPage()
    {
        if (pageNumber < maxPages)
        {
            //Remove previous buttons
            List<Button> lb = new List<Button>();
            lb.AddRange(selectionPanel.GetComponentsInChildren<Button>());
            List<GameObject> lg = new List<GameObject>();
            foreach (Button b in lb) { Destroy(b.gameObject); }

            pageNumber++;
            pageNumberText.text = "Page: " + pageNumber + "/" + maxPages;

            for (int i = (pageNumber * 4) - 4; i < (pageNumber * 4) && i < tileSetPaths.Count; i++)
            {
                GameObject button = Instantiate(tileSetButtonPrefab, selectionPanel.transform);
                button.name = "Icon Selection Button";
                button.GetComponent<TileSetSelectionButton>().path = tileSetPaths[i];
                //Take only the ending of the string
                button.GetComponentInChildren<Text>().text = tileSetPaths[i].Substring(tileSetPaths[i].LastIndexOf('\\') + 1);
                button.GetComponent<Button>().onClick.AddListener(TileSetButtonClicked);
            }
        }
    }

    public void DecrementPage()
    {
        if (pageNumber > 1)
        {
            //Remove previous buttons
            List<Button> lb = new List<Button>();
            lb.AddRange(selectionPanel.GetComponentsInChildren<Button>());
            List<GameObject> lg = new List<GameObject>();
            foreach (Button b in lb) { Destroy(b.gameObject); }

            pageNumber--;
            pageNumberText.text = "Page: " + pageNumber + "/" + maxPages;

            for (int i = (pageNumber * 4) - 4; i < (pageNumber * 4) && i < tileSetPaths.Count; i++)
            {
                GameObject button = Instantiate(tileSetButtonPrefab, selectionPanel.transform);
                button.name = "Icon Selection Button";
                button.GetComponent<TileSetSelectionButton>().path = tileSetPaths[i];
                //Take only the ending of the string
                button.GetComponentInChildren<Text>().text = tileSetPaths[i].Substring(tileSetPaths[i].LastIndexOf('\\') + 1);
                button.GetComponent<Button>().onClick.AddListener(TileSetButtonClicked);
            }
        }
    }

    public void RetrieveTileSetPaths()
    {
        tileSetPaths = new List<string>();

        if (!Directory.Exists(@"GameLoadedAssets\Sprites\TileSets\")) { return; }

        //Find all images in the designated folder
        List<string> filePaths = new List<string>();
        filePaths.AddRange(Directory.GetDirectories(@"GameLoadedAssets\Sprites\TileSets\"));

        tileSetPaths = filePaths;
    }

    public void TileSetButtonClicked()
    {
        //Replace every possible sprite
        if (!Directory.Exists(hoveredButton.GetComponent<TileSetSelectionButton>().path)) { return; }

        Manager.Instance.LoadTileSet(hoveredButton.GetComponent<TileSetSelectionButton>().path);
    }
    public void OpenAssociatedFolder()
    {
        if (Directory.Exists(@"GameLoadedAssets\Sprites\TileSets\"))
        {
            ProcessStartInfo sf = new ProcessStartInfo(@"GameLoadedAssets\Sprites\TileSets\");
            Process.Start(sf);
        }
    }
}
