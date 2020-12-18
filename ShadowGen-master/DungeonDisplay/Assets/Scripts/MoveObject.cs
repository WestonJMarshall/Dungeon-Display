using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;
using System.Drawing;
using System.Text;

public class MoveObject : MonoBehaviour
{
    public bool isPicked;
    public Tile gridSnappedTile;
    Manager manager;
    public bool lightObject = true;
    public string spritePath = "null";

    public bool networkReadyForUpdate = true;

    GameObject canvas;

    private void Awake()
    {
        manager = Manager.Instance;
        canvas = GameObject.Find("Canvas");
    }

    public void AssignSprite()
    {
        try
        {
            string iconPath = @"GameLoadedAssets\Sprites\Icons\Default.png";
            spritePath = @"GameLoadedAssets\Sprites\Icons\Default.png";
            //Load & assign sprite
            FileStream s = new FileStream(iconPath, FileMode.Open);
            Bitmap image = new Bitmap(s);
            int textureWidth = image.Width;
            int textureHeight = image.Height;
            s.Close();
            Texture2D texture = new Texture2D(textureWidth, textureHeight);
            texture.LoadImage(File.ReadAllBytes(iconPath));
            Sprite sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width);
            float mod = 100.0f / texture.width;
            GetComponentInChildren<SpriteRenderer>().sprite = sprite;
        }
        catch { }
    }

    public void AssignSprite(string iconPath)
    {
        //Load & assign sprite
        spritePath = iconPath;
        FileStream s = new FileStream(iconPath, FileMode.Open);
        Bitmap image = new Bitmap(s);
        int textureWidth = image.Width;
        int textureHeight = image.Height;
        s.Close();
        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        texture.LoadImage(File.ReadAllBytes(iconPath));
        Sprite sprite = Sprite.Create(texture, new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), new Vector2(0.5f, 0.5f), texture.width);
        float mod = 100.0f / texture.width;
        GetComponentInChildren<SpriteRenderer>().sprite = sprite;
    }

    private void Update()
    {
        if(GetComponentInChildren<CustomLight>() == null)
        {
            lightObject = false;
            gameObject.transform.GetChild(0).position = new Vector3(gameObject.transform.GetChild(0).position.x, gameObject.transform.GetChild(0).position.y, 0.3f);
        }
        if (manager == null) { manager = Manager.Instance; }

        //Check if it was clicked on
        if (Input.GetMouseButtonUp(0))
        {
            isPicked = false;
            GetComponentInChildren<SpriteRenderer>().color = UnityEngine.Color.white;
        }

        //Move it
        if (isPicked)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Tile closestTile = manager.tiles[0, 0];
            foreach (Tile t in manager.tiles)
            {
                if (Vector2.Distance(t.transform.position, transform.position) < Vector2.Distance(closestTile.transform.position, transform.position))
                {
                    closestTile = t;
                }
            }
            gridSnappedTile = closestTile;

            if (manager.gridSnap)
            {
                foreach (Tile t in manager.tiles)
                {
                    if (gridSnappedTile != null)
                    {
                        if (t != gridSnappedTile)
                        {
                            if (Vector2.Distance(mousePos, t.GetComponent<SpriteRenderer>().bounds.center) < t.GetComponent<SpriteRenderer>().bounds.extents.magnitude / 1.55f && Vector2.Distance(transform.position, t.GetComponent<SpriteRenderer>().bounds.center) > t.GetComponent<SpriteRenderer>().bounds.extents.magnitude / 3.0f)
                            {
                                if (t.Type != TileTypes.Wall && t.Type != TileTypes.Window)
                                {
                                    if (t.shadowObject != null)
                                    {
                                        ComplexShape shadow;
                                        if (t.shadowObject.TryGetComponent(out shadow))
                                        {
                                            if (!shadow.functional)
                                            {
                                                if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
                                                {
                                                    if (!networkReadyForUpdate) { return; }
                                                    networkReadyForUpdate = false;
                                                    if (lightObject)
                                                    {
                                                        NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"opc{transform.position.x}|{transform.position.y}|1|{t.index.x}|{t.index.y}"));
                                                    }
                                                    else
                                                    {
                                                        NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"opc{transform.position.x}|{transform.position.y}|0|{t.index.x}|{t.index.y}"));
                                                    }
                                                }
                                                else
                                                {
                                                    transform.position = t.GetComponent<SpriteRenderer>().bounds.center;
                                                    gridSnappedTile = t;
                                                    if (lightObject)
                                                    {
                                                        GetComponentInChildren<CustomLight>().Translate(t.GetComponent<SpriteRenderer>().bounds.center - (Vector3)GetComponentInChildren<CustomLight>().center);
                                                    }
                                                }
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
                                        {
                                            if (!networkReadyForUpdate) { return; }
                                            networkReadyForUpdate = false;
                                            if (lightObject)
                                            {
                                                NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"opc{transform.position.x}|{transform.position.y}|1|{t.index.x}|{t.index.y}"));
                                            }
                                            else
                                            {
                                                NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"opc{transform.position.x}|{transform.position.y}|0|{t.index.x}|{t.index.y}"));
                                            }
                                        }
                                        else
                                        {
                                            transform.position = t.GetComponent<SpriteRenderer>().bounds.center;
                                            gridSnappedTile = t;
                                            if (lightObject)
                                            {
                                                GetComponentInChildren<CustomLight>().Translate(t.GetComponent<SpriteRenderer>().bounds.center - (Vector3)GetComponentInChildren<CustomLight>().center);
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Vector2.Distance(mousePos, t.GetComponent<SpriteRenderer>().bounds.center) < t.GetComponent<SpriteRenderer>().bounds.extents.magnitude / 1.55f && Vector2.Distance(transform.position, t.GetComponent<SpriteRenderer>().bounds.center) > t.GetComponent<SpriteRenderer>().bounds.extents.magnitude / 3.0f)
                        {
                            if (t.Type != TileTypes.Wall && t.Type != TileTypes.Window)
                            {
                                if (t.shadowObject != null)
                                {
                                    ComplexShape shadow;
                                    if (t.shadowObject.TryGetComponent(out shadow))
                                    {
                                        if (!shadow.functional)
                                        {
                                            if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
                                            {
                                                if (!networkReadyForUpdate) { return; }
                                                networkReadyForUpdate = false;
                                                if (lightObject)
                                                {
                                                    NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"opc{transform.position.x}|{transform.position.y}|1|{t.index.x}|{t.index.y}"));
                                                }
                                                else
                                                {
                                                    NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"opc{transform.position.x}|{transform.position.y}|0|{t.index.x}|{t.index.y}"));
                                                }
                                            }
                                            else
                                            {
                                                transform.position = t.GetComponent<SpriteRenderer>().bounds.center;
                                                gridSnappedTile = t;
                                                if (lightObject)
                                                {
                                                    GetComponentInChildren<CustomLight>().Translate(t.GetComponent<SpriteRenderer>().bounds.center - (Vector3)GetComponentInChildren<CustomLight>().center);
                                                }
                                            }
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
                                    {
                                        if (!networkReadyForUpdate) { return; }
                                        networkReadyForUpdate = false;
                                        if (lightObject)
                                        {
                                            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"opc{transform.position.x}|{transform.position.y}|1|{t.index.x}|{t.index.y}"));
                                        }
                                        else
                                        {
                                            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"opc{transform.position.x}|{transform.position.y}|0|{t.index.x}|{t.index.y}"));
                                        }
                                    }
                                    else
                                    {
                                        transform.position = t.GetComponent<SpriteRenderer>().bounds.center;
                                        gridSnappedTile = t;
                                        if (lightObject)
                                        {
                                            GetComponentInChildren<CustomLight>().Translate(t.GetComponent<SpriteRenderer>().bounds.center - (Vector3)GetComponentInChildren<CustomLight>().center);
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
                {
                    if (lightObject)
                    {
                        NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"opc{transform.position.x}|{transform.position.y}|1|X|X|{mousePos.x}|{mousePos.y}"));
                    }
                    else
                    {
                        NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"opc{transform.position.x}|{transform.position.y}|1|X|X|{mousePos.x}|{mousePos.y}"));
                    }
                }
                else
                {
                    transform.position = mousePos;
                    if (lightObject)
                    {
                        GetComponentInChildren<CustomLight>().Translate(mousePos - (Vector2)transform.position);
                    }
                }
            }
        }
    }

    public void Clicked()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            isPicked = true;
            GetComponentInChildren<SpriteRenderer>().color = new UnityEngine.Color(0.4f, 0.6f, 1.0f, 0.55f);

            if (!NetworkingManager.Instance.host) { InfoLogCanvasScript.SendInfoMessage("Only Hosts Can Do This!", UnityEngine.Color.red, 9, false); return; }
            Manager.Instance.inspectingItem = gameObject;
            Manager.Instance.UpdateInspector();
        }
    }
}
