using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Drawing;
using System.Diagnostics;
using System.Text;
using Image = UnityEngine.UI.Image;
using System.Linq;

public class IconSelectCanvasScript : MonoBehaviour
{
    public int pageNumber = 1;
    public int maxPages = 1;
    public int totalItems = 0;
    public Text pageNumberText;
    public GameObject iconButtonPrefab;
    public GameObject selectionPanel;
    public GameObject inputObject;
    public GameObject hoveredButton;
    public List<Texture2D> icons = new List<Texture2D>();
    public int iconCount = 0;
    public List<string> iconPaths = new List<string>();
    public int directoryCount = 0;
    public List<string> directoryPaths = new List<string>();
    public string currentPath = @"GameLoadedAssets\Sprites\Icons\";

    public Sprite directorySprite;
    public Sprite returnSprite;

    public GameObject lastUsedSpriteButton;
    public Texture2D lastUsedSpriteTexture;
    public string lastUsedSpritePath = "";

    private void Awake()
    {
        lastUsedSpriteTexture = Texture2D.whiteTexture;
    }

    public void InitializeIconCanvas()
    {
        if(inputObject.GetComponent<Tile>() != null)
        {
            currentPath = @"GameLoadedAssets\Sprites\Tiles\";
        }
        else
        {
            currentPath = @"GameLoadedAssets\Sprites\Icons\";
        }

        SetupIconCanvas();
    }

    public void SetupIconCanvas()
    {
        //Remove previous buttons
        iconPaths = new List<string>();

        List<Button> lb = new List<Button>();
        lb.AddRange(selectionPanel.GetComponentsInChildren<Button>());
        foreach (Button b in lb) { Destroy(b.gameObject); }

        //Display Pages
        pageNumber = 1;

        //Find icons and setup list of icons
        RetrievePageIcons();

        if (currentPath != @"GameLoadedAssets\Sprites\Tiles\" && currentPath != @"GameLoadedAssets\Sprites\Icons\")
        {
            totalItems = (iconCount + 1 + directoryCount);
            maxPages = ((iconCount + 1 + directoryCount) / 9) + 1;
            if ((iconCount + directoryCount + 1) % 9 == 0) { maxPages--; }
        }
        else
        {
            totalItems = (iconCount + directoryCount);
            maxPages = ((iconCount + directoryCount) / 9) + 1;
            if ((iconCount + directoryCount) % 9 == 0) { maxPages--; }
        }
        pageNumberText.text = "Page: " + pageNumber + "/" + maxPages;

        //Create iconButtonPrefabs for as many icons that can fit on a page, assign them sprites and attatch their 'on click'
        SetupPageVisuals();
    }

    public void IncrementPage()
    {
        if(pageNumber < maxPages)
        {
            //Remove previous buttons
            List<Button> lb = new List<Button>();
            lb.AddRange(selectionPanel.GetComponentsInChildren<Button>());
            foreach (Button b in lb) { Destroy(b.gameObject); }

            pageNumber++;
            pageNumberText.text = "Page: " + pageNumber + "/" + maxPages;

            SetupPageVisuals();
        }
    }

    public void DecrementPage()
    {
        if (pageNumber > 1)
        {
            //Remove previous buttons
            List<Button> lb = new List<Button>();
            lb.AddRange(selectionPanel.GetComponentsInChildren<Button>());
            foreach (Button b in lb) { Destroy(b.gameObject); }

            pageNumber--;
            pageNumberText.text = "Page: " + pageNumber + "/" + maxPages;

            SetupPageVisuals();
        }
    }

    public void SetupPageVisuals()
    {
        RetrievePageIcons();

        if (currentPath != @"GameLoadedAssets\Sprites\Tiles\" && currentPath != @"GameLoadedAssets\Sprites\Icons\" && pageNumber == 1)
        {
            //Add back button
            GameObject button = Instantiate(iconButtonPrefab, selectionPanel.transform);
            button.name = "Back Button";
            button.GetComponent<Image>().sprite = returnSprite;
            button.GetComponent<Button>().onClick.AddListener(ReturnToPreviousDirectory);
            button.GetComponentInChildren<Text>().text = "Back";
        }

        for(int itemNumber = ((pageNumber * 9) - 9); itemNumber < totalItems && itemNumber < (pageNumber * 9); itemNumber++)
        {
            if (itemNumber < directoryCount)
            {
                GameObject button = Instantiate(iconButtonPrefab, selectionPanel.transform);
                button.name = "Directory Button";
                button.GetComponent<Image>().sprite = directorySprite;
                button.GetComponent<Button>().onClick.AddListener(DirectoryButtonClicked);
                button.GetComponentInChildren<Text>().text = directoryPaths[itemNumber].Split(new char[1] { '\\' })[directoryPaths[itemNumber].Split(new char[1] { '\\' }).Length - 1];
                button.GetComponent<IconSelectionButton>().buttonFilePath = directoryPaths[itemNumber];
            }
            else break;
        }
        for(int i = 0; i < icons.Count; i++)
        {
            if (icons[i] == null) { continue; }
            GameObject button = Instantiate(iconButtonPrefab, selectionPanel.transform);
            button.name = "Icon Selection Button";
            button.GetComponent<Image>().sprite = Sprite.Create(icons[i], new Rect(Vector2.zero, new Vector2(icons[i].width, icons[i].height)), Vector2.zero);
            button.GetComponent<Button>().onClick.AddListener(SpriteButtonClicked);
            button.GetComponent<IconSelectionButton>().buttonFilePath = iconPaths[i];
        }
    }

    public void ReturnToPreviousDirectory()
    {
        string[] a = currentPath.Split(new char[1] { '\\' });
        List<string> anew = a.ToList();
        if (a[a.Length - 1] == "")
        {
            anew.RemoveAt(a.Length - 1);
        }
        currentPath = "";
        for(int i = 0; i < anew.Count - 1; i++)
        {
            currentPath += anew[i] + "\\";
        }
        SetupIconCanvas();
    }

    public void RetrievePageIcons()
    {
        icons = new List<Texture2D>();

        directoryPaths = new List<string>();
        directoryPaths.AddRange(Directory.GetDirectories(currentPath));
        directoryCount = directoryPaths.Count;

        //Find all images in the designated folder
        List<string> filePaths = new List<string>();

        filePaths.AddRange(Directory.GetFiles(currentPath, "*.png"));
        filePaths.AddRange(Directory.GetFiles(currentPath, "*.jpg"));
        filePaths.AddRange(Directory.GetFiles(currentPath, "*.jpeg"));

        iconCount = filePaths.Count;
        for (int i = 0; i < iconCount; i++)
        {
            iconPaths.Add("");
            icons.Add(null);
        }

        int addA = 0;
        if (currentPath != @"GameLoadedAssets\Sprites\Tiles\" && currentPath != @"GameLoadedAssets\Sprites\Icons\")
        {
            addA = 1;
        }

        int otherCount = addA + directoryCount;
        int elementsToCount = (pageNumber * 9) - otherCount;
        elementsToCount = elementsToCount > 9 ? 9 : elementsToCount;

        int prevLoaded = ((pageNumber * 9) - 9) - otherCount;
        int startElement = prevLoaded < 0 ? 0 : prevLoaded;

        elementsToCount = elementsToCount > iconCount - startElement ? iconCount - startElement : elementsToCount;

        for (int i = startElement; i < startElement + elementsToCount; i++)
        {
            FileStream s = new FileStream(filePaths[i], FileMode.Open);
            Bitmap image = new Bitmap(s);
            int textureWidth = image.Width;
            int textureHeight = image.Height;
            s.Close();
            Texture2D texture = new Texture2D(textureWidth, textureHeight);
            texture.LoadImage(File.ReadAllBytes(filePaths[i]));
            icons[i] = (texture);
            iconPaths[i] = (filePaths[i]);
        }
    }

    public void DirectoryButtonClicked()
    {
        currentPath = hoveredButton.GetComponent<IconSelectionButton>().buttonFilePath;
        SetupIconCanvas();
    }

    public void SpriteButtonClicked()
    {
        Texture2D t = hoveredButton.GetComponent<Image>().sprite.texture;
        if(inputObject.GetComponent<Tile>() != null && NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            lastUsedSpritePath = hoveredButton.GetComponent<IconSelectionButton>().buttonFilePath;
            lastUsedSpriteTexture = t;
            lastUsedSpriteButton.GetComponent<Image>().sprite = Sprite.Create(t, new Rect(Vector2.zero, new Vector2(t.width, t.height)), Vector2.zero);

            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"sct{hoveredButton.GetComponent<IconSelectionButton>().buttonFilePath}|{inputObject.GetComponent<Tile>().index.x}|{inputObject.GetComponent<Tile>().index.y}"));
            return;
        }
        else if (inputObject.GetComponent<MoveObject>() != null && NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            lastUsedSpritePath = hoveredButton.GetComponent<IconSelectionButton>().buttonFilePath;
            lastUsedSpriteTexture = t;
            lastUsedSpriteButton.GetComponent<Image>().sprite = Sprite.Create(t, new Rect(Vector2.zero, new Vector2(t.width, t.height)), Vector2.zero);

            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"sco{hoveredButton.GetComponent<IconSelectionButton>().buttonFilePath}|{inputObject.transform.position.x}|{inputObject.transform.position.y}"));
            return;
        }

        Sprite sprite = null;
        if (inputObject.GetComponent<Tile>() != null)
        {
            if (t.width < t.height)
            {
                sprite = Sprite.Create(t, new Rect(Vector2.zero, new Vector2(t.width, t.width)), new Vector2(0.5f, 0.5f), t.width);
            }
            else if (t.height < t.width)
            {
                sprite = Sprite.Create(t, new Rect(Vector2.zero, new Vector2(t.height, t.height)), new Vector2(0.5f, 0.5f), t.height);
            }
            else
            {
                sprite = Sprite.Create(t, new Rect(Vector2.zero, new Vector2(t.width, t.height)), new Vector2(0.5f, 0.5f), t.width);
            }
        }
        else
        {
            if (t.width < t.height)
            {
                sprite = Sprite.Create(t, new Rect(Vector2.zero, new Vector2(t.width, t.height)), new Vector2(0.5f, 0.5f), t.height);
            }
            else if (t.height < t.width)
            {
                sprite = Sprite.Create(t, new Rect(Vector2.zero, new Vector2(t.height, t.width)), new Vector2(0.5f, 0.5f), t.width);
            }
            else
            {
                sprite = Sprite.Create(t, new Rect(Vector2.zero, new Vector2(t.width, t.height)), new Vector2(0.5f, 0.5f), t.width);
            }
        }

        lastUsedSpritePath = hoveredButton.GetComponent<IconSelectionButton>().buttonFilePath;
        lastUsedSpriteTexture = t;
        lastUsedSpriteButton.GetComponent<UnityEngine.UI.Image>().sprite = Sprite.Create(t, new Rect(Vector2.zero, new Vector2(t.width, t.height)), Vector2.zero);

        inputObject.GetComponentInChildren<SpriteRenderer>().sprite = sprite;
        if(inputObject.GetComponent<MoveObject>() != null)
        {
            inputObject.GetComponent<MoveObject>().spritePath = hoveredButton.GetComponent<IconSelectionButton>().buttonFilePath;
        }
        if (inputObject.GetComponent<Tile>() != null)
        {
            inputObject.GetComponent<Tile>().tileSpritePath = hoveredButton.GetComponent<IconSelectionButton>().buttonFilePath;
        }
        gameObject.SetActive(false);

        Manager.Instance.inspector.CloseIconSelect();
    }

    public void OpenAssociatedFolder()
    {
        if (Directory.Exists(currentPath))
        {
            ProcessStartInfo sf = new ProcessStartInfo(currentPath);
            Process.Start(sf);
        }
    }

    public void PreviousSpriteButtonPressed()
    {
        if(lastUsedSpriteTexture == null || lastUsedSpritePath == "")
        {
            return;
        }

        Texture2D t = lastUsedSpriteTexture;
        if (inputObject.GetComponent<Tile>() != null && NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"sct{lastUsedSpritePath}|{inputObject.GetComponent<Tile>().index.x}|{inputObject.GetComponent<Tile>().index.y}"));
            return;
        }
        else if (inputObject.GetComponent<MoveObject>() != null && NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"sco{lastUsedSpritePath}|{inputObject.transform.position.x}|{inputObject.transform.position.y}"));
            return;
        }

        Sprite sprite = null;
        if (inputObject.GetComponent<Tile>() != null)
        {
            if (t.width < t.height)
            {
                sprite = Sprite.Create(t, new Rect(Vector2.zero, new Vector2(t.width, t.width)), new Vector2(0.5f, 0.5f), t.width);
            }
            else if (t.height < t.width)
            {
                sprite = Sprite.Create(t, new Rect(Vector2.zero, new Vector2(t.height, t.height)), new Vector2(0.5f, 0.5f), t.height);
            }
            else
            {
                sprite = Sprite.Create(t, new Rect(Vector2.zero, new Vector2(t.width, t.height)), new Vector2(0.5f, 0.5f), t.width);
            }
        }
        else
        {
            if (t.width < t.height)
            {
                sprite = Sprite.Create(t, new Rect(Vector2.zero, new Vector2(t.width, t.height)), new Vector2(0.5f, 0.5f), t.height);
            }
            else if (t.height < t.width)
            {
                sprite = Sprite.Create(t, new Rect(Vector2.zero, new Vector2(t.height, t.width)), new Vector2(0.5f, 0.5f), t.width);
            }
            else
            {
                sprite = Sprite.Create(t, new Rect(Vector2.zero, new Vector2(t.width, t.height)), new Vector2(0.5f, 0.5f), t.width);
            }
        }

        inputObject.GetComponentInChildren<SpriteRenderer>().sprite = sprite;
        if (inputObject.GetComponent<MoveObject>() != null)
        {
            inputObject.GetComponent<MoveObject>().spritePath = lastUsedSpritePath;
        }
        if (inputObject.GetComponent<Tile>() != null)
        {
            inputObject.GetComponent<Tile>().tileSpritePath = lastUsedSpritePath;
        }
        gameObject.SetActive(false);

        Manager.Instance.inspector.CloseIconSelect();
    }
}
