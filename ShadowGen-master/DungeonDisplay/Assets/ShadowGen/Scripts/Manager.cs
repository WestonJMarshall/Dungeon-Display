using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text;
using UnityEngine.Profiling;

public class Manager : MonoBehaviour
{
    #region Singlton
    private static Manager instance;

    public static Manager Instance { get { return instance; } }
    #endregion

    //Properties
    private List<CustomLight> lights;
    private List<Shape> shapes;
    public Tile[,] tiles;
    private List<Tile> lightBlockTiles = new List<Tile>();

    public bool gridSnap = true;

    public Dictionary<CustomLight, List<Tile>> lightTileAssignments;
    private List<Tile> checkedTiles;

    public InspectorCanvasScript inspector;
    public GameObject inspectingItem;

    public GameObject notificationCanvasPrefab;
    public CanvasScript mainCanvas;

    public string baseMapPath;

    public string[] saveData = null;
    public int loadline = 0;
    public int orderedTileIndex = 0;
    public List<Tile> loadOrderedTiles = new List<Tile>();
    public Dictionary<string, Sprite> spriteLoadAssignments = new Dictionary<string, Sprite>();

    public Vector3 mouseCordPrevious;
    public Vector3 mouseCordCurrent;

    public GameObject finalButton;
    public GameObject shadowMenuPrefab;
    public GameObject shadowMenu;
    public GameObject lightMenuPrefab;
    public GameObject lightMenu;

    public GameObject tilePrefab;

    public DDGameServer gameServer;
    public DDGameClient gameClient;

    #region Properties

    public List<CustomLight> Lights
    {
        get { return lights; }
    }

    public List<Shape> Shapes
    {
        get { return shapes; }
    }

    public List<Tile> LightBlockTiles
    {
        get
        {
            if(lightBlockTiles.Count > 0 || tiles == null || DungeonFile.fileType == FileType.FreeFormMap) { return lightBlockTiles; }
            else
            {
                foreach(Tile t in tiles)
                {
                    if(t.Type == TileTypes.Wall || t.Type == TileTypes.Door || t.Type == TileTypes.LockedDoor || t.Type == TileTypes.SecretDoor || t.Type == TileTypes.TrappedDoor)
                    {
                        lightBlockTiles.Add(t);
                    }
                }
                return lightBlockTiles;
            }
        }
    }

    private void ResetBlockTiles()
    {
        lightBlockTiles = new List<Tile>();
        if (tiles == null) { return; }
        else
        {
            foreach (Tile t in tiles)
            {
                if (t.Type == TileTypes.Wall || t.Type == TileTypes.Door || t.Type == TileTypes.LockedDoor || t.Type == TileTypes.SecretDoor || t.Type == TileTypes.TrappedDoor)
                {
                    lightBlockTiles.Add(t);
                }
            }
        }
    }

    #endregion

    #region Unity Methods
    //Used to make sure the singleton is working
    private void Awake()
    {
        tiles = new Tile[0, 0];
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);

        SetupFileSystem();
    }

    void Start()
    {
        lights = new List<CustomLight>();
        shapes = new List<Shape>();
        lightTileAssignments = new Dictionary<CustomLight, List<Tile>>();
        checkedTiles = new List<Tile>();
        loadline = 0;
        orderedTileIndex = 0;
        loadOrderedTiles = new List<Tile>();

        FindElementsToManage();
        AssignShapesAmbiguous();

        InspectorCanvasScript[] tempI = FindObjectsOfType<InspectorCanvasScript>();
        inspector = tempI[0];
        inspector.gameObject.SetActive(false);

        CanvasScript[] tempC = FindObjectsOfType<CanvasScript>();
        mainCanvas = tempC[0];

        ShadowTool[] tempS = FindObjectsOfType<ShadowTool>();
        if (tempS.Length > 0)
        {
            shadowMenu = tempS[0].gameObject;
        }

        SceneManager.sceneLoaded += levelLoaded;

        foreach (GameObject g in FindObjectsOfType<GameObject>())
        {
            if (g.name.Contains("BSprite"))
            {
                g.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            }
        }
    }

    public void levelLoaded(Scene scene, LoadSceneMode lsm)
    {
        tiles = new Tile[0, 0];
        lightBlockTiles = new List<Tile>();

        FindElementsToManage();
        AssignShapesAmbiguous();

        InspectorCanvasScript[] tempI = FindObjectsOfType<InspectorCanvasScript>();
        if (tempI.Length > 0)
        {
            inspector = tempI[0];
            inspector.gameObject.SetActive(false);
        }

        CanvasScript[] tempC = FindObjectsOfType<CanvasScript>();
        if (tempC.Length > 0)
        {
            mainCanvas = tempC[0];
        }

        ShadowTool[] tempS = FindObjectsOfType<ShadowTool>();
        if (tempS.Length > 0)
        {
            shadowMenu = tempS[0].gameObject;
        }

        foreach(GameObject g in FindObjectsOfType<GameObject>())
        {
            if(g.name.Contains("BSprite"))
            {
                g.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            }
        }
    }

    private void Update()
    {
        float x = 0;
        float y = 0;

        if (Input.GetMouseButtonDown(1))
        {
            mouseCordPrevious = Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.transform.position;
            mouseCordCurrent = Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.transform.position;
        }
        else if (Input.GetMouseButton(1))
        {
            mouseCordPrevious = mouseCordCurrent;
            mouseCordCurrent = Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.transform.position; 
            Camera.main.transform.Translate(new Vector3((mouseCordPrevious - mouseCordCurrent).x, (mouseCordPrevious - mouseCordCurrent).y, 0));

            if (Camera.main.transform.position.x > tiles.GetLength(0) / 2f) { Camera.main.transform.position = new Vector3(tiles.GetLength(0) / 2f, Camera.main.transform.position.y, Camera.main.transform.position.z); }
            if (Camera.main.transform.position.y > tiles.GetLength(1) / 2f) { Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, tiles.GetLength(1) / 2f, Camera.main.transform.position.z); }
            if (Camera.main.transform.position.x < -tiles.GetLength(0) / 2f) { Camera.main.transform.position = new Vector3(-tiles.GetLength(0) / 2f, Camera.main.transform.position.y, Camera.main.transform.position.z); }
            if (Camera.main.transform.position.y < -tiles.GetLength(1) / 2f) { Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, -tiles.GetLength(1) / 2f, Camera.main.transform.position.z); }
        }
        else
        {
            float speed = 0.055f;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                speed = 0.125f;
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                y = speed;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                y = -speed;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                x = speed;
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                x = -speed;
            }

            if (x != 0 || y != 0)
            {
                Camera.main.transform.Translate(new Vector3(x, y, 0) * Camera.main.orthographicSize * 0.18f);
                if (Camera.main.transform.position.x > tiles.GetLength(0) / 2f) { Camera.main.transform.position = new Vector3(tiles.GetLength(0) / 2f, Camera.main.transform.position.y, Camera.main.transform.position.z); }
                if (Camera.main.transform.position.y > tiles.GetLength(1) / 2f) { Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, tiles.GetLength(1) / 2f, Camera.main.transform.position.z); }
                if (Camera.main.transform.position.x < -tiles.GetLength(0) / 2f) { Camera.main.transform.position = new Vector3(-tiles.GetLength(0) / 2f, Camera.main.transform.position.y, Camera.main.transform.position.z); }
                if (Camera.main.transform.position.y < -tiles.GetLength(1) / 2f) { Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, -tiles.GetLength(1) / 2f, Camera.main.transform.position.z); }
            }
        }
        if (Input.mouseScrollDelta.y != 0)
        {
            foreach (Camera c in Camera.main.GetComponentsInChildren<Camera>())
            {
                c.orthographicSize -= (Input.mouseScrollDelta.y * 0.09f) * c.orthographicSize;
                if (c.orthographicSize < 0.8f) { c.orthographicSize = 0.8f; }
                else if (c.orthographicSize > 200.0f) { c.orthographicSize = 200.0f; }
            }
        }
    }
    #endregion

    #region Methods
    private void AssignShapesAmbiguous()
    {
        foreach (CustomLight light in lights)
        {
            foreach (Shape shape in shapes)
            {
                if (Vector2.Distance(shape.boundingSphereCenter, light.boundingSphereCenter) <= shape.boundingSphereRadius + light.boundingSphereRadius)
                {
                    if (Shape.IsPointInTriangleArray(shape.Points, light.Points))
                    {
                        light.shapes.Add(shape);
                    }
                }
            }
        }
    }

    public void AssignShapes()
    {
        foreach (CustomLight light in lights)
        {
            if (light.points != null) { light.CompileEdgeList(); }
            light.shapes = new List<Shape>();
            foreach (Shape shape in shapes)
            {
                if (shape.points != null) { shape.CompileEdgeList(); }
                shape.GenerateBoundingSphere();
                light.GenerateBoundingSphere();
                if (Vector2.Distance(shape.boundingSphereCenter, light.boundingSphereCenter) <= shape.boundingSphereRadius + light.boundingSphereRadius)
                {
                    //More expensive check to see if the shape is actually within the light area
                    //Check if any of the shape points are within the light area
                    if (Shape.IsPointInTriangleArray(shape.Points, light.Points))
                    {
                        light.shapes.Add(shape);
                    }
                    else
                    {
                        bool found = false;
                        foreach (Edge el in light.edges)
                        {
                            foreach (Edge es in shape.edges)
                            {
                                if (el.EdgeIntersection(es))
                                {
                                    light.shapes.Add(shape);
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    public void AssignShapesSpecific(CustomLight light)
    {
        if (light.points != null) { light.CompileEdgeList(); }
        light.shapes = new List<Shape>();
        foreach (Shape shape in shapes)
        {
            if (shape.points != null) { shape.CompileEdgeList(); }
            shape.GenerateBoundingSphere();
            light.GenerateBoundingSphere();
            if (Vector2.Distance(shape.boundingSphereCenter, light.boundingSphereCenter) <= shape.boundingSphereRadius + light.boundingSphereRadius)
            {
                //More expensive check to see if the shape is actually within the light area
                //Check if any of the shape points are within the light area
                if (Shape.IsPointInTriangleArray(shape.Points, light.Points))
                {
                    light.shapes.Add(shape);
                }
                else
                {
                    bool found = false;
                    foreach (Edge el in light.edges)
                    {
                        foreach (Edge es in shape.edges)
                        {
                            if (el.EdgeIntersection(es))
                            {
                                light.shapes.Add(shape);
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }

    public void FindElementsToManage()
    {
        CustomLight[] tempL = FindObjectsOfType<CustomLight>();
        lights = new List<CustomLight>(tempL);

        Shape[] tempS = FindObjectsOfType<Shape>();
        shapes = new List<Shape>(tempS);
    }

    public void BuildAllLights()
    {
        foreach (CustomLight l in lights)
        {
            l.PrepareAndBuildLight();
        }
    }

    public IEnumerator UpdateAtFrameEnd(CustomLight light)
    {
        yield return new WaitForEndOfFrame();
        FindElementsToManage();
    }

    public void UpdateInspector()
    {
        inspector.gameObject.SetActive(true);
        inspector.inspectingItem = inspectingItem;
        inspector.SetupInformation();
        mainCanvas.tileSetSelectorCanvas.SetActive(false);
    }

    public void SetupFileSystem()
    {
        if (!Directory.Exists(@"GameLoadedAssets"))
        {
            Directory.CreateDirectory(@"GameLoadedAssets");
        }
        if (!Directory.Exists(@"GameLoadedAssets\Sprites\TileSets\"))
        {
            Directory.CreateDirectory(@"GameLoadedAssets\Sprites\TileSets\");
        }
        if (!Directory.Exists(@"GameLoadedAssets\Sprites\Icons\"))
        {
            Directory.CreateDirectory(@"GameLoadedAssets\Sprites\Icons\");
        }
        if (!Directory.Exists(@"GameLoadedAssets\Sprites\UIIcons\"))
        {
            Directory.CreateDirectory(@"GameLoadedAssets\Sprites\UIIcons\");
        }
        if (!Directory.Exists(@"GameLoadedAssets\Maps\PixelMaps\"))
        {
            Directory.CreateDirectory(@"GameLoadedAssets\Maps\PixelMaps\");
        }
        if (!Directory.Exists(@"GameLoadedAssets\Maps\TSVMaps\"))
        {
            Directory.CreateDirectory(@"GameLoadedAssets\Maps\TSVMaps\");
        }
        if (!Directory.Exists(@"GameLoadedAssets\Maps\FreeFormMaps\"))
        {
            Directory.CreateDirectory(@"GameLoadedAssets\Maps\FreeFormMaps\");
        }
        if (!Directory.Exists(@"GameLoadedAssets\Maps\SavedMaps\"))
        {
            Directory.CreateDirectory(@"GameLoadedAssets\Maps\SavedMaps\");
        }
        if (!Directory.Exists(@"GameLoadedAssets\Sprites\Tiles\"))
        {
            Directory.CreateDirectory(@"GameLoadedAssets\Sprites\Tiles\");
        }
    }

    public void LoadTileSet(string tileSetRoot)
    {
        //Replace every possible sprite
        if (!Directory.Exists(tileSetRoot)) { return; }

        //Server send, send everything in the folder
        string serverFilePaths = "";

        #region Setup all of the edge sprites
        List<string> tileSetFiles = new List<string>(Directory.GetFiles(tileSetRoot));
        List<string> imagePaths = new List<string>();
        foreach (string s in tileSetFiles)
        {
            if (s.Split(new char[1] { '.' }).Length > 1)
            {
                if ((s.Contains(".png") || s.Contains(".jpg") || s.Contains(".jpeg")) && !s.Contains(".meta"))
                {
                    imagePaths.Add(s);
                }
            }
        }
        Dictionary<string, List<string>> edgePaths = new Dictionary<string, List<string>>();
        foreach (string s in imagePaths)
        {
            if (s.Contains("Wall") && !s.Contains("WallEdge") && !s.Contains("WallCorner"))
            {
                if (edgePaths.ContainsKey("Wall"))
                {
                    edgePaths["Wall"].Add(s);
                }
                else
                {
                    edgePaths.Add("Wall", new List<string>() { s });
                }
            }
            else if (s.Contains("Floor") && !s.Contains("FloorEdge") && !s.Contains("FloorCorner"))
            {
                if (edgePaths.ContainsKey("Floor"))
                {
                    edgePaths["Floor"].Add(s);
                }
                else
                {
                    edgePaths.Add("Floor", new List<string>() { s });
                }
            }
            else if (s.Contains("Door") && !s.Contains("Secret") && !s.Contains("Trapped") && !s.Contains("Locked"))
            {
                if (edgePaths.ContainsKey("Door"))
                {
                    edgePaths["Door"].Add(s);
                }
                else
                {
                    edgePaths.Add("Door", new List<string>() { s });
                }
            }
            else if (s.Contains("SecretDoor"))
            {
                if (edgePaths.ContainsKey("SecretDoor"))
                {
                    edgePaths["SecretDoor"].Add(s);
                }
                else
                {
                    edgePaths.Add("SecretDoor", new List<string>() { s });
                }
            }
            else if (s.Contains("TrappedDoor"))
            {
                if (edgePaths.ContainsKey("TrappedDoor"))
                {
                    edgePaths["TrappedDoor"].Add(s);
                }
                else
                {
                    edgePaths.Add("TrappedDoor", new List<string>() { s });
                }
            }
            else if (s.Contains("LockedDoor"))
            {
                if (edgePaths.ContainsKey("LockedDoor"))
                {
                    edgePaths["LockedDoor"].Add(s);
                }
                else
                {
                    edgePaths.Add("LockedDoor", new List<string>() { s });
                }
            }
            else if (s.Contains("Window"))
            {
                if (edgePaths.ContainsKey("Window"))
                {
                    edgePaths["Window"].Add(s);
                }
                else
                {
                    edgePaths.Add("Window", new List<string>() { s });
                }
            }
            else if (s.Contains("StairUpBig"))
            {
                if (edgePaths.ContainsKey("StairUpBig"))
                {
                    edgePaths["StairUpBig"].Add(s);
                }
                else
                {
                    edgePaths.Add("StairUpBig", new List<string>() { s });
                }
            }
            else if (s.Contains("StairUpSmall"))
            {
                if (edgePaths.ContainsKey("StairUpSmall"))
                {
                    edgePaths["StairUpSmall"].Add(s);
                }
                else
                {
                    edgePaths.Add("StairUpSmall", new List<string>() { s });
                }
            }
            else if (s.Contains("StairDownSmall"))
            {
                if (edgePaths.ContainsKey("StairDownSmall"))
                {
                    edgePaths["StairDownSmall"].Add(s);
                }
                else
                {
                    edgePaths.Add("StairDownSmall", new List<string>() { s });
                }
            }
            else if (s.Contains("StairDownBig"))
            {
                if (edgePaths.ContainsKey("StairDownBig"))
                {
                    edgePaths["StairDownBig"].Add(s);
                }
                else
                {
                    edgePaths.Add("StairDownBig", new List<string>() { s });
                }
            }
        }
        #endregion

        #region  Setup name -> sprite dictionary
        Dictionary<string, List<Sprite>> spriteAssignments = new Dictionary<string, List<Sprite>>();

        List<string> tileTypeStrings = new List<string>()
        {
            "Wall","Floor","Door","TrappedDoor","SecretDoor","LockedDoor","Window","StairDownBig","StairDownSmall","StairUpBig","StairUpSmall"
        };

        foreach (string str in tileTypeStrings)
        {
            if (edgePaths.ContainsKey(str))
            {
                spriteAssignments.Add(str, new List<Sprite>());

                foreach (string path in edgePaths[str])
                {
                    try
                    {
                        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
                        {
                            serverFilePaths += path + "|";
                        }
                        else
                        {
                            FileStream s = new FileStream(path, FileMode.Open);
                            Bitmap image = new Bitmap(s);
                            int textureWidth = image.Width;
                            int textureHeight = image.Height;
                            s.Close();
                            Texture2D texture = new Texture2D(textureWidth, textureHeight);
                            texture.LoadImage(File.ReadAllBytes(path));
                            if (texture.width < texture.height)
                            {
                                spriteAssignments[str].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.width)), new Vector2(0.5f, 0.5f), texture.width));
                            }
                            else if (texture.height < texture.width)
                            {
                                spriteAssignments[str].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.height, texture.height)), new Vector2(0.5f, 0.5f), texture.height));
                            }
                            else
                            {
                                spriteAssignments[str].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width));
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        #endregion

        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            serverFilePaths += SendEdgeData(serverFilePaths, tileSetRoot);
            serverFilePaths = serverFilePaths.Substring(0, serverFilePaths.Length - 1);
            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"tsc{serverFilePaths}"));
            return;
        }

        #region Assign Sprites To Tiles
        foreach (Tile t in tiles)
        {
            if (t.Type == TileTypes.Wall)
            {
                if (!spriteAssignments.ContainsKey("Wall")) { continue; }
                if (spriteAssignments["Wall"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["Wall"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["Wall"][index];
                t.tileSpritePath = edgePaths["Wall"][index];
            }
            else if (t.Type == TileTypes.Floor)
            {
                if (!spriteAssignments.ContainsKey("Floor")) { continue; }
                if (spriteAssignments["Floor"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["Floor"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["Floor"][index];
                t.tileSpritePath = edgePaths["Floor"][index];
            }
            else if (t.Type == TileTypes.Door)
            {
                if (!spriteAssignments.ContainsKey("Door")) { continue; }
                if (spriteAssignments["Door"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["Door"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["Door"][index];
                t.tileSpritePath = edgePaths["Door"][index];
            }
            else if (t.Type == TileTypes.SecretDoor)
            {
                if (!spriteAssignments.ContainsKey("SecretDoor")) { continue; }
                if (spriteAssignments["SecretDoor"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["SecretDoor"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["SecretDoor"][index];
                t.tileSpritePath = edgePaths["SecretDoor"][index];
            }
            else if (t.Type == TileTypes.TrappedDoor)
            {
                if (!spriteAssignments.ContainsKey("TrappedDoor")) { continue; }
                if (spriteAssignments["TrappedDoor"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["TrappedDoor"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["TrappedDoor"][index];
                t.tileSpritePath = edgePaths["TrappedDoor"][index];
            }
            else if (t.Type == TileTypes.LockedDoor)
            {
                if (!spriteAssignments.ContainsKey("LockedDoor")) { continue; }
                if (spriteAssignments["LockedDoor"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["LockedDoor"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["LockedDoor"][index];
                t.tileSpritePath = edgePaths["LockedDoor"][index];
            }
            else if (t.Type == TileTypes.Window)
            {
                if (!spriteAssignments.ContainsKey("Window")) { continue; }
                if (spriteAssignments["Window"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["Window"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["Window"][index];
                t.tileSpritePath = edgePaths["Window"][index];
            }
            else if (t.Type == TileTypes.StairDownBig)
            {
                if (!spriteAssignments.ContainsKey("StairDownBig")) { continue; }
                if (spriteAssignments["StairDownBig"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["StairDownBig"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["StairDownBig"][index];
                t.tileSpritePath = edgePaths["StairDownBig"][index];
            }
            else if (t.Type == TileTypes.StairDownSmall)
            {
                if (!spriteAssignments.ContainsKey("StairDownSmall")) { continue; }
                if (spriteAssignments["StairDownSmall"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["StairDownSmall"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["StairDownSmall"][index];
                t.tileSpritePath = edgePaths["StairDownSmall"][index];
            }
            else if (t.Type == TileTypes.StairUpSmall)
            {
                if (!spriteAssignments.ContainsKey("StairUpSmall")) { continue; }
                if (spriteAssignments["StairUpSmall"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["StairUpSmall"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["StairUpSmall"][index];
                t.tileSpritePath = edgePaths["StairUpSmall"][index];
            }
            else if (t.Type == TileTypes.StairUpBig)
            {
                if (!spriteAssignments.ContainsKey("StairUpBig")) { continue; }
                if (spriteAssignments["StairUpBig"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["StairUpBig"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["StairUpBig"][index];
                t.tileSpritePath = edgePaths["StairUpBig"][index];
            }
        }
        #endregion

        IntelligentLoadEdgeSet();
    }

    public string SendEdgeData(string inputPaths, string setPath)
    {
        string edgeFilePaths = "";

        #region Setup all of the edge sprites
        List<string> tileSetFiles = new List<string>(Directory.GetFiles(setPath));
        List<string> imagePaths = new List<string>();
        foreach (string s in tileSetFiles)
        {
            if (s.Split(new char[1] { '.' }).Length > 1)
            {
                if (s.Contains(".png") || s.Contains(".jpg") || s.Contains(".jpeg"))
                {
                    imagePaths.Add(s);
                }
            }
        }
        Dictionary<string, List<string>> edgePaths = new Dictionary<string, List<string>>();
        foreach (string s in imagePaths)
        {
            if (s.Contains("WallEdgeOne"))
            {
                if (edgePaths.ContainsKey("WallEdgeOne"))
                {
                    edgePaths["WallEdgeOne"].Add(s);
                }
                else
                {
                    edgePaths.Add("WallEdgeOne", new List<string>() { s });
                }
            }
            else if (s.Contains("WallEdgeTwo"))
            {
                if (edgePaths.ContainsKey("WallEdgeTwo"))
                {
                    edgePaths["WallEdgeTwo"].Add(s);
                }
                else
                {
                    edgePaths.Add("WallEdgeTwo", new List<string>() { s });
                }
            }
            else if (s.Contains("WallEdgeThree"))
            {
                if (edgePaths.ContainsKey("WallEdgeThree"))
                {
                    edgePaths["WallEdgeThree"].Add(s);
                }
                else
                {
                    edgePaths.Add("WallEdgeThree", new List<string>() { s });
                }
            }
            else if (s.Contains("WallEdgeFour"))
            {
                if (edgePaths.ContainsKey("WallEdgeFour"))
                {
                    edgePaths["WallEdgeFour"].Add(s);
                }
                else
                {
                    edgePaths.Add("WallEdgeFour", new List<string>() { s });
                }
            }
            else if (s.Contains("WallCorner"))
            {
                if (edgePaths.ContainsKey("WallCorner"))
                {
                    edgePaths["WallCorner"].Add(s);
                }
                else
                {
                    edgePaths.Add("WallCorner", new List<string>() { s });
                }
            }
            else if (s.Contains("FloorEdgeOne"))
            {
                if (edgePaths.ContainsKey("FloorEdgeOne"))
                {
                    edgePaths["FloorEdgeOne"].Add(s);
                }
                else
                {
                    edgePaths.Add("FloorEdgeOne", new List<string>() { s });
                }
            }
            else if (s.Contains("FloorEdgeTwo"))
            {
                if (edgePaths.ContainsKey("FloorEdgeTwo"))
                {
                    edgePaths["FloorEdgeTwo"].Add(s);
                }
                else
                {
                    edgePaths.Add("FloorEdgeTwo", new List<string>() { s });
                }
            }
            else if (s.Contains("FloorEdgeThree"))
            {
                if (edgePaths.ContainsKey("FloorEdgeThree"))
                {
                    edgePaths["FloorEdgeThree"].Add(s);
                }
                else
                {
                    edgePaths.Add("FloorEdgeThree", new List<string>() { s });
                }
            }
            else if (s.Contains("FloorEdgeFour"))
            {
                if (edgePaths.ContainsKey("FloorEdgeFour"))
                {
                    edgePaths["FloorEdgeFour"].Add(s);
                }
                else
                {
                    edgePaths.Add("FloorEdgeFour", new List<string>() { s });
                }
            }
            else if (s.Contains("FloorCorner"))
            {
                if (edgePaths.ContainsKey("FloorCorner"))
                {
                    edgePaths["FloorCorner"].Add(s);
                }
                else
                {
                    edgePaths.Add("FloorCorner", new List<string>() { s });
                }
            }
        }
        #endregion

        #region  Setup name -> sprite dictionary
        Dictionary<string, List<Sprite>> spriteAssignments = new Dictionary<string, List<Sprite>>();

        List<string> tileTypeStrings = new List<string>()
        {
            "WallEdgeOne","WallEdgeTwo","WallEdgeThree","WallEdgeFour","WallCorner","FloorEdgeOne","FloorEdgeTwo","FloorEdgeThree","FloorEdgeFour","FloorCorner"
        };

        foreach (string str in tileTypeStrings)
        {
            if (edgePaths.ContainsKey(str))
            {
                spriteAssignments.Add(str, new List<Sprite>());

                foreach (string path in edgePaths[str])
                {
                    edgeFilePaths += path + "|";
                }
            }
        }
        #endregion

        return edgeFilePaths;
    }

    public void ServerLoadTileSet(string tileSetRoot)
    {
        //Replace every possible sprite
        if (!Directory.Exists(tileSetRoot)) { return; }

        #region Setup all of the edge sprites
        List<string> tileSetFiles = new List<string>(Directory.GetFiles(tileSetRoot));
        List<string> imagePaths = new List<string>();
        foreach (string s in tileSetFiles)
        {
            if (s.Split(new char[1] { '.' }).Length > 1)
            {
                if ((s.Contains(".png") || s.Contains(".jpg") || s.Contains(".jpeg")) && !s.Contains(".meta"))
                {
                    imagePaths.Add(s);
                }
            }
        }
        Dictionary<string, List<string>> edgePaths = new Dictionary<string, List<string>>();
        foreach (string s in imagePaths)
        {
            if (s.Contains("Wall") && !s.Contains("WallEdge") && !s.Contains("WallCorner"))
            {
                if (edgePaths.ContainsKey("Wall"))
                {
                    edgePaths["Wall"].Add(s);
                }
                else
                {
                    edgePaths.Add("Wall", new List<string>() { s });
                }
            }
            else if (s.Contains("Floor") && !s.Contains("FloorEdge") && !s.Contains("FloorCorner"))
            {
                if (edgePaths.ContainsKey("Floor"))
                {
                    edgePaths["Floor"].Add(s);
                }
                else
                {
                    edgePaths.Add("Floor", new List<string>() { s });
                }
            }
            else if (s.Contains("Door") && !s.Contains("Secret") && !s.Contains("Trapped") && !s.Contains("Locked"))
            {
                if (edgePaths.ContainsKey("Door"))
                {
                    edgePaths["Door"].Add(s);
                }
                else
                {
                    edgePaths.Add("Door", new List<string>() { s });
                }
            }
            else if (s.Contains("SecretDoor"))
            {
                if (edgePaths.ContainsKey("SecretDoor"))
                {
                    edgePaths["SecretDoor"].Add(s);
                }
                else
                {
                    edgePaths.Add("SecretDoor", new List<string>() { s });
                }
            }
            else if (s.Contains("TrappedDoor"))
            {
                if (edgePaths.ContainsKey("TrappedDoor"))
                {
                    edgePaths["TrappedDoor"].Add(s);
                }
                else
                {
                    edgePaths.Add("TrappedDoor", new List<string>() { s });
                }
            }
            else if (s.Contains("LockedDoor"))
            {
                if (edgePaths.ContainsKey("LockedDoor"))
                {
                    edgePaths["LockedDoor"].Add(s);
                }
                else
                {
                    edgePaths.Add("LockedDoor", new List<string>() { s });
                }
            }
            else if (s.Contains("Window"))
            {
                if (edgePaths.ContainsKey("Window"))
                {
                    edgePaths["Window"].Add(s);
                }
                else
                {
                    edgePaths.Add("Window", new List<string>() { s });
                }
            }
            else if (s.Contains("StairUpBig"))
            {
                if (edgePaths.ContainsKey("StairUpBig"))
                {
                    edgePaths["StairUpBig"].Add(s);
                }
                else
                {
                    edgePaths.Add("StairUpBig", new List<string>() { s });
                }
            }
            else if (s.Contains("StairUpSmall"))
            {
                if (edgePaths.ContainsKey("StairUpSmall"))
                {
                    edgePaths["StairUpSmall"].Add(s);
                }
                else
                {
                    edgePaths.Add("StairUpSmall", new List<string>() { s });
                }
            }
            else if (s.Contains("StairDownSmall"))
            {
                if (edgePaths.ContainsKey("StairDownSmall"))
                {
                    edgePaths["StairDownSmall"].Add(s);
                }
                else
                {
                    edgePaths.Add("StairDownSmall", new List<string>() { s });
                }
            }
            else if (s.Contains("StairDownBig"))
            {
                if (edgePaths.ContainsKey("StairDownBig"))
                {
                    edgePaths["StairDownBig"].Add(s);
                }
                else
                {
                    edgePaths.Add("StairDownBig", new List<string>() { s });
                }
            }
        }
        #endregion

        #region  Setup name -> sprite dictionary
        Dictionary<string, List<Sprite>> spriteAssignments = new Dictionary<string, List<Sprite>>();

        List<string> tileTypeStrings = new List<string>()
        {
            "Wall","Floor","Door","TrappedDoor","SecretDoor","LockedDoor","Window","StairDownBig","StairDownSmall","StairUpBig","StairUpSmall"
        };

        foreach (string str in tileTypeStrings)
        {
            if (edgePaths.ContainsKey(str))
            {
                spriteAssignments.Add(str, new List<Sprite>());

                foreach (string path in edgePaths[str])
                {
                    try
                    {
                        FileStream s = new FileStream(path, FileMode.Open);
                        Bitmap image = new Bitmap(s);
                        int textureWidth = image.Width;
                        int textureHeight = image.Height;
                        s.Close();
                        Texture2D texture = new Texture2D(textureWidth, textureHeight);
                        texture.LoadImage(File.ReadAllBytes(path));
                        if (texture.width < texture.height)
                        {
                            spriteAssignments[str].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.width)), new Vector2(0.5f, 0.5f), texture.width));
                        }
                        else if (texture.height < texture.width)
                        {
                            spriteAssignments[str].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.height, texture.height)), new Vector2(0.5f, 0.5f), texture.height));
                        }
                        else
                        {
                            spriteAssignments[str].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width));
                        }
                    }
                    catch { }
                }
            }
        }
        #endregion

        #region Assign Sprites To Tiles
        foreach (Tile t in tiles)
        {
            if (t.Type == TileTypes.Wall)
            {
                if (!spriteAssignments.ContainsKey("Wall")) { continue; }
                if (spriteAssignments["Wall"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["Wall"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["Wall"][index];
                t.tileSpritePath = edgePaths["Wall"][index];
            }
            else if (t.Type == TileTypes.Floor)
            {
                if (!spriteAssignments.ContainsKey("Floor")) { continue; }
                if (spriteAssignments["Floor"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["Floor"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["Floor"][index];
                t.tileSpritePath = edgePaths["Floor"][index];
            }
            else if (t.Type == TileTypes.Door)
            {
                if (!spriteAssignments.ContainsKey("Door")) { continue; }
                if (spriteAssignments["Door"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["Door"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["Door"][index];
                t.tileSpritePath = edgePaths["Door"][index];
            }
            else if (t.Type == TileTypes.SecretDoor)
            {
                if (!spriteAssignments.ContainsKey("SecretDoor")) { continue; }
                if (spriteAssignments["SecretDoor"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["SecretDoor"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["SecretDoor"][index];
                t.tileSpritePath = edgePaths["SecretDoor"][index];
            }
            else if (t.Type == TileTypes.TrappedDoor)
            {
                if (!spriteAssignments.ContainsKey("TrappedDoor")) { continue; }
                if (spriteAssignments["TrappedDoor"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["TrappedDoor"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["TrappedDoor"][index];
                t.tileSpritePath = edgePaths["TrappedDoor"][index];
            }
            else if (t.Type == TileTypes.LockedDoor)
            {
                if (!spriteAssignments.ContainsKey("LockedDoor")) { continue; }
                if (spriteAssignments["LockedDoor"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["LockedDoor"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["LockedDoor"][index];
                t.tileSpritePath = edgePaths["LockedDoor"][index];
            }
            else if (t.Type == TileTypes.Window)
            {
                if (!spriteAssignments.ContainsKey("Window")) { continue; }
                if (spriteAssignments["Window"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["Window"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["Window"][index];
                t.tileSpritePath = edgePaths["Window"][index];
            }
            else if (t.Type == TileTypes.StairDownBig)
            {
                if (!spriteAssignments.ContainsKey("StairDownBig")) { continue; }
                if (spriteAssignments["StairDownBig"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["StairDownBig"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["StairDownBig"][index];
                t.tileSpritePath = edgePaths["StairDownBig"][index];
            }
            else if (t.Type == TileTypes.StairDownSmall)
            {
                if (!spriteAssignments.ContainsKey("StairDownSmall")) { continue; }
                if (spriteAssignments["StairDownSmall"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["StairDownSmall"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["StairDownSmall"][index];
                t.tileSpritePath = edgePaths["StairDownSmall"][index];
            }
            else if (t.Type == TileTypes.StairUpSmall)
            {
                if (!spriteAssignments.ContainsKey("StairUpSmall")) { continue; }
                if (spriteAssignments["StairUpSmall"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["StairUpSmall"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["StairUpSmall"][index];
                t.tileSpritePath = edgePaths["StairUpSmall"][index];
            }
            else if (t.Type == TileTypes.StairUpBig)
            {
                if (!spriteAssignments.ContainsKey("StairUpBig")) { continue; }
                if (spriteAssignments["StairUpBig"].Count == 0) { continue; }
                int index = Random.Range(0, spriteAssignments["StairUpBig"].Count);
                t.GetComponent<SpriteRenderer>().sprite = spriteAssignments["StairUpBig"][index];
                t.tileSpritePath = edgePaths["StairUpBig"][index];
            }
        }
        #endregion
        IntelligentLoadEdgeSet();
    }

    public void ApplyBackgroundMap(string filePath, int pixelsPerInch)
    {
        GameObject g = new GameObject("Background Image");
        g.transform.position = new Vector3(-0.5f, -0.5f, 2.0f);
        g.AddComponent<SpriteRenderer>();

        FileStream s = new FileStream(filePath, FileMode.Open);
        Bitmap image = new Bitmap(s);
        int textureWidth = image.Width;
        int textureHeight = image.Height;
        s.Close();
        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        texture.LoadImage(File.ReadAllBytes(filePath));
        g.GetComponent<SpriteRenderer>().sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), pixelsPerInch);
    }

    public List<byte[]> RetrieveMapImageData()
    {
        Dictionary<string, byte[]> imageData = new Dictionary<string, byte[]>();
        foreach (Tile t in tiles)
        {
            if (!imageData.ContainsKey(t.tileSpritePath))
            {
                imageData.Add(t.tileSpritePath, File.ReadAllBytes(t.tileSpritePath));
            }
        }
        MoveObject[] moveObjects = GameObject.FindObjectsOfType<MoveObject>();
        foreach (MoveObject m in moveObjects)
        {
            if (!imageData.ContainsKey(m.spritePath))
            {
                imageData.Add(m.spritePath, File.ReadAllBytes(m.spritePath));
            }
        }
        List<byte[]> imageDataList = new List<byte[]>();
        foreach (KeyValuePair<string, byte[]> sb in imageData)
        {
            imageDataList.Add(sb.Value);
        }
        return imageDataList;
    }

    public string RetrieveMapImageLocations()
    {
        string imageLocations = "";
        foreach (Tile t in tiles)
        {
            if (!imageLocations.Contains(t.tileSpritePath))
            {
                imageLocations += t.tileSpritePath + "|";
            }
        }
        MoveObject[] moveObjects = GameObject.FindObjectsOfType<MoveObject>();
        foreach(MoveObject m in moveObjects)
        {
            if(!imageLocations.Contains(m.spritePath))
            {
                imageLocations += m.spritePath + "|";
            }
        }
        return imageLocations.Substring(0, imageLocations.Length - 1);
    }

    public void OpenShadowMenu()
    {
        try
        {
            if (shadowMenu.activeSelf)
            {
                shadowMenu.GetComponent<ShadowTool>().StopAllCoroutines();
                shadowMenu.GetComponent<ShadowTool>().CloseShadowTool();
                shadowMenu.SetActive(!shadowMenu.activeSelf);
            }
            else
            {
                shadowMenu.SetActive(!shadowMenu.activeSelf);
                shadowMenu.GetComponent<ShadowTool>().OpenShadowTool();
            }
        }
        catch
        {
            shadowMenu = Instantiate(shadowMenuPrefab);
            shadowMenu.GetComponent<ShadowTool>().OpenShadowTool();
        }
        try
        {
            if (lightMenu.activeSelf)
            {
                lightMenu.GetComponent<LightTool>().StopAllCoroutines();
                lightMenu.GetComponent<LightTool>().CloseLightTool();
                lightMenu.SetActive(!lightMenu.activeSelf);
            }
        }
        catch { }

        inspector.CloseInspector();
        if (mainCanvas.tileSetSelectorCanvas.activeSelf)
        {
            mainCanvas.TileSetSelect();
        }
    }

    public void OpenShadowMenuButton()
    {
        if (!NetworkingManager.Instance.host) { InfoLogCanvasScript.SendInfoMessage("Only Hosts Can Do This!", UnityEngine.Color.red, 9, false); return; }

        try
        {
            if (shadowMenu.activeSelf)
            {
                shadowMenu.GetComponent<ShadowTool>().StopAllCoroutines();
                shadowMenu.GetComponent<ShadowTool>().CloseShadowTool();
                shadowMenu.SetActive(!shadowMenu.activeSelf);
            }
            else
            {
                shadowMenu.SetActive(!shadowMenu.activeSelf);
                shadowMenu.GetComponent<ShadowTool>().OpenShadowTool();
            }
        }
        catch
        {
            shadowMenu = Instantiate(shadowMenuPrefab);
            shadowMenu.GetComponent<ShadowTool>().OpenShadowTool();
        }
        try
        {
            if (lightMenu.activeSelf)
            {
                lightMenu.GetComponent<LightTool>().StopAllCoroutines();
                lightMenu.GetComponent<LightTool>().CloseLightTool();
                lightMenu.SetActive(!lightMenu.activeSelf);
            }
        }
        catch { }

        inspector.CloseInspector();
        if (mainCanvas.tileSetSelectorCanvas.activeSelf)
        {
            mainCanvas.TileSetSelect();
        }
    }

    public void OpenLightMenu()
    {
        try
        {
            if (lightMenu.activeSelf)
            {
                lightMenu.GetComponent<LightTool>().StopAllCoroutines();
                lightMenu.GetComponent<LightTool>().CloseLightTool();
                lightMenu.SetActive(!lightMenu.activeSelf);
            }
            else
            {
                lightMenu.SetActive(!lightMenu.activeSelf);
                lightMenu.GetComponent<LightTool>().OpenLightTool();
            }
        }
        catch
        {
            lightMenu = Instantiate(lightMenuPrefab);
            lightMenu.GetComponent<LightTool>().OpenLightTool();
        }

        try
        {
            if (shadowMenu.activeSelf)
            {
                shadowMenu.GetComponent<ShadowTool>().StopAllCoroutines();
                shadowMenu.GetComponent<ShadowTool>().CloseShadowTool();
                shadowMenu.SetActive(!shadowMenu.activeSelf);
            }
        }
        catch { }

        inspector.CloseInspector();
        if (mainCanvas.tileSetSelectorCanvas.activeSelf)
        {
            mainCanvas.TileSetSelect();
        }
    }

    public void OpenLightMenuButton()
    {
        if (!NetworkingManager.Instance.host) { InfoLogCanvasScript.SendInfoMessage("Only Hosts Can Do This!", UnityEngine.Color.red, 9, false); return; }

        try
        {
            if (lightMenu.activeSelf)
            {
                lightMenu.GetComponent<LightTool>().StopAllCoroutines();
                lightMenu.GetComponent<LightTool>().CloseLightTool();
                lightMenu.SetActive(!lightMenu.activeSelf);
            }
            else
            {
                lightMenu.SetActive(!lightMenu.activeSelf);
                lightMenu.GetComponent<LightTool>().OpenLightTool();
            }
        }
        catch
        {
            lightMenu = Instantiate(lightMenuPrefab);
            lightMenu.GetComponent<LightTool>().OpenLightTool();
        }

        try
        {
            if (shadowMenu.activeSelf)
            {
                shadowMenu.GetComponent<ShadowTool>().StopAllCoroutines();
                shadowMenu.GetComponent<ShadowTool>().CloseShadowTool();
                shadowMenu.SetActive(!shadowMenu.activeSelf);
            }
        }
        catch { }

        inspector.CloseInspector();
        if (mainCanvas.tileSetSelectorCanvas.activeSelf)
        {
            mainCanvas.TileSetSelect();
        }
    }

    public void CloseLightMenu()
    {
        try
        {
            if (lightMenu.activeSelf)
            {
                lightMenu.GetComponent<LightTool>().StopAllCoroutines();
                lightMenu.GetComponent<LightTool>().CloseLightTool();
                lightMenu.SetActive(!lightMenu.activeSelf);
            }
        }
        catch { }
    }

    public void CloseShadowMenu()
    {
        try
        {
            if (shadowMenu.activeSelf)
            {
                shadowMenu.GetComponent<ShadowTool>().StopAllCoroutines();
                shadowMenu.GetComponent<ShadowTool>().CloseShadowTool();
                shadowMenu.SetActive(!shadowMenu.activeSelf);
            }
        }
        catch { }
    }

    #endregion

    #region Saving and Loading

    public void SaveMap(string saveName)
    {
        if(DungeonFile.fileType == FileType.FreeFormMap) { SaveFreeFormMap(saveName); return; }

        InfoLogCanvasScript.SendInfoMessage("Saving...", UnityEngine.Color.black);

        List<string> saveData = new List<string>();
        //location of base map
        if(DungeonFile.fromSaveFile)
        {
            saveData.Add(File.ReadAllLines(DungeonFile.path)[0]);
        }
        else
        {
            saveData.Add(DungeonFile.path);
        }

        //Type of the basemap
        saveData.Add(DungeonFile.fileType.ToString());

        FreeLightArea[] flal = FindObjectsOfType<FreeLightArea>();
        foreach (FreeLightArea fla in flal)
        {
            foreach (Vector2 v in fla.points)
            {
                saveData.Add(v.ToString());
            }
            saveData.Add("E");
        }
        saveData.Add("X");

        //Move Object saveData
        MoveObject[] tempMO = FindObjectsOfType<MoveObject>();
        List<GameObject> moveObjects = new List<GameObject>();
        foreach (MoveObject m in tempMO)
        {
            moveObjects.Add(m.gameObject);
        }

        for (int i = 0; i < moveObjects.Count; i++)
        {
            //Cast light bool
            saveData.Add(moveObjects[i].GetComponent<MoveObject>().lightObject.ToString());

            //Location
            saveData.Add(moveObjects[i].transform.position.ToString());

            if (moveObjects[i].GetComponent<MoveObject>().lightObject)
            {
                //functional bool
                saveData.Add(moveObjects[i].GetComponentInChildren<CustomLight>().functional.ToString());

                //functional light size
                saveData.Add(moveObjects[i].GetComponentInChildren<CustomLight>().boundingSphereRadius.ToString());
            }
            else
            {
                //PlaceHolders
                saveData.Add("null");
                saveData.Add("null");
            }

            //Scale
            saveData.Add(moveObjects[i].GetComponentInChildren<SpriteRenderer>().transform.localScale.x.ToString());

            //Rotation
            saveData.Add(moveObjects[i].GetComponentInChildren<SpriteRenderer>().transform.rotation.eulerAngles.z.ToString());

            //Sprite Path
            saveData.Add(moveObjects[i].GetComponent<MoveObject>().spritePath);

            saveData.Add("E");
        }
        saveData.Add("X");

        string doorStates = "";

        //Add the list of each tile's sprite file location
        foreach (Tile t in tiles)
        {
            saveData.Add(t.tileSpritePath);
            saveData.Add(t.Type.ToString());
            if(t.Type == TileTypes.Door || t.Type == TileTypes.SecretDoor || t.Type == TileTypes.LockedDoor || t.Type == TileTypes.TrappedDoor)
            {
                doorStates += t.index.x.ToString() + "|";
                doorStates += t.index.y.ToString() + "|";
                doorStates += (t.shadowObject.GetComponent<ComplexShape>().functional ? "1" : "0") + "|";
            }
        }
        if (doorStates != "")
        {
            doorStates = doorStates.Substring(0, doorStates.Length - 1);
            saveData.Add(doorStates);
        }

        //Add light type to save file
        saveData.Add((mainCanvas.globalLight.GetComponent<Light2D>().intensity < 0.5f).ToString());

        //StreamWriter saveStream = null;
        if (File.Exists(@"GameLoadedAssets\Maps\SavedMaps\" + saveName + ".txt"))
        {
            File.Delete(@"GameLoadedAssets\Maps\SavedMaps\" + saveName + ".txt");
            GameObject notificationPanel = Instantiate(notificationCanvasPrefab);
            notificationPanel.GetComponent<NotificationCanvasScript>().notificationText.text = "Saved Successfuly and Overwrote Previous Save File at: " + @"GameLoadedAssets\Maps\SavedMaps\" + saveName + ".txt";
        }
        else
        {
            GameObject notificationPanel = Instantiate(notificationCanvasPrefab);
            notificationPanel.GetComponent<NotificationCanvasScript>().notificationText.text = "Saved Successfuly at: " + @"GameLoadedAssets\Maps\SavedMaps\" + saveName + ".txt";
        }

        File.WriteAllLines(@"GameLoadedAssets\Maps\SavedMaps\" + saveName + ".txt", saveData.ToArray());
    }

    public void SaveFreeFormMap(string saveName)
    {
        InfoLogCanvasScript.SendInfoMessage("Saving...", UnityEngine.Color.black);

        List<string> saveData = new List<string>();
        //location of base map
        if (DungeonFile.fromSaveFile)
        {
            saveData.Add(File.ReadAllLines(DungeonFile.path)[0]);
        }
        else
        {
            saveData.Add(DungeonFile.path);
        }

        //Type of the basemap
        saveData.Add(DungeonFile.fileType.ToString());

        //Save grid size
        saveData.Add(DungeonFile.freeFormSize.ToString());

        FreeLightArea[] flal = FindObjectsOfType<FreeLightArea>();
        foreach(FreeLightArea fla in flal)
        {
            foreach (Vector2 v in fla.points)
            {
                saveData.Add(v.ToString());
            }
            saveData.Add("E");
        }
        saveData.Add("X");

        //Save shadow points
        foreach (Shape s in shapes)
        {
            foreach(Vector2 v in s.points)
            {
                saveData.Add(v.ToString());
            }
            saveData.Add("E");
        }
        saveData.Add("X");

        //Move Object saveData
        MoveObject[] tempMO = FindObjectsOfType<MoveObject>();
        List<GameObject> moveObjects = new List<GameObject>();
        foreach (MoveObject m in tempMO)
        {
            moveObjects.Add(m.gameObject);
        }

        for (int i = 0; i < moveObjects.Count; i++)
        {
            //Cast light bool
            saveData.Add(moveObjects[i].GetComponent<MoveObject>().lightObject.ToString());

            //Location
            saveData.Add(moveObjects[i].transform.position.ToString());

            if (moveObjects[i].GetComponent<MoveObject>().lightObject)
            {
                //functional bool
                saveData.Add(moveObjects[i].GetComponentInChildren<CustomLight>().functional.ToString());

                //functional light size
                saveData.Add(moveObjects[i].GetComponentInChildren<CustomLight>().boundingSphereRadius.ToString());
            }
            else
            {
                //PlaceHolders
                saveData.Add("null");
                saveData.Add("null");
            }

            //Scale
            saveData.Add(moveObjects[i].GetComponentInChildren<SpriteRenderer>().transform.localScale.x.ToString());

            //Rotation
            saveData.Add(moveObjects[i].GetComponentInChildren<SpriteRenderer>().transform.rotation.eulerAngles.z.ToString());

            //Sprite Path
            saveData.Add(moveObjects[i].GetComponent<MoveObject>().spritePath);

            saveData.Add("E");
        }
        saveData.Add("X");

        //Add the list of each tile's sprite file location
        foreach (Tile t in tiles)
        {
            saveData.Add(t.tileSpritePath);
            saveData.Add(t.Type.ToString());
        }

        //Add light type to save file
        saveData.Add((mainCanvas.globalLight.GetComponent<Light2D>().intensity < 0.5f).ToString());

        //StreamWriter saveStream = null;
        if (File.Exists(@"GameLoadedAssets\Maps\SavedMaps\" + saveName + ".txt"))
        {
            File.Delete(@"GameLoadedAssets\Maps\SavedMaps\" + saveName + ".txt");
            if (NetworkingManager.Instance.networkingState == NetworkingState.NoConnection)
            {
                GameObject notificationPanel = Instantiate(notificationCanvasPrefab);
                notificationPanel.GetComponent<NotificationCanvasScript>().notificationText.text = "Saved Successfuly and Overwrote Previous Save File at: " + @"GameLoadedAssets\Maps\SavedMaps\" + saveName + ".txt";
            }
        }
        else
        {
            if (NetworkingManager.Instance.networkingState == NetworkingState.NoConnection)
            {
                GameObject notificationPanel = Instantiate(notificationCanvasPrefab);
                notificationPanel.GetComponent<NotificationCanvasScript>().notificationText.text = "Saved Successfuly at: " + @"GameLoadedAssets\Maps\SavedMaps\" + saveName + ".txt";
            }
        }

        File.WriteAllLines(@"GameLoadedAssets\Maps\SavedMaps\" + saveName + ".txt", saveData.ToArray());
    }

    public IEnumerator LoadMap()
    {
        if (DungeonFile.fileType == FileType.FreeFormMap) { StartCoroutine("LoadFreeFormMap"); yield break; }

        yield return new WaitForEndOfFrame();

        PrepareFinalButton();

        string saveFilePath = DungeonFile.path;
        //InfoLogCanvasScript.SendInfoMessage("Loading..." + " " + saveFilePath, UnityEngine.Color.black);
        mainCanvas.gameObject.SetActive(false);
        if (File.Exists(saveFilePath))
        {
            try
            {
                string[] data = File.ReadAllLines(saveFilePath);
                saveData = data;

                int line = 2;

                //Load free light areas
                while (data[line] != "X")
                {
                    GameObject lightArea = new GameObject("Free Light Area");
                    lightArea.AddComponent<FreeLightArea>();
                    lightArea.GetComponent<FreeLightArea>().points = new List<Vector2>();

                    while (data[line] != "E")
                    {
                        string unFormattedPosString = data[line].Substring(1);
                        unFormattedPosString = unFormattedPosString.Remove(unFormattedPosString.Length - 1);
                        string[] formattedPosString = unFormattedPosString.Split(new char[1] { ',' });
                        float x = 0.0f;
                        float.TryParse(formattedPosString[0], out x);
                        float y = 0.0f;
                        float.TryParse(formattedPosString[1], out y);

                        lightArea.GetComponent<FreeLightArea>().points.Add(new Vector2(x, y));
                        line++;
                    }
                    lightArea.GetComponent<FreeLightArea>().DrawLight();
                    line++;
                }
                line++;

                //The map should have already been loaded and now the scene is ready to be updated
                //Load all move objects
                while (data[line] != "X")
                {
                    while (data[line] != "E")
                    {
                        string unFormattedPosString = data[line + 1].Substring(1);
                        unFormattedPosString = unFormattedPosString.Remove(unFormattedPosString.Length - 1);
                        string[] formattedPosString = unFormattedPosString.Split(new char[1] { ',' });
                        float x = 0.0f;
                        float.TryParse(formattedPosString[0], out x);
                        float y = 0.0f;
                        float.TryParse(formattedPosString[1], out y);
                        float z = 0.0f;
                        float.TryParse(formattedPosString[2], out z);

                        Vector3 pos = new Vector3(x, y, z);
                        if (data[line] == "True")
                        {
                            GameObject moveObject = MakePrefab(pos);
                            if (data[line + 2] == "False")
                            {
                                moveObject.GetComponentInChildren<CustomLight>().functional = false;
                            }
                            float scaleMultiplier = float.Parse(data[line + 3]) / moveObject.GetComponentInChildren<CustomLight>().boundingSphereRadius;
                            moveObject.GetComponentInChildren<CustomLight>().Scale(moveObject.GetComponentInChildren<CustomLight>().center, scaleMultiplier);

                            moveObject.GetComponentInChildren<Light2D>().pointLightOuterRadius = moveObject.GetComponentInChildren<CustomLight>().boundingSphereRadius * 1.65f;

                            moveObject.GetComponent<MoveObject>().AssignSprite(data[line + 6]);

                            moveObject.GetComponent<MoveObject>().GetComponentInChildren<SpriteRenderer>().transform.localScale = new Vector3(float.Parse(data[line + 4]), float.Parse(data[line + 4]), 1);

                            moveObject.GetComponentInChildren<SpriteRenderer>().transform.localRotation = Quaternion.Euler(moveObject.GetComponentInChildren<SpriteRenderer>().transform.localRotation.eulerAngles.x, moveObject.GetComponentInChildren<SpriteRenderer>().transform.localRotation.eulerAngles.y, float.Parse(data[line + 5]));
                        }
                        else
                        {
                            GameObject moveObject = MakeObject(pos);

                            moveObject.GetComponent<MoveObject>().GetComponentInChildren<SpriteRenderer>().transform.localScale = new Vector3(float.Parse(data[line + 4]), float.Parse(data[line + 4]), 1);

                            moveObject.GetComponentInChildren<SpriteRenderer>().transform.localRotation = Quaternion.Euler(moveObject.GetComponentInChildren<SpriteRenderer>().transform.localRotation.eulerAngles.x, moveObject.GetComponentInChildren<SpriteRenderer>().transform.localRotation.eulerAngles.y, float.Parse(data[line + 5]));

                            moveObject.GetComponent<MoveObject>().AssignSprite(data[line + 6]);
                        }

                        line += 7;
                    }
                    line++;
                }
                line++;

                loadline = line;
                spriteLoadAssignments = new Dictionary<string, Sprite>();
                loadOrderedTiles = new List<Tile>();

                if (data[line] != "null")
                {
                    //Load in the proper sprites
                    foreach (Tile t in tiles)
                    {
                        loadOrderedTiles.Add(t);
                        if (data[line] != "null" && line != data.Length - 1)
                        {
                            string unformattedTileTypeString = data[line];
                            if (!spriteLoadAssignments.ContainsKey(unformattedTileTypeString))
                            {
                                //Load & add sprite
                                FileStream s = new FileStream(unformattedTileTypeString, FileMode.Open);
                                Bitmap image = new Bitmap(s);
                                int textureWidth = image.Width;
                                int textureHeight = image.Height;
                                s.Close();
                                Texture2D texture = new Texture2D(textureWidth, textureHeight);
                                texture.LoadImage(File.ReadAllBytes(unformattedTileTypeString));
                                Sprite sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width);
                                spriteLoadAssignments.Add(unformattedTileTypeString, sprite);
                            }
                            string[] formattedTileTypeString = unformattedTileTypeString.Split(new char[1] { '\\' });
                            string dataTypeString = formattedTileTypeString[formattedTileTypeString.Length - 1];
                            dataTypeString = dataTypeString.Substring(0, dataTypeString.IndexOf('.'));
                            if (data[line + 1] == "Empty") { t.Type = TileTypes.Empty; }
                            else if (data[line + 1] == "Wall") { t.Type = TileTypes.Wall; }
                            else if (data[line + 1] == "Floor") { t.Type = TileTypes.Floor; }
                            else if (data[line + 1] == "Door") { t.Type = TileTypes.Door; }
                            else if (data[line + 1] == "TrappedDoor") { t.Type = TileTypes.TrappedDoor; }
                            else if (data[line + 1] == "SecretDoor") { t.Type = TileTypes.SecretDoor; }
                            else if (data[line + 1] == "LockedDoor") { t.Type = TileTypes.LockedDoor; }
                            else if (data[line + 1] == "StairDownBig") { t.Type = TileTypes.StairDownBig; }
                            else if (data[line + 1] == "StairDownSmall") { t.Type = TileTypes.StairDownSmall; }
                            else if (data[line + 1] == "StairUpBig") { t.Type = TileTypes.StairUpBig; }
                            else if (data[line + 1] == "StairUpSmall") { t.Type = TileTypes.StairUpSmall; }
                            else if (data[line + 1] == "Window") { t.Type = TileTypes.Window; }
                            t.SelectSprite();
                        }
                        else
                        {
                            t.Type = TileTypes.Wall;
                            t.SelectSprite();
                        }
                        line += 2;
                    }
                }
                else
                {
                    //Load in the proper sprites
                    foreach (Tile t in tiles)
                    {
                        t.SelectSpriteFromFile();
                    }
                }
                CreateShadowObjects();

                try
                {
                    string[] doorStates = data[line].Split(new char[1] { '|' });
                    for (int i = 0; i < doorStates.Length; i += 3)
                    {
                        tiles[int.Parse(doorStates[i]), int.Parse(doorStates[i + 1])].shadowObject.GetComponent<ComplexShape>().functional = doorStates[i + 2] == "1";
                        if (doorStates[i + 2] == "0")
                        {
                            tiles[int.Parse(doorStates[i]), int.Parse(doorStates[i + 1])].spriteRenderer.color = new UnityEngine.Color(tiles[int.Parse(doorStates[i]), int.Parse(doorStates[i + 1])].spriteRenderer.color.r, tiles[int.Parse(doorStates[i]), int.Parse(doorStates[i + 1])].spriteRenderer.color.g, tiles[int.Parse(doorStates[i]), int.Parse(doorStates[i + 1])].spriteRenderer.color.b, 0.35f);
                        }
                    }
                }
                catch { line--; }

                RotateDoorTiles();

                line++;
                mainCanvas.globalLight.GetComponent<Light2D>().intensity = data[line] == "True" ? 0 : 1 - (0.1f * (data[line] == "True" ? 0 : 1 - 1));

                if (NetworkingManager.Instance.host)
                {
                    mainCanvas.ToggleShadows();
                }

                orderedTileIndex = 0;
                for (int i = 0; i < 24; i++)
                {
                    StartCoroutine("LoadSpritesRealTime");
                }

                InfoLogCanvasScript.SendInfoMessage("Loading tile sprites..." + " " + saveFilePath, UnityEngine.Color.black);
            }
            catch
            {
                GameObject notificationPanel = Instantiate(notificationCanvasPrefab);
                notificationPanel.GetComponent<NotificationCanvasScript>().notificationText.text = "Load Failed, Return to Main Menu";
                notificationPanel.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
                notificationPanel.GetComponentInChildren<Button>().onClick.AddListener(ReturnToMainMenu);
            }
        }
        else
        {
            GameObject notificationPanel = Instantiate(notificationCanvasPrefab);
            notificationPanel.GetComponent<NotificationCanvasScript>().notificationText.text = "Load Failed, Return to Main Menu";
            notificationPanel.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
            notificationPanel.GetComponentInChildren<Button>().onClick.AddListener(ReturnToMainMenu);
        }
    }

    public IEnumerator LoadFreeFormMap()
    {
        yield return new WaitForEndOfFrame();

        PrepareFinalButton();

        string saveFilePath = DungeonFile.path;
        InfoLogCanvasScript.SendInfoMessage("Loading..." + " " + saveFilePath, UnityEngine.Color.black);
        if (NetworkingManager.Instance.host)
        {
            mainCanvas.ToggleShadows();
        }
        mainCanvas.gameObject.SetActive(false);
        if (File.Exists(saveFilePath))
        {
            try
            {
                string[] data = File.ReadAllLines(saveFilePath);
                saveData = data;

                int line = 3;

                //Load free light areas
                while (data[line] != "X")
                {
                    GameObject lightArea = new GameObject("Free Light Area");
                    lightArea.AddComponent<FreeLightArea>();
                    lightArea.GetComponent<FreeLightArea>().points = new List<Vector2>();

                    while (data[line] != "E")
                    {
                        string unFormattedPosString = data[line].Substring(1);
                        unFormattedPosString = unFormattedPosString.Remove(unFormattedPosString.Length - 1);
                        string[] formattedPosString = unFormattedPosString.Split(new char[1] { ',' });
                        float x = 0.0f;
                        float.TryParse(formattedPosString[0], out x);
                        float y = 0.0f;
                        float.TryParse(formattedPosString[1], out y);

                        lightArea.GetComponent<FreeLightArea>().points.Add(new Vector2(x, y));
                        line++;
                    }
                    lightArea.GetComponent<FreeLightArea>().DrawLight();
                    line++;
                }
                line++;

                for (int i = 0; i < shapes.Count; i++)
                {
                    Destroy(shapes[i].gameObject);
                }
                shapes = new List<Shape>();
                while (data[line] != "X")
                {
                    GameObject shadow = new GameObject("LoadedShadow");
                    shadow.AddComponent<ComplexShape>();
                    while (data[line] != "E")
                    {
                        string unFormattedPosString = data[line].Substring(1);
                        unFormattedPosString = unFormattedPosString.Remove(unFormattedPosString.Length - 1);
                        string[] formattedPosString = unFormattedPosString.Split(new char[1] { ',' });
                        float x = 0.0f;
                        float.TryParse(formattedPosString[0], out x);
                        float y = 0.0f;
                        float.TryParse(formattedPosString[1], out y);

                        shadow.GetComponent<ComplexShape>().points.Add(new Vector2(x, y));
                        line++;
                    }
                    shapes.Add(shadow.GetComponent<ComplexShape>());
                    line++;
                }
                line++;

                //The map should have already been loaded and now the scene is ready to be updated
                //Load all move objects
                while (data[line] != "X")
                {
                    while (data[line] != "E")
                    {
                        string unFormattedPosString = data[line + 1].Substring(1);
                        unFormattedPosString = unFormattedPosString.Remove(unFormattedPosString.Length - 1);
                        string[] formattedPosString = unFormattedPosString.Split(new char[1] { ',' });
                        float x = 0.0f;
                        float.TryParse(formattedPosString[0], out x);
                        float y = 0.0f;
                        float.TryParse(formattedPosString[1], out y);
                        float z = 0.0f;
                        float.TryParse(formattedPosString[2], out z);

                        Vector3 pos = new Vector3(x, y, z);
                        if (data[line] == "True")
                        {
                            GameObject moveObject = MakePrefab(pos);
                            if (data[line + 2] == "False")
                            {
                                moveObject.GetComponentInChildren<CustomLight>().functional = false;
                            }
                            float scaleMultiplier = float.Parse(data[line + 3]) / moveObject.GetComponentInChildren<CustomLight>().boundingSphereRadius;
                            moveObject.GetComponentInChildren<CustomLight>().Scale(moveObject.GetComponentInChildren<CustomLight>().center, scaleMultiplier);

                            moveObject.GetComponentInChildren<Light2D>().pointLightOuterRadius = moveObject.GetComponentInChildren<CustomLight>().boundingSphereRadius * 1.65f;

                            moveObject.GetComponent<MoveObject>().AssignSprite(data[line + 6]);

                            moveObject.GetComponent<MoveObject>().GetComponentInChildren<SpriteRenderer>().transform.localScale = new Vector3(float.Parse(data[line + 4]), float.Parse(data[line + 4]), 1);

                            moveObject.GetComponentInChildren<SpriteRenderer>().transform.localRotation = Quaternion.Euler(moveObject.GetComponentInChildren<SpriteRenderer>().transform.localRotation.eulerAngles.x, moveObject.GetComponentInChildren<SpriteRenderer>().transform.localRotation.eulerAngles.y, float.Parse(data[line + 5]));
                        }
                        else
                        {
                            GameObject moveObject = MakeObject(pos);

                            moveObject.GetComponent<MoveObject>().GetComponentInChildren<SpriteRenderer>().transform.localScale = new Vector3(float.Parse(data[line + 4]), float.Parse(data[line + 4]), 1);

                            moveObject.GetComponentInChildren<SpriteRenderer>().transform.localRotation = Quaternion.Euler(moveObject.GetComponentInChildren<SpriteRenderer>().transform.localRotation.eulerAngles.x, moveObject.GetComponentInChildren<SpriteRenderer>().transform.localRotation.eulerAngles.y, float.Parse(data[line + 5]));

                            moveObject.GetComponent<MoveObject>().AssignSprite(data[line + 6]);
                        }

                        line += 7;
                    }
                    line++;
                }
                line++;

                loadline = line;
                spriteLoadAssignments = new Dictionary<string, Sprite>();
                loadOrderedTiles = new List<Tile>();

                if (data[line] != "null")
                {
                    //Load in the proper sprites
                    foreach (Tile t in tiles)
                    {
                        loadOrderedTiles.Add(t);
                        if (data[line] != "null")
                        {
                            string unformattedTileTypeString = data[line];
                            if (!spriteLoadAssignments.ContainsKey(unformattedTileTypeString))
                            {
                                //Load & add sprite
                                FileStream s = new FileStream(unformattedTileTypeString, FileMode.Open);
                                Bitmap image = new Bitmap(s);
                                int textureWidth = image.Width;
                                int textureHeight = image.Height;
                                s.Close();
                                Texture2D texture = new Texture2D(textureWidth, textureHeight);
                                texture.LoadImage(File.ReadAllBytes(unformattedTileTypeString));
                                Sprite sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width);
                                spriteLoadAssignments.Add(unformattedTileTypeString, sprite);
                            }
                            string[] formattedTileTypeString = unformattedTileTypeString.Split(new char[1] { '\\' });
                            string dataTypeString = formattedTileTypeString[formattedTileTypeString.Length - 1];
                            dataTypeString = dataTypeString.Substring(0, dataTypeString.IndexOf('.'));
                            t.Type = TileTypes.Floor;
                            t.SelectSprite();
                        }
                        else
                        {
                            t.Type = TileTypes.Floor;
                            t.SelectSprite();
                        }
                        line += 2;
                    }
                }
                else
                {
                    //Load in the proper sprites
                    foreach (Tile t in tiles)
                    {
                        t.SelectSpriteFromFile();
                    }
                }

                if (NetworkingManager.Instance.host)
                {
                    mainCanvas.ToggleShadows();
                }

                mainCanvas.globalLight.GetComponent<Light2D>().intensity = data[line] == "True" ? 0 : 1 - (0.1f * (data[line] == "True" ? 0 : 1 - 1));

                orderedTileIndex = 0;
                for(int i = 0; i < 24; i++)
                {
                    StartCoroutine("LoadSpritesRealTime");
                }

                Instance.FindElementsToManage();
                Instance.AssignShapes();
                Instance.BuildAllLights();

                InfoLogCanvasScript.SendInfoMessage("Loading tile sprites..." + " " + saveFilePath, UnityEngine.Color.black);
            }
            catch
            {
                GameObject notificationPanel = Instantiate(notificationCanvasPrefab);
                notificationPanel.GetComponent<NotificationCanvasScript>().notificationText.text = "Load Failed, Return to Main Menu";
                notificationPanel.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
                notificationPanel.GetComponentInChildren<Button>().onClick.AddListener(ReturnToMainMenu);
            }
        }
        else
        {
            GameObject notificationPanel = Instantiate(notificationCanvasPrefab);
            notificationPanel.GetComponent<NotificationCanvasScript>().notificationText.text = "Load Failed, Return to Main Menu";
            notificationPanel.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
            notificationPanel.GetComponentInChildren<Button>().onClick.AddListener(ReturnToMainMenu);
        }
    }

    public IEnumerator LoadSpritesRealTime()
    {
        string unformattedTileTypeString = saveData[loadline];
        if (unformattedTileTypeString != "null")
        {
            try
            {
                loadOrderedTiles[orderedTileIndex].GetComponent<SpriteRenderer>().sprite = spriteLoadAssignments[unformattedTileTypeString];
                loadOrderedTiles[orderedTileIndex].tileSpritePath = unformattedTileTypeString;
            }
            catch { }
        }
        else
        {
            loadOrderedTiles[orderedTileIndex].SelectSpriteFromFile();
        }

        yield return new WaitForSecondsRealtime(0.02f);

        loadline += 2;
        orderedTileIndex++;
        if (orderedTileIndex < loadOrderedTiles.Count)
        {
            StartCoroutine("LoadSpritesRealTime");
        }
        else
        {
            StopAllCoroutines();
            LoadFinalization();
        }
    }

    private void LoadFinalization()
    {
        foreach (Tile t in tiles)
        {
            t.transform.position = new Vector3(t.transform.position.x, t.transform.position.y, 1);
        }

        ResetBlockTiles();

        IntelligentLoadEdgeSet();

        FindElementsToManage();
        AssignShapes();
        BuildAllLights();

        mainCanvas.gameObject.SetActive(true);

        InfoLogCanvasScript.SendInfoMessage("Load complete!", UnityEngine.Color.black);
    }

    public void PrepareFinalButton()
    {
        DestroyImmediate(GameObject.Find("LightTool").GetComponent<Button>());
        GameObject.Find("LightTool").AddComponent<Button>();
        GameObject.Find("LightTool").GetComponent<Button>().onClick.AddListener(OpenLightMenuButton);
        finalButton = GameObject.Find("FinalButton");
        finalButton.GetComponent<Button>().onClick.RemoveAllListeners();

        if (DungeonFile.fileType == FileType.FreeFormMap)
        {
            finalButton.GetComponent<Button>().onClick.AddListener(OpenShadowMenuButton);

            FileStream s = new FileStream(@"GameLoadedAssets\Sprites\UIIcons\ShadowMenuIcon.png", FileMode.Open);
            Bitmap image = new Bitmap(s);
            int textureWidth = image.Width;
            int textureHeight = image.Height;
            s.Close();
            Texture2D texture = new Texture2D(textureWidth, textureHeight);
            texture.LoadImage(File.ReadAllBytes(@"GameLoadedAssets\Sprites\UIIcons\ShadowMenuIcon.png"));

            finalButton.GetComponent<UnityEngine.UI.Image>().sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f));

            finalButton.GetComponentInChildren<Text>().text = "Shadow Tool";
        }
        else
        {
            FileStream s = new FileStream(@"GameLoadedAssets\Sprites\UIIcons\TileSetSwapIcon.png", FileMode.Open);
            Bitmap image = new Bitmap(s);
            int textureWidth = image.Width;
            int textureHeight = image.Height;
            s.Close();
            Texture2D texture = new Texture2D(textureWidth, textureHeight);
            texture.LoadImage(File.ReadAllBytes(@"GameLoadedAssets\Sprites\UIIcons\TileSetSwapIcon.png"));

            finalButton.GetComponent<UnityEngine.UI.Image>().sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f));

            finalButton.GetComponent<Button>().onClick.AddListener(mainCanvas.GetComponent<CanvasScript>().TileSetSelect);

            finalButton.GetComponentInChildren<Text>().text = "Tile Sets";
        }
    }

    public IEnumerator PrepareMap()
    {
        yield return new WaitForEndOfFrame();

        PrepareFinalButton();

        try
        {
            if (NetworkingManager.Instance.host)
            {
                mainCanvas.ToggleShadows();
            }
            mainCanvas.gameObject.SetActive(false);

            spriteLoadAssignments = new Dictionary<string, Sprite>();
            LoadTileSet(@"GameLoadedAssets\Sprites\TileSets\Default\");

            mainCanvas.gameObject.SetActive(true);

            IntelligentLoadEdgeSet();

            RotateDoorTiles();
        }
        catch
        {
            GameObject notificationPanel = Instantiate(notificationCanvasPrefab);
            notificationPanel.GetComponent<NotificationCanvasScript>().notificationText.text = "Load Failed, Return to Main Menu";
            notificationPanel.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
            notificationPanel.GetComponentInChildren<Button>().onClick.AddListener(ReturnToMainMenu);
        }
    }

    public IEnumerator PrepareFreeFormMap()
    {
        yield return new WaitForEndOfFrame();

        PrepareFinalButton();

        try
        {
            if (NetworkingManager.Instance.host)
            {
                mainCanvas.ToggleShadows();
            }
            mainCanvas.gameObject.SetActive(false);

            spriteLoadAssignments = new Dictionary<string, Sprite>();

            //Load & add sprite
            FileStream s = new FileStream(@"GameLoadedAssets\Sprites\TileSets\Default\Invisible.png", FileMode.Open);
            Bitmap image = new Bitmap(s);
            int textureWidth = image.Width;
            int textureHeight = image.Height;
            s.Close();
            Texture2D texture = new Texture2D(textureWidth, textureHeight);
            texture.LoadImage(File.ReadAllBytes(@"GameLoadedAssets\Sprites\TileSets\Default\Invisible.png"));
            Sprite sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width);
            spriteLoadAssignments.Add("Invis", sprite);

            foreach (Tile t in tiles)
            {
                t.spriteRenderer.sprite = spriteLoadAssignments["Invis"];
                t.tileSpritePath = @"GameLoadedAssets\Sprites\TileSets\Default\Invisible.png";
            }

            mainCanvas.gameObject.SetActive(true);
        }
        catch
        {
            GameObject notificationPanel = Instantiate(notificationCanvasPrefab);
            notificationPanel.GetComponent<NotificationCanvasScript>().notificationText.text = "Load Failed, Return to Main Menu";
            notificationPanel.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
            notificationPanel.GetComponentInChildren<Button>().onClick.AddListener(ReturnToMainMenu);
        }
    }

    public GameObject MakePrefab(Vector3 pos)
    {
        GameObject instantiatedLight = Instantiate(mainCanvas.LightPrefab, Vector3.zero, Quaternion.identity);
        instantiatedLight.GetComponent<MoveObject>().AssignSprite();
        instantiatedLight.GetComponentInChildren<CustomLight>().TranslateWithoutBuilding(pos);
        instantiatedLight.transform.position = pos;
        instantiatedLight.GetComponentInChildren<CustomLight>().UpdateLoop();
        FindElementsToManage();
        AssignShapes();
        instantiatedLight.GetComponentInChildren<Light2D>().pointLightOuterRadius = instantiatedLight.GetComponentInChildren<CustomLight>().boundingSphereRadius * 1.65f;

        return instantiatedLight;
    }

    public GameObject MakeObject(Vector3 pos)
    {
        GameObject instantiatedLight = Instantiate(mainCanvas.LightPrefab, Vector3.zero, Quaternion.identity);
        instantiatedLight.transform.position = pos;
        Destroy(instantiatedLight.GetComponentInChildren<CustomLight>().gameObject);
        Destroy(instantiatedLight.GetComponentInChildren<Light2D>().gameObject);
        instantiatedLight.GetComponent<MoveObject>().AssignSprite();
        instantiatedLight.GetComponentInChildren<SpriteRenderer>().sortingLayerName = "Default";

        return instantiatedLight;
    }

    public void IntelligentLoadEdgeSet()
    {
        //Find which sprite set should be used
        string tileSetPath = null;
        foreach (Tile t in tiles)
        {
            if (t.tileSpritePath != null)
            {
                if (t.tileSpritePath.Contains("TileSets"))
                {
                    tileSetPath = t.tileSpritePath.Replace(t.tileSpritePath.Split(new char[1] { '\\' })[t.tileSpritePath.Split(new char[1] { '\\' }).Length - 1], "");
                    break;
                }
            }
        }
        if (tileSetPath != null)
        {
            LoadEdgeSet(tileSetPath);
        }

        foreach(Tile t in tiles)
        {
            t.transform.position = new Vector3(t.transform.position.x, t.transform.position.y, 1.0f);
        }
    }

    public void LoadEdgeSet(string setPath)
    {
        //Remove all overlay objects
        List<GameObject> currentEdgeGraphics = new List<GameObject>(GameObject.FindGameObjectsWithTag("EdgeGraphic"));
        foreach(GameObject g in currentEdgeGraphics) { Destroy(g); }

        #region Setup all of the edge sprites
        List<string> tileSetFiles = new List<string>(Directory.GetFiles(setPath));
        List<string> imagePaths = new List<string>();
        foreach(string s in tileSetFiles)
        {
            if (s.Split(new char[1] { '.' }).Length > 1)
            {
                if (s.Contains(".png") || s.Contains(".jpg") || s.Contains(".jpeg"))
                {
                    imagePaths.Add(s);
                }
            }
        }
        Dictionary<string, List<string>> edgePaths = new Dictionary<string, List<string>>();
        foreach (string s in imagePaths)
        {
            if (s.Contains("WallEdgeOne"))
            {
                if(edgePaths.ContainsKey("WallEdgeOne"))
                {
                    edgePaths["WallEdgeOne"].Add(s);
                }
                else
                {
                    edgePaths.Add("WallEdgeOne", new List<string>() { s });
                }
            }
            else if (s.Contains("WallEdgeTwo"))
            {
                if (edgePaths.ContainsKey("WallEdgeTwo"))
                {
                    edgePaths["WallEdgeTwo"].Add(s);
                }
                else
                {
                    edgePaths.Add("WallEdgeTwo", new List<string>() { s });
                }
            }
            else if (s.Contains("WallEdgeThree"))
            {
                if (edgePaths.ContainsKey("WallEdgeThree"))
                {
                    edgePaths["WallEdgeThree"].Add(s);
                }
                else
                {
                    edgePaths.Add("WallEdgeThree", new List<string>() { s });
                }
            }
            else if (s.Contains("WallEdgeFour"))
            {
                if (edgePaths.ContainsKey("WallEdgeFour"))
                {
                    edgePaths["WallEdgeFour"].Add(s);
                }
                else
                {
                    edgePaths.Add("WallEdgeFour", new List<string>() { s });
                }
            }
            else if (s.Contains("WallCorner"))
            {
                if (edgePaths.ContainsKey("WallCorner"))
                {
                    edgePaths["WallCorner"].Add(s);
                }
                else
                {
                    edgePaths.Add("WallCorner", new List<string>() { s });
                }
            }
            else if (s.Contains("FloorEdgeOne"))
            {
                if (edgePaths.ContainsKey("FloorEdgeOne"))
                {
                    edgePaths["FloorEdgeOne"].Add(s);
                }
                else
                {
                    edgePaths.Add("FloorEdgeOne", new List<string>() { s });
                }
            }
            else if (s.Contains("FloorEdgeTwo"))
            {
                if (edgePaths.ContainsKey("FloorEdgeTwo"))
                {
                    edgePaths["FloorEdgeTwo"].Add(s);
                }
                else
                {
                    edgePaths.Add("FloorEdgeTwo", new List<string>() { s });
                }
            }
            else if (s.Contains("FloorEdgeThree"))
            {
                if (edgePaths.ContainsKey("FloorEdgeThree"))
                {
                    edgePaths["FloorEdgeThree"].Add(s);
                }
                else
                {
                    edgePaths.Add("FloorEdgeThree", new List<string>() { s });
                }
            }
            else if (s.Contains("FloorEdgeFour"))
            {
                if (edgePaths.ContainsKey("FloorEdgeFour"))
                {
                    edgePaths["FloorEdgeFour"].Add(s);
                }
                else
                {
                    edgePaths.Add("FloorEdgeFour", new List<string>() { s });
                }
            }
            else if (s.Contains("FloorCorner"))
            {
                if (edgePaths.ContainsKey("FloorCorner"))
                {
                    edgePaths["FloorCorner"].Add(s);
                }
                else
                {
                    edgePaths.Add("FloorCorner", new List<string>() { s });
                }
            }
        }
        #endregion

        #region  Setup name -> sprite dictionary
        Dictionary<string, List<Sprite>> spriteAssignments = new Dictionary<string, List<Sprite>>();
        if(edgePaths.ContainsKey("WallEdgeOne"))
        {
            spriteAssignments.Add("WallEdgeOne", new List<Sprite>());

            foreach(string path in edgePaths["WallEdgeOne"])
            {
                FileStream s = new FileStream(path, FileMode.Open);
                Bitmap image = new Bitmap(s);
                int textureWidth = image.Width;
                int textureHeight = image.Height;
                s.Close();
                Texture2D texture = new Texture2D(textureWidth, textureHeight);
                texture.LoadImage(File.ReadAllBytes(path));
                spriteAssignments["WallEdgeOne"].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width));
            }
        }
        if (edgePaths.ContainsKey("WallEdgeTwo"))
        {
            spriteAssignments.Add("WallEdgeTwo", new List<Sprite>());

            foreach (string path in edgePaths["WallEdgeTwo"])
            {
                FileStream s = new FileStream(path, FileMode.Open);
                Bitmap image = new Bitmap(s);
                int textureWidth = image.Width;
                int textureHeight = image.Height;
                s.Close();
                Texture2D texture = new Texture2D(textureWidth, textureHeight);
                texture.LoadImage(File.ReadAllBytes(path));
                spriteAssignments["WallEdgeTwo"].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width));
            }
        }
        if (edgePaths.ContainsKey("WallEdgeThree"))
        {
            spriteAssignments.Add("WallEdgeThree", new List<Sprite>());

            foreach (string path in edgePaths["WallEdgeThree"])
            {
                FileStream s = new FileStream(path, FileMode.Open);
                Bitmap image = new Bitmap(s);
                int textureWidth = image.Width;
                int textureHeight = image.Height;
                s.Close();
                Texture2D texture = new Texture2D(textureWidth, textureHeight);
                texture.LoadImage(File.ReadAllBytes(path));
                spriteAssignments["WallEdgeThree"].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width));
            }
        }
        if (edgePaths.ContainsKey("WallEdgeFour"))
        {
            spriteAssignments.Add("WallEdgeFour", new List<Sprite>());

            foreach (string path in edgePaths["WallEdgeFour"])
            {
                FileStream s = new FileStream(path, FileMode.Open);
                Bitmap image = new Bitmap(s);
                int textureWidth = image.Width;
                int textureHeight = image.Height;
                s.Close();
                Texture2D texture = new Texture2D(textureWidth, textureHeight);
                texture.LoadImage(File.ReadAllBytes(path));
                spriteAssignments["WallEdgeFour"].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width));
            }
        }
        if (edgePaths.ContainsKey("WallCorner"))
        {
            spriteAssignments.Add("WallCorner", new List<Sprite>());

            foreach (string path in edgePaths["WallCorner"])
            {
                FileStream s = new FileStream(path, FileMode.Open);
                Bitmap image = new Bitmap(s);
                int textureWidth = image.Width;
                int textureHeight = image.Height;
                s.Close();
                Texture2D texture = new Texture2D(textureWidth, textureHeight);
                texture.LoadImage(File.ReadAllBytes(path));
                spriteAssignments["WallCorner"].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width));
            }
        }
        if (edgePaths.ContainsKey("FloorEdgeOne"))
        {
            spriteAssignments.Add("FloorEdgeOne", new List<Sprite>());

            foreach (string path in edgePaths["FloorEdgeOne"])
            {
                FileStream s = new FileStream(path, FileMode.Open);
                Bitmap image = new Bitmap(s);
                int textureWidth = image.Width;
                int textureHeight = image.Height;
                s.Close();
                Texture2D texture = new Texture2D(textureWidth, textureHeight);
                texture.LoadImage(File.ReadAllBytes(path));
                spriteAssignments["FloorEdgeOne"].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width));
            }
        }
        if (edgePaths.ContainsKey("FloorEdgeTwo"))
        {
            spriteAssignments.Add("FloorEdgeTwo", new List<Sprite>());

            foreach (string path in edgePaths["FloorEdgeTwo"])
            {
                FileStream s = new FileStream(path, FileMode.Open);
                Bitmap image = new Bitmap(s);
                int textureWidth = image.Width;
                int textureHeight = image.Height;
                s.Close();
                Texture2D texture = new Texture2D(textureWidth, textureHeight);
                texture.LoadImage(File.ReadAllBytes(path));
                spriteAssignments["FloorEdgeTwo"].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width));
            }
        }
        if (edgePaths.ContainsKey("FloorEdgeThree"))
        {
            spriteAssignments.Add("FloorEdgeThree", new List<Sprite>());

            foreach (string path in edgePaths["FloorEdgeThree"])
            {
                FileStream s = new FileStream(path, FileMode.Open);
                Bitmap image = new Bitmap(s);
                int textureWidth = image.Width;
                int textureHeight = image.Height;
                s.Close();
                Texture2D texture = new Texture2D(textureWidth, textureHeight);
                texture.LoadImage(File.ReadAllBytes(path));
                spriteAssignments["FloorEdgeThree"].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width));
            }
        }
        if (edgePaths.ContainsKey("FloorEdgeFour"))
        {
            spriteAssignments.Add("FloorEdgeFour", new List<Sprite>());

            foreach (string path in edgePaths["FloorEdgeFour"])
            {
                FileStream s = new FileStream(path, FileMode.Open);
                Bitmap image = new Bitmap(s);
                int textureWidth = image.Width;
                int textureHeight = image.Height;
                s.Close();
                Texture2D texture = new Texture2D(textureWidth, textureHeight);
                texture.LoadImage(File.ReadAllBytes(path));
                spriteAssignments["FloorEdgeFour"].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width));
            }
        }
        if (edgePaths.ContainsKey("FloorCorner"))
        {
            spriteAssignments.Add("FloorCorner", new List<Sprite>());

            foreach (string path in edgePaths["FloorCorner"])
            {
                FileStream s = new FileStream(path, FileMode.Open);
                Bitmap image = new Bitmap(s);
                int textureWidth = image.Width;
                int textureHeight = image.Height;
                s.Close();
                Texture2D texture = new Texture2D(textureWidth, textureHeight);
                texture.LoadImage(File.ReadAllBytes(path));
                spriteAssignments["FloorCorner"].Add(Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width));
            }
        }
        #endregion

        //Check tiles and create the appropriate graphics for it
        //Walls
        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                if (tiles[i, j].Type == TileTypes.Wall)
                {
                    #region Wall Check
                    //Check if there is a floor tile on the adjacent 4 sides
                    bool top = true; //true = floor here
                    bool bottom = true;
                    bool left = true;
                    bool right = true;

                    if (i == 0)
                    {
                        if (tiles[i + 1, j].Type != TileTypes.Floor && tiles[i + 1, j].Type != TileTypes.StairDownBig && tiles[i + 1, j].Type != TileTypes.StairDownSmall && tiles[i + 1, j].Type != TileTypes.StairUpBig && tiles[i + 1, j].Type != TileTypes.StairUpSmall) { right = false; }
                        left = false;
                    }
                    else if (i == tiles.GetLength(0) - 1)
                    {
                        if (tiles[i - 1, j].Type != TileTypes.Floor && tiles[i - 1, j].Type != TileTypes.StairDownBig && tiles[i - 1, j].Type != TileTypes.StairDownSmall && tiles[i - 1, j].Type != TileTypes.StairUpBig && tiles[i - 1, j].Type != TileTypes.StairUpSmall) { left = false; }
                        right = false;
                    }
                    else
                    {
                        if (tiles[i + 1, j].Type != TileTypes.Floor && tiles[i + 1, j].Type != TileTypes.StairDownBig && tiles[i + 1, j].Type != TileTypes.StairDownSmall && tiles[i + 1, j].Type != TileTypes.StairUpBig && tiles[i + 1, j].Type != TileTypes.StairUpSmall) { right = false; }
                        if (tiles[i - 1, j].Type != TileTypes.Floor && tiles[i - 1, j].Type != TileTypes.StairDownBig && tiles[i - 1, j].Type != TileTypes.StairDownSmall && tiles[i - 1, j].Type != TileTypes.StairUpBig && tiles[i - 1, j].Type != TileTypes.StairUpSmall) { left = false; }
                    }

                    if (j == 0)
                    {
                        if (tiles[i, j + 1].Type != TileTypes.Floor && tiles[i, j + 1].Type != TileTypes.StairDownBig && tiles[i, j + 1].Type != TileTypes.StairDownSmall && tiles[i, j + 1].Type != TileTypes.StairUpBig && tiles[i, j + 1].Type != TileTypes.StairUpSmall) { top = false; }
                        bottom = false;
                    }
                    else if (j == tiles.GetLength(1) - 1)
                    {
                        if (tiles[i, j - 1].Type != TileTypes.Floor && tiles[i, j - 1].Type != TileTypes.StairDownBig && tiles[i, j - 1].Type != TileTypes.StairDownSmall && tiles[i, j - 1].Type != TileTypes.StairUpBig && tiles[i, j - 1].Type != TileTypes.StairUpSmall) { bottom = false; }
                        top = false;
                    }
                    else
                    {
                        if (tiles[i, j + 1].Type != TileTypes.Floor && tiles[i, j + 1].Type != TileTypes.StairDownBig && tiles[i, j + 1].Type != TileTypes.StairDownSmall && tiles[i, j + 1].Type != TileTypes.StairUpBig && tiles[i, j + 1].Type != TileTypes.StairUpSmall) { top = false; }
                        if (tiles[i, j - 1].Type != TileTypes.Floor && tiles[i, j - 1].Type != TileTypes.StairDownBig && tiles[i, j - 1].Type != TileTypes.StairDownSmall && tiles[i, j - 1].Type != TileTypes.StairUpBig && tiles[i, j - 1].Type != TileTypes.StairUpSmall) { bottom = false; }
                    }

                    int trueNum = 0;
                    if (top) { trueNum++; }
                    if (bottom) { trueNum++; }
                    if (left) { trueNum++; }
                    if (right) { trueNum++; }

                    //Determine which diagonals will be checked
                    bool tl = false;
                    bool tr = false;
                    bool bl = false;
                    bool br = false;

                    bool noLeft = false;
                    bool noRight = false;
                    bool noTop = false;
                    bool noBottom = false;
                    //Special Checks
                    if (i == tiles.GetLength(0) - 1)
                    {
                        noRight = true;
                    }
                    if (i == 0)
                    {
                        noLeft = true;
                    }
                    if (j == tiles.GetLength(1) - 1)
                    {
                        noTop = true;
                    }
                    if (j == 0)
                    {
                        noBottom = true;
                    }

                    if (!noTop && !noRight && (tiles[i + 1, j + 1].Type == TileTypes.Floor || tiles[i + 1, j + 1].Type == TileTypes.StairDownBig || tiles[i + 1, j + 1].Type == TileTypes.StairDownSmall || tiles[i + 1, j + 1].Type == TileTypes.StairUpBig || tiles[i + 1, j + 1].Type == TileTypes.StairUpSmall))
                    {
                        tr = true;
                    }
                    if (!noBottom && !noRight && (tiles[i + 1, j - 1].Type == TileTypes.Floor || tiles[i + 1, j - 1].Type == TileTypes.StairDownBig || tiles[i + 1, j - 1].Type == TileTypes.StairDownSmall || tiles[i + 1, j - 1].Type == TileTypes.StairUpBig || tiles[i + 1, j - 1].Type == TileTypes.StairUpSmall))
                    {
                        br = true;
                    }
                    if (!noTop && !noLeft && (tiles[i - 1, j + 1].Type == TileTypes.Floor || tiles[i - 1, j + 1].Type == TileTypes.StairDownBig || tiles[i - 1, j + 1].Type == TileTypes.StairDownSmall || tiles[i - 1, j + 1].Type == TileTypes.StairUpBig || tiles[i - 1, j + 1].Type == TileTypes.StairUpSmall))
                    {
                        tl = true;
                    }
                    if (!noBottom && !noLeft && (tiles[i - 1, j - 1].Type == TileTypes.Floor || tiles[i - 1, j - 1].Type == TileTypes.StairDownBig || tiles[i - 1, j - 1].Type == TileTypes.StairDownSmall || tiles[i - 1, j - 1].Type == TileTypes.StairUpBig || tiles[i - 1, j - 1].Type == TileTypes.StairUpSmall))
                    {
                        bl = true;
                    }

                    if (trueNum == 0)
                    {
                        if (spriteAssignments.ContainsKey("WallCorner"))
                        {
                            //Create a top-right corner piece
                            if (tr)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                            if (br)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 270);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                            if (bl)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 180);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                            if (tl)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 90);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                        }
                    }
                    else if (trueNum == 1)
                    {
                        if (spriteAssignments.ContainsKey("WallCorner") && spriteAssignments.ContainsKey("WallEdgeOne"))
                        {
                            if (top)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallEdgeOne"][Random.Range(0, spriteAssignments["WallEdgeOne"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 270);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                                if (bl)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 180);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                                if (br)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 270);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                            if (bottom)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallEdgeOne"][Random.Range(0, spriteAssignments["WallEdgeOne"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 90);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                                if (tl)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 90);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                                if (tr)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                            if (left)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallEdgeOne"][Random.Range(0, spriteAssignments["WallEdgeOne"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                                if (tr)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                                if (br)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 270);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                            if (right)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallEdgeOne"][Random.Range(0, spriteAssignments["WallEdgeOne"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 180);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                                if (tl)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 90);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                                if (bl)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 180);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                        }
                    }
                    else if (trueNum == 2)
                    {
                        if (spriteAssignments.ContainsKey("WallCorner") && spriteAssignments.ContainsKey("WallEdgeTwo") && spriteAssignments.ContainsKey("WallEdgeOne"))
                        {
                            if (top && left)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallEdgeTwo"][Random.Range(0, spriteAssignments["WallEdgeTwo"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.position -= new Vector3(0, 0, 0.25f); 
                                if (br)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 270);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                            if (bottom && right)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallEdgeTwo"][Random.Range(0, spriteAssignments["WallEdgeTwo"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 180);
                                g.transform.position -= new Vector3(0, 0, 0.25f); 
                                if (tl)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 90);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                            if (right && top)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallEdgeTwo"][Random.Range(0, spriteAssignments["WallEdgeTwo"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 270);
                                g.transform.position -= new Vector3(0, 0, 0.25f); 
                                if (bl)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 180);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                            if (left && bottom)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallEdgeTwo"][Random.Range(0, spriteAssignments["WallEdgeTwo"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 90);
                                g.transform.position -= new Vector3(0, 0, 0.25f); 
                                if (tr)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["WallCorner"][Random.Range(0, spriteAssignments["WallCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                            if (top && bottom)
                            {
                                GameObject ga = CreateEdgeGameObject(spriteAssignments["WallEdgeOne"][Random.Range(0, spriteAssignments["WallEdgeOne"].Count)], tiles[i, j].transform.position);
                                ga.transform.parent = tiles[i, j].transform;
                                ga.transform.rotation = Quaternion.Euler(0, 0, 270);
                                ga.transform.position -= new Vector3(0, 0, 0.25f);

                                GameObject gb = CreateEdgeGameObject(spriteAssignments["WallEdgeOne"][Random.Range(0, spriteAssignments["WallEdgeOne"].Count)], tiles[i, j].transform.position);
                                gb.transform.parent = tiles[i, j].transform;
                                gb.transform.rotation = Quaternion.Euler(0, 0, 90);
                                gb.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                            if (left && right)
                            {
                                GameObject ga = CreateEdgeGameObject(spriteAssignments["WallEdgeOne"][Random.Range(0, spriteAssignments["WallEdgeOne"].Count)], tiles[i, j].transform.position);
                                ga.transform.parent = tiles[i, j].transform;
                                ga.transform.position -= new Vector3(0, 0, 0.25f);

                                GameObject gb = CreateEdgeGameObject(spriteAssignments["WallEdgeOne"][Random.Range(0, spriteAssignments["WallEdgeOne"].Count)], tiles[i, j].transform.position);
                                gb.transform.parent = tiles[i, j].transform;
                                gb.transform.rotation = Quaternion.Euler(0, 0, 180);
                                gb.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                        }
                    }
                    else if (trueNum == 3)
                    {
                        if (spriteAssignments.ContainsKey("WallEdgeThree"))
                        {
                            if (!top)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallEdgeThree"][Random.Range(0, spriteAssignments["WallEdgeThree"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 180);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                            if (!bottom)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallEdgeThree"][Random.Range(0, spriteAssignments["WallEdgeThree"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                            if (!left)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallEdgeThree"][Random.Range(0, spriteAssignments["WallEdgeThree"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 270);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                            if (!right)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["WallEdgeThree"][Random.Range(0, spriteAssignments["WallEdgeThree"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 90);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                        }
                    }
                    else if (trueNum == 4)
                    {
                        if (spriteAssignments.ContainsKey("WallEdgeFour"))
                        {
                            GameObject g = CreateEdgeGameObject(spriteAssignments["WallEdgeFour"][Random.Range(0, spriteAssignments["WallEdgeFour"].Count)], tiles[i, j].transform.position);
                            g.transform.parent = tiles[i, j].transform;
                            g.transform.position -= new Vector3(0, 0, 0.25f);
                        }
                    }

                    #endregion
                }
                else if(tiles[i, j].Type == TileTypes.Floor)
                {
                    #region Floor Check
                    //Check if there is a floor tile on the adjacent 4 sides
                    bool top = true; //true = floor here
                    bool bottom = true;
                    bool left = true;
                    bool right = true;

                    if (i == 0)
                    {
                        if (tiles[i + 1, j].Type != TileTypes.Wall) { right = false; }
                        left = false;
                    }
                    else if (i == tiles.GetLength(0) - 1)
                    {
                        if (tiles[i - 1, j].Type != TileTypes.Wall) { left = false; }
                        right = false;
                    }
                    else
                    {
                        if (tiles[i + 1, j].Type != TileTypes.Wall) { right = false; }
                        if (tiles[i - 1, j].Type != TileTypes.Wall) { left = false; }
                    }

                    if (j == 0)
                    {
                        if (tiles[i, j + 1].Type != TileTypes.Wall) { top = false; }
                        bottom = false;
                    }
                    else if (j == tiles.GetLength(1) - 1)
                    {
                        if (tiles[i, j - 1].Type != TileTypes.Wall) { bottom = false; }
                        top = false;
                    }
                    else
                    {
                        if (tiles[i, j + 1].Type != TileTypes.Wall) { top = false; }
                        if (tiles[i, j - 1].Type != TileTypes.Wall) { bottom = false; }
                    }

                    int trueNum = 0;
                    if (top) { trueNum++; }
                    if (bottom) { trueNum++; }
                    if (left) { trueNum++; }
                    if (right) { trueNum++; }

                    //Determine which diagonals will be checked
                    bool tl = false;
                    bool tr = false;
                    bool bl = false;
                    bool br = false;

                    bool noLeft = false;
                    bool noRight = false;
                    bool noTop = false;
                    bool noBottom = false;
                    //Special Checks
                    if (i == tiles.GetLength(0) - 1)
                    {
                        noRight = true;
                    }
                    if (i == 0)
                    {
                        noLeft = true;
                    }
                    if (j == tiles.GetLength(1) - 1)
                    {
                        noTop = true;
                    }
                    if (j == 0)
                    {
                        noBottom = true;
                    }

                    if (!noTop && !noRight && (tiles[i + 1, j + 1].Type == TileTypes.Wall))
                    {
                        tr = true;
                    }
                    if (!noBottom && !noRight && (tiles[i + 1, j - 1].Type == TileTypes.Wall))
                    {
                        br = true;
                    }
                    if (!noTop && !noLeft && (tiles[i - 1, j + 1].Type == TileTypes.Wall))
                    {
                        tl = true;
                    }
                    if (!noBottom && !noLeft && (tiles[i - 1, j - 1].Type == TileTypes.Wall))
                    {
                        bl = true;
                    }


                    if (trueNum == 0)
                    {
                        if (spriteAssignments.ContainsKey("FloorCorner"))
                        {
                            //Create a top-right corner piece
                            if (tr)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                            if (br)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 270);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                            if (bl)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 180);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                            if (tl)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 90);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                        }
                    }
                    else if (trueNum == 1)
                    {
                        if (spriteAssignments.ContainsKey("FloorCorner") && spriteAssignments.ContainsKey("FloorEdgeOne"))
                        {
                            if (top)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorEdgeOne"][Random.Range(0, spriteAssignments["FloorEdgeOne"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 270);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                                if (bl)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 180);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                                if (br)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 270);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                            if (bottom)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorEdgeOne"][Random.Range(0, spriteAssignments["FloorEdgeOne"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 90);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                                if (tl)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 90);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                                if (tr)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                            if (left)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorEdgeOne"][Random.Range(0, spriteAssignments["FloorEdgeOne"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                                if (tr)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                                if (br)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 270);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                            if (right)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorEdgeOne"][Random.Range(0, spriteAssignments["FloorEdgeOne"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 180);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                                if (tl)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 90);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                                if (bl)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 180);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                        }
                    }
                    else if (trueNum == 2)
                    {
                        if (spriteAssignments.ContainsKey("FloorCorner") && spriteAssignments.ContainsKey("FloorEdgeTwo") && spriteAssignments.ContainsKey("FloorEdgeOne"))
                        {
                            if (top && left)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorEdgeTwo"][Random.Range(0, spriteAssignments["FloorEdgeTwo"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                                if (br)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 270);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                            if (bottom && right)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorEdgeTwo"][Random.Range(0, spriteAssignments["FloorEdgeTwo"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 180);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                                if (tl)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 90);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                            if (right && top)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorEdgeTwo"][Random.Range(0, spriteAssignments["FloorEdgeTwo"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 270);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                                if (bl)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.rotation = Quaternion.Euler(0, 0, 180);
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                            if (left && bottom)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorEdgeTwo"][Random.Range(0, spriteAssignments["FloorEdgeTwo"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 90);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                                if (tr)
                                {
                                    GameObject gb = CreateEdgeGameObject(spriteAssignments["FloorCorner"][Random.Range(0, spriteAssignments["FloorCorner"].Count)], tiles[i, j].transform.position);
                                    gb.transform.parent = tiles[i, j].transform;
                                    gb.transform.position -= new Vector3(0, 0, 0.25f);
                                }
                            }
                            if (top && bottom)
                            {
                                GameObject ga = CreateEdgeGameObject(spriteAssignments["FloorEdgeOne"][Random.Range(0, spriteAssignments["FloorEdgeOne"].Count)], tiles[i, j].transform.position);
                                ga.transform.parent = tiles[i, j].transform;
                                ga.transform.rotation = Quaternion.Euler(0, 0, 270);
                                ga.transform.position -= new Vector3(0, 0, 0.25f);

                                GameObject gb = CreateEdgeGameObject(spriteAssignments["FloorEdgeOne"][Random.Range(0, spriteAssignments["FloorEdgeOne"].Count)], tiles[i, j].transform.position);
                                gb.transform.parent = tiles[i, j].transform;
                                gb.transform.rotation = Quaternion.Euler(0, 0, 90);
                                gb.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                            if (left && right)
                            {
                                GameObject ga = CreateEdgeGameObject(spriteAssignments["FloorEdgeOne"][Random.Range(0, spriteAssignments["FloorEdgeOne"].Count)], tiles[i, j].transform.position);
                                ga.transform.parent = tiles[i, j].transform;
                                ga.transform.position -= new Vector3(0, 0, 0.25f);

                                GameObject gb = CreateEdgeGameObject(spriteAssignments["FloorEdgeOne"][Random.Range(0, spriteAssignments["FloorEdgeOne"].Count)], tiles[i, j].transform.position);
                                gb.transform.parent = tiles[i, j].transform;
                                gb.transform.rotation = Quaternion.Euler(0, 0, 180);
                                gb.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                        }
                    }
                    else if (trueNum == 3)
                    {
                        if (spriteAssignments.ContainsKey("FloorEdgeThree"))
                        {
                            if (!top)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorEdgeThree"][Random.Range(0, spriteAssignments["FloorEdgeThree"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 180);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                            if (!bottom)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorEdgeThree"][Random.Range(0, spriteAssignments["FloorEdgeThree"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                            if (!left)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorEdgeThree"][Random.Range(0, spriteAssignments["FloorEdgeThree"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 270);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                            if (!right)
                            {
                                GameObject g = CreateEdgeGameObject(spriteAssignments["FloorEdgeThree"][Random.Range(0, spriteAssignments["FloorEdgeThree"].Count)], tiles[i, j].transform.position);
                                g.transform.parent = tiles[i, j].transform;
                                g.transform.rotation = Quaternion.Euler(0, 0, 90);
                                g.transform.position -= new Vector3(0, 0, 0.25f);
                            }
                        }
                    }
                    else if (trueNum == 4)
                    {
                        if (spriteAssignments.ContainsKey("FloorEdgeFour"))
                        {
                            GameObject g = CreateEdgeGameObject(spriteAssignments["FloorEdgeFour"][Random.Range(0, spriteAssignments["FloorEdgeFour"].Count)], tiles[i, j].transform.position);
                            g.transform.parent = tiles[i, j].transform;
                            g.transform.position -= new Vector3(0, 0, 0.25f);
                        }
                    }
                    #endregion
                }
            }
        }
    }

    private GameObject CreateEdgeGameObject(Sprite sprite, Vector3 pos)
    {
        GameObject edgeGraphic = Instantiate(tilePrefab);
        edgeGraphic.name = "EdgeGraphic";
        edgeGraphic.tag = "EdgeGraphic";
        DestroyImmediate(edgeGraphic.GetComponent<Tile>());
        edgeGraphic.AddComponent<TileEdge>();
        edgeGraphic.GetComponent<SpriteRenderer>().sprite = sprite;
        edgeGraphic.transform.position = pos;
        return edgeGraphic;
    }

    public void RotateDoorTiles()
    {
        //Check for above / below tile, if yes, rotate 90 degrees
        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                if (tiles[i, j].Type == TileTypes.Window || tiles[i, j].Type == TileTypes.Door || tiles[i, j].Type == TileTypes.TrappedDoor || tiles[i, j].Type == TileTypes.SecretDoor || tiles[i, j].Type == TileTypes.LockedDoor)
                {
                    if (j != tiles.GetLength(1) - 1 && j != 0)
                    {
                        if (tiles[i, j + 1].Type != TileTypes.Floor && tiles[i, j + 1].Type != TileTypes.StairDownBig && tiles[i, j + 1].Type != TileTypes.StairDownSmall && tiles[i, j + 1].Type != TileTypes.StairUpBig && tiles[i, j + 1].Type != TileTypes.StairUpSmall && tiles[i, j + 1].Type != TileTypes.Window)
                        {
                            tiles[i, j].transform.rotation = Quaternion.Euler(0, 0, 90);
                        }
                        else if (tiles[i, j - 1].Type != TileTypes.Floor && tiles[i, j - 1].Type != TileTypes.StairDownBig && tiles[i, j - 1].Type != TileTypes.StairDownSmall && tiles[i, j - 1].Type != TileTypes.StairUpBig && tiles[i, j - 1].Type != TileTypes.StairUpSmall && tiles[i, j - 1].Type != TileTypes.Window)
                        {
                            tiles[i, j].transform.rotation = Quaternion.Euler(0, 0, 90);
                        }
                    }
                }
            }
        }

    }

    /// <summary>
    /// Creates a shape for shadow casting along the walls of the dungeon
    /// </summary>
    public void CreateShadowObjects()
    {
        //Create primitive blocks
        if(GameObject.Find("Shadow Object"))
        {
            DestroyImmediate(GameObject.Find("Shadow Object"));
        }
        List<GameObject>  primitiveShadowShapes = new List<GameObject>();
        Dictionary<Vector2Int, GameObject> shadowAssignments = new Dictionary<Vector2Int, GameObject>();

        //Setup the lists that will tell us which tiles have been completed
        List<Vector2Int> notCompletedTiles = new List<Vector2Int>();

        foreach(Tile t in tiles)
        {
            t.shadowObject = null;
        }

        //Setup list of not completed tiles
        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                if (tiles[i, j].Type != TileTypes.Empty && tiles[i, j].Type != TileTypes.Floor && tiles[i, j].Type != TileTypes.Window && tiles[i, j].Type != TileTypes.StairDownBig && tiles[i, j].Type != TileTypes.StairDownSmall && tiles[i, j].Type != TileTypes.StairUpBig && tiles[i, j].Type != TileTypes.StairUpSmall)
                {
                    if (tiles[i, j].Type == TileTypes.Door || tiles[i, j].Type == TileTypes.LockedDoor || tiles[i, j].Type == TileTypes.SecretDoor || tiles[i, j].Type == TileTypes.TrappedDoor)
                    {
                        GameObject shadowObject = new GameObject("Shadow Object");

                        shadowAssignments.Add(new Vector2Int(i, j), shadowObject);

                        //Create the shadow shape based on the 4 points found
                        shadowObject.AddComponent<ComplexShape>();
                        //Upper Left Point
                        shadowObject.GetComponent<ComplexShape>().points.Add(new Vector2(tiles[i, j].GetComponent<SpriteRenderer>().bounds.min.x, tiles[i, j].GetComponent<SpriteRenderer>().bounds.max.y));
                        //Upper Right Point
                        shadowObject.GetComponent<ComplexShape>().points.Add(new Vector2(tiles[i, j].GetComponent<SpriteRenderer>().bounds.max.x, tiles[i, j].GetComponent<SpriteRenderer>().bounds.max.y));
                        //Bottom Right Point
                        shadowObject.GetComponent<ComplexShape>().points.Add(new Vector2(tiles[i, j].GetComponent<SpriteRenderer>().bounds.max.x, tiles[i, j].GetComponent<SpriteRenderer>().bounds.min.y));
                        //Bottom Left Point
                        shadowObject.GetComponent<ComplexShape>().points.Add(new Vector2(tiles[i, j].GetComponent<SpriteRenderer>().bounds.min.x, tiles[i, j].GetComponent<SpriteRenderer>().bounds.min.y));

                        primitiveShadowShapes.Add(shadowObject);
                    }
                    else
                    {
                        notCompletedTiles.Add(new Vector2Int(i, j));
                    }
                }
            }
        }

        Vector2Int currentTile = new Vector2Int();
        TileTypes currentTileType = TileTypes.Empty;

        while (notCompletedTiles.Count > 0)
        {
            for (int i = 0; i < tiles.GetLength(0); i++) //y for tiles
            {
                for (int j = 0; j < tiles.GetLength(1); j++) //x for tiles
                {
                    if (notCompletedTiles.Contains(new Vector2Int(i, j)))
                    {
                        GameObject shadowObject = new GameObject("Shadow Object");
                        //WHETHER WE SHOULD CHECK DOWN AND RIGHT OR RIGHT THEN DOWN WILL DEPEND ON I J ORDER
                        currentTile = new Vector2Int(i, j);
                        if (shadowAssignments.ContainsKey(new Vector2Int(currentTile.x, currentTile.y)))
                        {
                            shadowAssignments[new Vector2Int(currentTile.x, currentTile.y)] = shadowObject;
                        }
                        else
                        {
                            shadowAssignments.Add(new Vector2Int(currentTile.x, currentTile.y), shadowObject);
                        }
                        currentTileType = tiles[i, j].Type;
                        notCompletedTiles.Remove(currentTile);
                        int furthestLeft = currentTile.x;
                        int furthestUp = currentTile.y;

                        //Check Right
                        while (notCompletedTiles.Contains(new Vector2Int(currentTile.x + 1, j)) && currentTile.x < tiles.GetLength(0) - 1 && tiles[currentTile.x + 1, j].Type == currentTileType)
                        {
                            currentTile = new Vector2Int(currentTile.x + 1, currentTile.y);
                            if (shadowAssignments.ContainsKey(new Vector2Int(currentTile.x, currentTile.y)))
                            {
                                shadowAssignments[new Vector2Int(currentTile.x, currentTile.y)] = shadowObject;
                            }
                            else
                            {
                                shadowAssignments.Add(new Vector2Int(currentTile.x, currentTile.y), shadowObject);
                            }
                            notCompletedTiles.Remove(currentTile);
                        }
                        //Check Down
                        int furthestRight = currentTile.x;

                        List<Vector2Int> possibleRemovals = new List<Vector2Int>();

                        while (notCompletedTiles.Contains(new Vector2Int(furthestRight, currentTile.y + 1)) && currentTile.y < tiles.GetLength(1) - 1 && tiles[furthestRight, currentTile.y + 1].Type == currentTileType)
                        {
                            //Check all left at this new y value
                            currentTile.y += 1;
                            if (shadowAssignments.ContainsKey(new Vector2Int(currentTile.x, currentTile.y)))
                            {
                                shadowAssignments[new Vector2Int(currentTile.x, currentTile.y)] = shadowObject;
                            }
                            else
                            {
                                shadowAssignments.Add(new Vector2Int(currentTile.x, currentTile.y), shadowObject);
                            }
                            possibleRemovals.Add(new Vector2Int(currentTile.x, currentTile.y));
                            while (notCompletedTiles.Contains(new Vector2Int(currentTile.x - 1, currentTile.y)) && currentTile.x > furthestLeft && tiles[currentTile.x - 1, currentTile.y].Type == currentTileType)
                            {
                                currentTile.x -= 1;
                                if (shadowAssignments.ContainsKey(new Vector2Int(currentTile.x, currentTile.y)))
                                {
                                    shadowAssignments[new Vector2Int(currentTile.x, currentTile.y)] = shadowObject;
                                }
                                else
                                {
                                    shadowAssignments.Add(new Vector2Int(currentTile.x, currentTile.y), shadowObject);
                                }
                                possibleRemovals.Add(new Vector2Int(currentTile.x, currentTile.y));
                            }
                            //The current line was NOT succesfully made
                            if (currentTile.x != furthestLeft)
                            {
                                currentTile.y -= 1;
                                if (shadowAssignments.ContainsKey(new Vector2Int(currentTile.x, currentTile.y)))
                                {
                                    shadowAssignments[new Vector2Int(currentTile.x, currentTile.y)] = shadowObject;
                                }
                                else
                                {
                                    shadowAssignments.Add(new Vector2Int(currentTile.x, currentTile.y), shadowObject);
                                }
                                //Remove unneeded values from the possibleRemovals list
                                for (int c = 0; c <= Mathf.Abs(currentTile.x - furthestRight); c++)
                                {
                                    possibleRemovals.RemoveAt(possibleRemovals.Count - 1);
                                }
                                break;
                            }
                            currentTile.x = furthestRight;
                        }
                        foreach (Vector2Int v in possibleRemovals)
                        {
                            notCompletedTiles.Remove(v);
                        }

                        int furthestDown = currentTile.y;

                        //Create the shadow shape based on the 4 points found
                        shadowObject.AddComponent<ComplexShape>();
                        //Upper Left Point
                        shadowObject.GetComponent<ComplexShape>().points.Add(new Vector2(tiles[furthestLeft, furthestDown].GetComponent<SpriteRenderer>().bounds.min.x, tiles[furthestLeft, furthestDown].GetComponent<SpriteRenderer>().bounds.max.y));
                        //Upper Right Point
                        shadowObject.GetComponent<ComplexShape>().points.Add(new Vector2(tiles[furthestRight, furthestDown].GetComponent<SpriteRenderer>().bounds.max.x, tiles[furthestRight, furthestDown].GetComponent<SpriteRenderer>().bounds.max.y));
                        //Bottom Right Point
                        shadowObject.GetComponent<ComplexShape>().points.Add(new Vector2(tiles[furthestRight, furthestUp].GetComponent<SpriteRenderer>().bounds.max.x, tiles[furthestRight, furthestUp].GetComponent<SpriteRenderer>().bounds.min.y));
                        //Bottom Left Point
                        shadowObject.GetComponent<ComplexShape>().points.Add(new Vector2(tiles[furthestLeft, furthestUp].GetComponent<SpriteRenderer>().bounds.min.x, tiles[furthestLeft, furthestUp].GetComponent<SpriteRenderer>().bounds.min.y));

                        primitiveShadowShapes.Add(shadowObject);
                    }
                }
            }
        }

        foreach (KeyValuePair<Vector2Int, GameObject> g in shadowAssignments)
        {
            tiles[g.Key.x, g.Key.y].shadowObject = g.Value;
        }
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(0);
        SceneManager.UnloadSceneAsync(1);
    }

    #endregion
}
