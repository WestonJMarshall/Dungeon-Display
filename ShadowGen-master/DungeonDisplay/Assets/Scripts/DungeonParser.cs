using System.Collections;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class DungeonParser : MonoBehaviour
{
    Dungeon dungeon;

    [SerializeField, Tooltip("The tile size in world units")]
    private float gridSize = 0.0f;
    [SerializeField]
    private GameObject tilePrefab = null;

    private Tile[,] tiles;

    private List<GameObject> primitiveShadowShapes;
    Dictionary<Vector2Int, GameObject> shadowAssignments;

    #region Unity Functions

    private void Awake()
    {
        tiles = new Tile[0,0];
        shadowAssignments = new Dictionary<Vector2Int, GameObject>();
    }

    private void Start()
    {
        Manager manager = Manager.Instance;
        manager.FindElementsToManage();
        manager.AssignShapes();

        if (DungeonFile.fromSaveFile)
        {
            string[] saveData = File.ReadAllLines(DungeonFile.path);
            string filePath = saveData[0];
            string fileType = saveData[1];
            if (fileType == "TSV") { DungeonFile.fileType = FileType.TSV; }
            else if (fileType == "PixelMap") { DungeonFile.fileType = FileType.PixelMap; }
            else if (fileType == "FreeFormMap") { DungeonFile.fileType = FileType.FreeFormMap; }

            tiles = new Tile[0, 0];
            if (File.Exists(filePath))
            {
                switch (DungeonFile.fileType)
                {
                    case FileType.TSV:
                        SetupDefaultDonJonKey(DungeonFile.donjonKey);

                        Dictionary<string, int> dunjonParserKey = new Dictionary<string, int>();

                        //Parser key for DonJon dungeons
                        for (int i = 0; i < DungeonFile.donjonKey.Count; i++)
                        {
                            dunjonParserKey.Add(DungeonFile.donjonKey[i], i);
                        }

                        DonJonParser("DonJon Dungeon", LoadDunJonFile(filePath), dunjonParserKey);
                        break;
                    case FileType.PixelMap:
                        Dictionary<UnityEngine.Color, int> pixelParserKey = new Dictionary<UnityEngine.Color, int>();

                        for (int i = 0; i < 20; i++)
                        {
                            pixelParserKey.Add(new UnityEngine.Color((float)Random.Range(0,1000) / 1000.0f, (float)Random.Range(0, 1000) / 1000.0f, (float)Random.Range(0, 1000) / 1000.0f), i);
                        }

                        Texture2D pixelMap = new Texture2D(32, 32);
                        pixelMap.LoadImage(File.ReadAllBytes(filePath));

                        PixelParser("Pixel Dungeon", pixelMap, pixelParserKey);
                        break;
                    case FileType.FreeFormMap:
                        FileStream s = new FileStream(filePath, FileMode.Open);
                        Bitmap image = new Bitmap(s);
                        int textureWidth = image.Width;
                        int textureHeight = image.Height;
                        s.Close();
                        Texture2D texture = new Texture2D(textureWidth, textureHeight);
                        texture.LoadImage(File.ReadAllBytes(filePath));

                        DungeonFile.freeFormSize = int.Parse(saveData[2]);
                        FreeFormParser("Freeform Dungeon", texture);
                        break;
                }

                SetupTilesForSaveFiles();

                CreateShadowObjects();

                foreach (GameObject g in primitiveShadowShapes)
                {
                    g.GetComponent<ComplexShape>().GenerateBoundingSphere();
                }

                manager.FindElementsToManage();
                manager.AssignShapes();

                if (DungeonFile.fileType == FileType.FreeFormMap)
                {
                    manager.ApplyBackgroundMap(filePath, DungeonFile.freeFormPixelsPerInch);
                    Manager.Instance.StartCoroutine("PrepareFreeFormMap");
                }

                Manager.Instance.StartCoroutine("LoadMap");
            }
        }
        else
        {
            tiles = new Tile[0, 0];
            if (File.Exists(DungeonFile.path))
            {
                switch (DungeonFile.fileType)
                {
                    case FileType.TSV:
                        SetupDefaultDonJonKey(DungeonFile.donjonKey);

                        Dictionary<string, int> dunjonParserKey = new Dictionary<string, int>();

                        //Parser key for DonJon dungeons
                        for (int i = 0; i < DungeonFile.donjonKey.Count; i++)
                        {
                            dunjonParserKey.Add(DungeonFile.donjonKey[i], i);
                        }

                        DonJonParser("DonJon Dungeon", LoadDunJonFile(DungeonFile.path), dunjonParserKey);
                        break;
                    case FileType.PixelMap:
                        Dictionary<UnityEngine.Color, int> pixelParserKey = new Dictionary<UnityEngine.Color, int>();

                        for (int i = 0; i < DungeonFile.pixelKey.Count; i++)
                        {
                            pixelParserKey.Add(DungeonFile.pixelKey[i], i);
                        }

                        Texture2D pixelMap = new Texture2D(32, 32);
                        pixelMap.LoadImage(File.ReadAllBytes(DungeonFile.path));

                        PixelParser("Pixel Dungeon", pixelMap, pixelParserKey);
                        break;
                    case FileType.FreeFormMap:
                        FileStream s = new FileStream(DungeonFile.path, FileMode.Open);
                        Bitmap image = new Bitmap(s);
                        int textureWidth = image.Width;
                        int textureHeight = image.Height;
                        s.Close();
                        Texture2D texture = new Texture2D(textureWidth, textureHeight);
                        texture.LoadImage(File.ReadAllBytes(DungeonFile.path));

                        FreeFormParser("Freeform Dungeon", texture);
                        break;
                }

                SetupTiles();
                CreateShadowObjects();

                foreach (GameObject g in primitiveShadowShapes)
                {
                    g.GetComponent<ComplexShape>().GenerateBoundingSphere();
                }

                manager.FindElementsToManage();
                manager.AssignShapes();

                if (DungeonFile.fileType == FileType.FreeFormMap)
                {
                    manager.ApplyBackgroundMap(DungeonFile.path, DungeonFile.freeFormPixelsPerInch);
                    Manager.Instance.StartCoroutine("PrepareFreeFormMap");
                }
                else
                {
                    Manager.Instance.StartCoroutine("PrepareMap");
                }
            }
            else
            {
                //Debug.LogError("No file selected, a file must be selected in the file selection scene or otherwise set up before a dungeon can be loaded.");
                List<string> tempKey = new List<string>();
                SetupDefaultDonJonKey(tempKey);

                Dictionary<string, int> dunjonParserKey = new Dictionary<string, int>();

                //Parser key for DonJon dungeons
                for (int i = 0; i < tempKey.Count; i++)
                {
                    dunjonParserKey.Add(tempKey[i], i);
                }

                DonJonParser("Test Dungeon", LoadDunJonFile("FullDonJonMap.txt"), dunjonParserKey);

                SetupTiles();
                CreateShadowObjects();

                foreach (GameObject g in primitiveShadowShapes)
                {
                    g.GetComponent<ComplexShape>().GenerateBoundingSphere();
                }

                manager.FindElementsToManage();
                manager.AssignShapes();
            }
        }
    }

    #endregion

    #region Shape Construction

    /// <summary>
    /// Creates a shape for shadow casting along the walls of the dungeon
    /// </summary>
    public void CreateShadowObjects()
    {
        //Create primitive blocks
        primitiveShadowShapes = new List<GameObject>();

        //Setup the lists that will tell us which tiles have been completed
        List<Vector2Int> notCompletedTiles = new List<Vector2Int>();

        //Setup list of not completed tiles
        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                if(tiles[i, j].Type != TileTypes.Empty && tiles[i, j].Type != TileTypes.Floor && tiles[i, j].Type != TileTypes.Window && tiles[i, j].Type != TileTypes.StairDownBig && tiles[i, j].Type != TileTypes.StairDownSmall && tiles[i, j].Type != TileTypes.StairUpBig && tiles[i, j].Type != TileTypes.StairUpSmall)
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
                            while(notCompletedTiles.Contains(new Vector2Int(currentTile.x - 1, currentTile.y)) && currentTile.x > furthestLeft && tiles[currentTile.x - 1, currentTile.y].Type == currentTileType)
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
                        foreach(Vector2Int v in possibleRemovals)
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

    public void ClearTiles()
    {
        for(int i = 0; i < tiles.GetLength(0); i++)
        {
            for(int j = 0; j < tiles.GetLength(1); j++)
            {
                Destroy(tiles[i, j].gameObject);
            }
        }
    }

    public void SetupTiles()
    {
        ClearTiles();

        tiles = new Tile[dungeon.tiles.GetLength(0), dungeon.tiles.GetLength(1)];
        Vector3 topCorner = new Vector3(tiles.GetLength(0), tiles.GetLength(1), 0) * -0.5f * gridSize;

        for(int i = 0; i < tiles.GetLength(0); i++)
        {
            for(int j = 0; j < tiles.GetLength(1); j++)
            {
                Vector3 tileOffset = new Vector3(i, j, 0) * gridSize;
                GameObject tile = Instantiate(tilePrefab, topCorner + tileOffset, Quaternion.identity);
                if(dungeon.tiles[i, j] != (int)TileTypes.Floor)
                {
                    tile.transform.position += new Vector3(0.0f, 0.0f, -1.5f);
                }
                else
                {
                    tile.transform.position += new Vector3(0.0f, 0.0f, -0.5f);
                }
                tile.transform.localScale = new Vector3(gridSize, gridSize, 1.0f);
                tiles[i, j] = tile.GetComponent<Tile>();
                tiles[i, j].index = new Vector2Int(i, j);
                tiles[i, j].Type = (TileTypes)dungeon.tiles[i, j];
            }
        }

        Manager.Instance.tiles = tiles;
        Manager.Instance.FindElementsToManage();
    }

    public void SetupTilesForSaveFiles()
    {
        ClearTiles();

        tiles = new Tile[dungeon.tiles.GetLength(0), dungeon.tiles.GetLength(1)];
        Vector3 topCorner = new Vector3(tiles.GetLength(0), tiles.GetLength(1), 0) * -0.5f * gridSize;

        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                Vector3 tileOffset = new Vector3(i, j, 0) * gridSize;
                GameObject tile = Instantiate(tilePrefab, topCorner + tileOffset, Quaternion.identity);
                if (dungeon.tiles[i, j] != (int)TileTypes.Floor)
                {
                    tile.transform.position += new Vector3(0.0f, 0.0f, -1.5f);
                }
                else
                {
                    tile.transform.position += new Vector3(0.0f, 0.0f, -0.5f);
                }
                tile.transform.localScale = new Vector3(gridSize, gridSize, 1.0f);
                tiles[i, j] = tile.GetComponent<Tile>();
                tiles[i, j].index = new Vector2Int(i, j);
                tiles[i, j].Type = TileTypes.Wall;
            }
        }

        Manager.Instance.tiles = tiles;
        Manager.Instance.FindElementsToManage();
    }

    #endregion

    #region Parsers

    /// <summary>
    /// Parses a text file into a Dungeon struct based on the given key
    /// </summary>
    /// <param name="name">The name to assign to the parsed dungeon</param>
    /// <param name="textFile"></param>
    /// <param name="key">The key to use to assign tile types based on the string</param>
    /// <returns>The parsed Dungeon</returns>
    public void DonJonParser(string name, string[,] textFile, Dictionary<string, int> key)
    {
        dungeon.name = name;
        dungeon.tiles = new int[textFile.GetLength(1), textFile.GetLength(0)];

        for (int i = 0; i < textFile.GetLength(0); i++)
        {
            for (int j = 0; j < textFile.GetLength(1); j++)
            {
                string current = textFile[i,j];
                if(current == null) { current = "W"; }
                if (key.ContainsKey(current))
                {
                    dungeon.tiles[j, textFile.GetLength(0) - 1 - i] = key[current];
                }
                else
                {
                    dungeon.tiles[j, textFile.GetLength(0) - 1 - i] = (int)(TileTypes.Empty);
                }
            }
        }
    }

    /// <summary>
    /// Parses a pixel image into a Dungeon struct based on the given key
    /// </summary>
    /// <param name="name">The name to assign to the parsed dungeon</param>
    /// <param name="image">The image to parse</param>
    /// <param name="key">The key to use to assign tile types based on pixel color</param>
    /// <returns>The parsed Dungeon</returns>
    public void PixelParser(string name, Texture2D image, Dictionary<UnityEngine.Color, int> key)
    {
        dungeon.name = name;
        dungeon.tiles = new int[image.width, image.height];

        List<UnityEngine.Color> keyColors = new List<UnityEngine.Color>();
        foreach(KeyValuePair<UnityEngine.Color, int> pair in key)
        {
            keyColors.Add(pair.Key);
        }

        for(int i = 0; i < image.height; i++)
        {
            for(int j = 0; j < image.width; j++)
            {
                UnityEngine.Color current = image.GetPixel(j, i);

                if (key.ContainsKey(current))
                {
                    dungeon.tiles[j, i] = key[current];
                }
                else
                {
                    dungeon.tiles[j, i] = key[closestColor(current, keyColors)];
                }
            }
        }
    }

    /// <summary>
    /// Parses a pixel image into a Dungeon struct based on the given key
    /// </summary>
    /// <param name="name">The name to assign to the parsed dungeon</param>
    /// <param name="image">The image to parse</param>
    /// <param name="key">The key to use to assign tile types based on pixel color</param>
    /// <returns>The parsed Dungeon</returns>
    public void FreeFormParser(string name, Texture2D image)
    {
        dungeon.name = name;

        if(DungeonFile.freeFormSize == 0) { DungeonFile.freeFormSize = 16; }

        float ratio = (float)image.height / (float)image.width;
        int y = (int)((float)DungeonFile.freeFormSize * (float)ratio);
        DungeonFile.freeFormPixelsPerInch = image.width / DungeonFile.freeFormSize;

        dungeon.tiles = new int[DungeonFile.freeFormSize, y];

        for (int i = 0; i < dungeon.tiles.GetLength(0); i++)
        {
            for (int j = 0; j < dungeon.tiles.GetLength(1); j++)
            {
                dungeon.tiles[i, j] = 1;
            }
        }
    }

    #endregion

    #region Helper Functions

    /// <summary>
    /// Finds the closest color in the array to the given color
    /// </summary>
    /// <param name="value">The color to check for</param>
    /// <param name="keys">The color to match with</param>
    /// <returns>The color in the array closest to the given color</returns>
    public UnityEngine.Color closestColor(UnityEngine.Color value, List<UnityEngine.Color> keys)
    {
        float minDistance = Mathf.Abs(value.r - keys[0].r) + Mathf.Abs(value.g - keys[0].g) + Mathf.Abs(value.b - keys[0].b) + Mathf.Abs(value.a - keys[0].a);
        int minIndex = 0;

        for(int i = 1; i < keys.Count; i++)
        {
            float currentDistance = Mathf.Abs(value.r - keys[i].r) + Mathf.Abs(value.g - keys[i].g) + Mathf.Abs(value.b - keys[i].b) + Mathf.Abs(value.a - keys[i].a);

            if (currentDistance < minDistance)
            {
                minIndex = i;
                minDistance = currentDistance;
            }
        }

        return keys[minIndex];
    }

    public void SetupDefaultDonJonKey(List<string> donjonKey)
    {
        if (donjonKey != null)
        {
            donjonKey.Clear();

            //0 - Empty
            donjonKey.Add("E");
            //1 - Floor
            donjonKey.Add("F");
            //2 - Wall
            donjonKey.Add("W");
            //3 - Door
            donjonKey.Add("D");
            //4 - Locked Door
            donjonKey.Add("L");
            //5 - Trapped Door
            donjonKey.Add("T");
            //6 - Secret Door
            donjonKey.Add("S");
            //7 - Window
            donjonKey.Add("M");
            //8 - StairDownSmall
            donjonKey.Add("1");
            //9 - StairDownBig
            donjonKey.Add("2");
            //10 - StairUpSmall
            donjonKey.Add("3");
            //11 - StairUpBig
            donjonKey.Add("4");

            donjonKey.Add("a");
            donjonKey.Add("b");
            donjonKey.Add("c");
            donjonKey.Add("d");
            donjonKey.Add("e");
            donjonKey.Add("f");
            donjonKey.Add("g");
            donjonKey.Add("h");
        }
    }

    public string[,] LoadDunJonFile(string dungeonFile)
    {
        //Create the array
        StreamReader streamReader;
        try
        {
            streamReader = new StreamReader(dungeonFile);
        }
        catch
        {
            return new string[0, 0];
        }
        List<List<char>> formatedFile = new List<List<char>>();
        List<char> formatedFileLine = new List<char>();
        string current;
        for (int i = 0; true; i++)
        {
            current = streamReader.ReadLine();
            if(current == null) { break; }
            for(int j = 0; j < current.Length; j++)
            {
                if(j == 0) { formatedFileLine.Add('W'); }
                if (j == current.Length - 1) { formatedFileLine.Add('W'); }
                else if (current[j] == '\t')
                {
                    //Add whatever is after the tab to the list
                    if (current[j + 1] == '\t')
                    {
                        formatedFileLine.Add('W');
                    }
                    else
                    {
                        if(current[j + 1] == 'D')
                        {
                            if (current[j + 2] == 'R')
                            {
                                formatedFileLine.Add('D');
                            }
                            else if(current[j + 2] == 'L')
                            {
                                formatedFileLine.Add('L');
                            }
                            else if (current[j + 2] == 'T')
                            {
                                formatedFileLine.Add('T');
                            }
                            else if (current[j + 2] == 'S')
                            {
                                formatedFileLine.Add('S');
                            }
                            else if (current[j + 2] == 'B')
                            {
                                formatedFileLine.Add('T');
                            }
                            else if (current[j + 2] == 'P')
                            {
                                formatedFileLine.Add('D');
                            }
                        }
                        else if (current[j + 1] == 'S')
                        {
                            if (current[j + 2] == 'D')
                            {
                                if (current[j + 3] == 'D')
                                {
                                    formatedFileLine.Add('2');
                                }
                                else
                                {
                                    formatedFileLine.Add('1');
                                }
                            }
                            else if (current[j + 2] == 'U')
                            {
                                if (current[j + 3] == 'U')
                                {
                                    formatedFileLine.Add('4');
                                }
                                else
                                {
                                    formatedFileLine.Add('3');
                                }
                            }
                        }
                        else if(current[j + 1] == 'F')
                        {
                            formatedFileLine.Add('F');
                        }
                        else
                        {
                            formatedFileLine.Add('W');
                        }
                    }
                }
            }
            formatedFile.Add(new List<char>(formatedFileLine));
            formatedFileLine = new List<char>();
        }

        string[,] dungeon = new string[formatedFile.Count, formatedFile[0].Count];
        
        streamReader = new StreamReader(dungeonFile);
        for (int i = 0; i < formatedFile.Count; i++)
        {
            for (int j = 0; j < formatedFile[0].Count; j++)
            {
                dungeon[i, j] = formatedFile[i][j].ToString();
            }
        }
        streamReader.Close();

        InfoLogCanvasScript.SendInfoMessage(dungeon.Length + " Tiles Loaded", UnityEngine.Color.black, 10);

        return dungeon;
    }

    #endregion
}
