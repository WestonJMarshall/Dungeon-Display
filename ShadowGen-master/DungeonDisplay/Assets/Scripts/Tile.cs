using UnityEngine;
using UnityEngine.EventSystems;
using System.Drawing;
using System.IO;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class Tile : MonoBehaviour
{
    [SerializeField]
    private TileTypes type;
    [SerializeField]
    private Sprite[] tileSprites = null;

    public SpriteRenderer spriteRenderer;
    public BoxCollider2D boxCollider;
    public GameObject shadowObject;
    public Vector2Int index;
    public string tileSpritePath = "null";

    #region Properties

    /// <summary>
    /// The type of object that this tile is
    /// </summary>
    public TileTypes Type
    {
        get { return type; }
        set {
            type = value;
            SelectSprite();
        }
    }

    #endregion

    #region Unity Functions

    private void Awake()
    {
        tileSpritePath = "null";
        spriteRenderer = GetComponent<SpriteRenderer>();
        gameObject.AddComponent<BoxCollider2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
    }

    public void SelectSprite()
    {
        //Assign Sprites
        spriteRenderer.sprite = tileSprites[(int)type];

        float depth = 0;

        switch (Type)
        {
            case TileTypes.Door:
            case TileTypes.Wall:
            case TileTypes.LockedDoor:
            case TileTypes.SecretDoor:
            case TileTypes.TrappedDoor:
            case TileTypes.Window:
                depth = 1;
                break;
            default:
                depth = 1;
                break;
        }

        transform.position = new Vector3(transform.position.x, transform.position.y, depth);
    }

    /// <summary>
    /// Selects the correct sprite based on the tiles type
    /// </summary>
    public void SelectSpriteFromFile()
    {
        //Assign Sprites
        string iconPath = @"GameLoadedAssets\Sprites\TileSets\Default\";
        switch (Type)
        {
            case TileTypes.Door:
                iconPath += "Door";
                break;
            case TileTypes.Wall:
                iconPath += "Wall";
                break;
            case TileTypes.Floor:
                iconPath += "Floor";
                break;
            case TileTypes.LockedDoor:
                iconPath += "LockedDoor";
                break;
            case TileTypes.SecretDoor:
                iconPath += "SecretDoor";
                break;
            case TileTypes.TrappedDoor:
                iconPath += "TrappedDoor";
                break;
            case TileTypes.Window:
                iconPath += "Window";
                break;
            case TileTypes.StairDownBig:
                iconPath += "StairDownBig";
                break;
            case TileTypes.StairDownSmall:
                iconPath += "StairDownSmall";
                break;
            case TileTypes.StairUpBig:
                iconPath += "StairUpBig";
                break;
            case TileTypes.StairUpSmall:
                iconPath += "StairUpSmall";
                break;
            default:
                iconPath += "Empty";
                break;
        }

        if (File.Exists(iconPath + ".png"))
        {
            iconPath += ".png";
        }
        else
        {
            iconPath += ".jpg";
        }

        tileSpritePath = iconPath;

        //Load & assign sprite
        FileStream s = new FileStream(iconPath, FileMode.Open);
        Bitmap image = new Bitmap(s);
        int textureWidth = image.Width;
        int textureHeight = image.Height;
        s.Close();
        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        texture.LoadImage(File.ReadAllBytes(iconPath));
        Sprite sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width);
        GetComponent<SpriteRenderer>().sprite = sprite;
    }

    public void SelectSpriteFromFile(string iconPath)
    {
        //Assign Sprites
        switch (Type)
        {
            case TileTypes.Door:
                iconPath += "Door";
                break;
            case TileTypes.Wall:
                iconPath += "Wall";
                break;
            case TileTypes.Floor:
                iconPath += "Floor";
                break;
            case TileTypes.LockedDoor:
                iconPath += "LockedDoor";
                break;
            case TileTypes.SecretDoor:
                iconPath += "SecretDoor";
                break;
            case TileTypes.TrappedDoor:
                iconPath += "TrappedDoor";
                break;
            case TileTypes.Window:
                iconPath += "Window";
                break;
            case TileTypes.StairDownBig:
                iconPath += "StairDownBig";
                break;
            case TileTypes.StairDownSmall:
                iconPath += "StairDownSmall";
                break;
            case TileTypes.StairUpBig:
                iconPath += "StairUpBig";
                break;
            case TileTypes.StairUpSmall:
                iconPath += "StairUpSmall";
                break;
            default:
                iconPath += "Empty";
                break;
        }

        if (File.Exists(iconPath + ".png"))
        {
            iconPath += ".png";
        }
        else
        {
            iconPath += ".jpg";
        }

        tileSpritePath = iconPath;

        //Load & assign sprite
        FileStream s = new FileStream(iconPath, FileMode.Open);
        Bitmap image = new Bitmap(s);
        int textureWidth = image.Width;
        int textureHeight = image.Height;
        s.Close();
        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        texture.LoadImage(File.ReadAllBytes(iconPath));
        Sprite sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width);
        GetComponent<SpriteRenderer>().sprite = sprite;
    }

    private void OnMouseDown()
    {
        if (!NetworkingManager.Instance.host) { return; }
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            Manager.Instance.inspectingItem = gameObject;
            Manager.Instance.UpdateInspector();
        }
    }

    #endregion
}
