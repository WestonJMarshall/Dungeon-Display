using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.UI;

public class CanvasScript : MonoBehaviour
{
    #region Public Variables
    public GameObject LightPrefab;
    public bool shadowsEnabled = true;
    public bool gridDisplay = false;
    GameObject shadow;

    public GameObject gridDisplayToggleButton;
    public Sprite gridDisplayToggleSpriteOn;
    public Sprite gridDisplayToggleSpriteOff;
    public GameObject gridSnapToggleButton;
    public Sprite gridSnapToggleSpriteOn;
    public Sprite gridSnapToggleSpriteOff;
    public GameObject shadowToggleButton;
    public Sprite shadowToggleSpriteOn;
    public Sprite shadowToggleSpriteOff;

    public Sprite gridDisplaySprite;

    public GameObject settingsCanvas;

    public GameObject tileSetSelectorCanvas;
    public GameObject globalLight;

    public bool displayUp = true;
    bool moving = false;
    public GameObject movingPanel;
    public RectTransform moveButtonTransform;
    #endregion

    private void Awake()
    {
        shadow = GameObject.Find("Shadow");

        Manager.Instance.gridSnap = true;
        GridSnapToggleSpriteSwitch();
    }

    /// <summary>
    /// Creates a light token on the tile closest to the mouse
    /// </summary>
    public void MakePrefab()
    {
        if (!NetworkingManager.Instance.host) { InfoLogCanvasScript.SendInfoMessage("Only Hosts Can Do This!", UnityEngine.Color.red, 9, false); return; }

        Vector3 pos = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2));

        #region Place On Closest Proper Tile
        try
        {
            Tile closestTile = Manager.Instance.tiles[0, 0];
            foreach (Tile t in Manager.Instance.tiles)
            {
                if (t.Type != TileTypes.Wall && t.Type != TileTypes.Door && t.Type != TileTypes.LockedDoor && t.Type != TileTypes.SecretDoor && t.Type != TileTypes.TrappedDoor && t.Type != TileTypes.Window)
                {
                    if (Vector2.Distance(closestTile.transform.position, pos) > Vector2.Distance(t.transform.position, pos))
                    {
                        closestTile = t;
                    }
                }
            }
            if (closestTile != Manager.Instance.tiles[0, 0])
            {
                pos = closestTile.transform.position;
            }
        }
        catch
        { pos = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2)); }
        #endregion

        pos.z = 0;

        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"lcr{pos.x}|{pos.y}"));
        }
        else
        {
            GameObject instantiatedLight = Instantiate(LightPrefab, Vector3.zero, Quaternion.identity);
            instantiatedLight.GetComponent<MoveObject>().AssignSprite();
            instantiatedLight.GetComponentInChildren<CustomLight>().TranslateWithoutBuilding(pos);
            instantiatedLight.transform.position = pos;
            instantiatedLight.GetComponentInChildren<CustomLight>().UpdateLoop();
            Manager.Instance.FindElementsToManage();
            Manager.Instance.AssignShapes();
            Manager.Instance.BuildAllLights();
            instantiatedLight.GetComponentInChildren<Light2D>().pointLightOuterRadius = instantiatedLight.GetComponentInChildren<CustomLight>().boundingSphereRadius * 1.65f;
        }
    }

    /// <summary>
    /// Creates a light token from server instructions
    /// </summary>
    public void ServerMakePrefab(Vector3 pos)
    {
        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            GameObject instantiatedLight = Instantiate(LightPrefab, Vector3.zero, Quaternion.identity);
            instantiatedLight.GetComponent<MoveObject>().AssignSprite();
            instantiatedLight.GetComponentInChildren<CustomLight>().TranslateWithoutBuilding(pos);
            instantiatedLight.transform.position = pos;
            instantiatedLight.GetComponentInChildren<CustomLight>().UpdateLoop();
            Manager.Instance.FindElementsToManage();
            Manager.Instance.AssignShapes();
            Manager.Instance.BuildAllLights();
            instantiatedLight.GetComponentInChildren<Light2D>().pointLightOuterRadius = instantiatedLight.GetComponentInChildren<CustomLight>().boundingSphereRadius * 1.65f;
        }
    }

    /// <summary>
    /// Creates a token that does not cast light on the tile closest to the mouse
    /// </summary>
    public void MakeObject()
    {
        if (!NetworkingManager.Instance.host) { InfoLogCanvasScript.SendInfoMessage("Only Hosts Can Do This!", UnityEngine.Color.red, 9, false); return; }
        Vector3 pos = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2));

        #region Place On Closest Proper Tile
        try
        {
            Tile closestTile = Manager.Instance.tiles[0, 0];
            foreach (Tile t in Manager.Instance.tiles)
            {
                if (t.Type != TileTypes.Wall && t.Type != TileTypes.Door && t.Type != TileTypes.LockedDoor && t.Type != TileTypes.SecretDoor && t.Type != TileTypes.TrappedDoor)
                {
                    if (Vector2.Distance(closestTile.transform.position, pos) > Vector2.Distance(t.transform.position, pos))
                    {
                        closestTile = t;
                    }
                }
            }
            if (closestTile != Manager.Instance.tiles[0, 0])
            {
                pos = closestTile.transform.position;
            }
        }
        catch
        { pos = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2)); }
        #endregion

        pos.z = 0;

        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"ocr{pos.x}|{pos.y}"));
        }
        else
        {
            GameObject instantiatedLight = Instantiate(LightPrefab, Vector3.zero, Quaternion.identity);
            instantiatedLight.transform.position = pos;
            Destroy(instantiatedLight.GetComponentInChildren<CustomLight>().gameObject);
            Destroy(instantiatedLight.GetComponentInChildren<Light2D>().gameObject);
            instantiatedLight.GetComponent<MoveObject>().AssignSprite();
            instantiatedLight.GetComponentInChildren<SpriteRenderer>().sortingLayerName = "Default";
        }
    }

    /// <summary>
    /// Creates a object token from server instructions
    /// </summary>
    public void ServerMakeObject(Vector3 pos)
    {
        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            GameObject instantiatedLight = Instantiate(LightPrefab, Vector3.zero, Quaternion.identity);

            instantiatedLight.transform.position = pos;
            Destroy(instantiatedLight.GetComponentInChildren<CustomLight>().gameObject);
            Destroy(instantiatedLight.GetComponentInChildren<Light2D>().gameObject);
            instantiatedLight.GetComponent<MoveObject>().AssignSprite();
            instantiatedLight.GetComponentInChildren<SpriteRenderer>().sortingLayerName = "Default";
        }
    }

    /// <summary>
    /// Turns off and on the functionality of all shadows in the map
    /// </summary>
    public void ToggleShadows()
    {
        if (!NetworkingManager.Instance.host) { InfoLogCanvasScript.SendInfoMessage("Only Hosts Can Do This!", UnityEngine.Color.red, 9, false); return; }
        foreach (Tile t in Manager.Instance.tiles)
        {
            if(t.shadowObject != null)
            {
                if (t.Type != TileTypes.Door && t.Type != TileTypes.TrappedDoor && t.Type != TileTypes.SecretDoor && t.Type != TileTypes.LockedDoor)
                {
                    t.shadowObject.GetComponent<ComplexShape>().functional = !t.shadowObject.GetComponent<ComplexShape>().functional;
                }
            }
        }
        if (shadow.transform.position.z > -0.1f)
        {
            shadow.transform.position = new Vector3(shadow.transform.position.x, shadow.transform.position.y, -15.0f);
        }
        else
        {
            shadow.transform.position = new Vector3(shadow.transform.position.x, shadow.transform.position.y, 0.0f);
        }
        Manager.Instance.FindElementsToManage();
        Manager.Instance.AssignShapes();
        Manager.Instance.BuildAllLights();

        ShadowToggleSpriteSwitch();
    }

    /// <summary>
    ///Turns off and on whether tokens will snap to the grid when moved
    /// <summary>
    public void ToggleGridSnap()
    {
        Manager.Instance.gridSnap = !Manager.Instance.gridSnap;
        GridSnapToggleSpriteSwitch();
    }

    /// <summary>
    ///Turns off and on the global light
    /// <summary>
    public void ToggleLighting()
    {
        if (!NetworkingManager.Instance.host) { InfoLogCanvasScript.SendInfoMessage("Only Hosts Can Do This!", UnityEngine.Color.red, 9, false); return; }
        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"glt"));
        }
        else
        {
            if (globalLight.GetComponent<Light2D>().intensity < 0.5f)
            {
                globalLight.GetComponent<Light2D>().intensity = 1.0f;
            }
            else
            {
                globalLight.GetComponent<Light2D>().intensity = 0.1f;
            }
        }
    }

    /// <summary>
    ///Server turns off and on the global light
    /// <summary>
    public void ServerToggleLighting()
    {
        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            if (globalLight.GetComponent<Light2D>().intensity < 0.5f)
            {
                globalLight.GetComponent<Light2D>().intensity = 1.0f;
            }
            else
            {
                globalLight.GetComponent<Light2D>().intensity = 0.1f;
            }
        }
    }

    /// <summary>
    /// Turns the display of the grid off and on
    /// </summary>
    public void ToggleGridDisplay()
    {
        gridDisplay = !gridDisplay;
        if(!gridDisplay)
        {
            foreach(Tile t in Manager.Instance.tiles)
            {
                GameObject tileBevel = new GameObject("tileBevel");
                tileBevel.transform.position = t.transform.position;
                tileBevel.transform.position = new Vector3(tileBevel.transform.position.x, tileBevel.transform.position.y, -3.0f);
                tileBevel.transform.SetParent(t.transform);
                tileBevel.AddComponent<SpriteRenderer>();
                tileBevel.GetComponent<SpriteRenderer>().sprite = gridDisplaySprite;
            }
        }
        else
        {
            foreach (Tile t in Manager.Instance.tiles)
            {
                for(int i = 0; i < t.transform.childCount; i++)
                {
                    if(t.transform.GetChild(i).gameObject.name != "tileHighlight" && !t.transform.GetChild(i).gameObject.CompareTag("EdgeGraphic"))
                    {
                        Destroy(t.transform.GetChild(i).gameObject);
                    }
                }
            }
        }

        GridDisplayToggleSpriteSwitch();
    }

    /// <summary>
    /// Swaps sprites on the button when shadow toggle button is pressed
    /// </summary>
    public void ShadowToggleSpriteSwitch()
    {
        if(shadowsEnabled)
        {
            shadowToggleButton.GetComponent<Image>().sprite = shadowToggleSpriteOff;
            shadowsEnabled = !shadowsEnabled;
        }
        else
        {
            shadowToggleButton.GetComponent<Image>().sprite = shadowToggleSpriteOn;
            shadowsEnabled = !shadowsEnabled;
        }
    }

    /// <summary>
    /// Swaps sprites on the button when grid snap toggle button is pressed
    /// </summary>
    public void GridSnapToggleSpriteSwitch()
    {
        if (!Manager.Instance.gridSnap)
        {
            gridSnapToggleButton.GetComponent<Image>().sprite = gridSnapToggleSpriteOff;
        }
        else
        {
            gridSnapToggleButton.GetComponent<Image>().sprite = gridSnapToggleSpriteOn;
        }
    }

    /// <summary>
    /// Swaps sprites on the button when grid display toggle button is pressed
    /// </summary>
    public void GridDisplayToggleSpriteSwitch()
    {
        if (gridDisplay)
        {
            gridDisplayToggleButton.GetComponent<Image>().sprite = gridDisplayToggleSpriteOff;
        }
        else
        {
            gridDisplayToggleButton.GetComponent<Image>().sprite = gridDisplayToggleSpriteOn;
        }
    }

    /// <summary>
    /// Creates and destroys a settings menu
    /// </summary>
    public void OpenSettings()
    {
        if(settingsCanvas.activeSelf)
        {
            settingsCanvas.SetActive(false);
        }
        else
        {
            settingsCanvas.SetActive(true);
        }
    }

    /// <summary>
    /// Displays the tile set menu when the tile set button is pressed
    /// </summary>
    public void TileSetSelect()
    {
        if (!NetworkingManager.Instance.host) { InfoLogCanvasScript.SendInfoMessage("Only Hosts Can Do This!", UnityEngine.Color.red, 9, false); return; }
        tileSetSelectorCanvas.SetActive(!tileSetSelectorCanvas.activeSelf);
        if (tileSetSelectorCanvas.activeSelf)
        {
            tileSetSelectorCanvas.GetComponent<TileSetSelectCanvasScript>().InitializeTileSetCanvas();
        }
        Manager.Instance.inspector.IconSelectionCanvas.SetActive(false);
        Manager.Instance.inspector.gameObject.SetActive(false);
    }

    /// <summary>
    /// Closes tile set menu
    /// </summary>
    public void CloseTileSetSelector()
    {
        if (tileSetSelectorCanvas.activeSelf)
        {
            tileSetSelectorCanvas.SetActive(false);
        }
    }

    /// <summary>
    /// Transitions the tools panel up and down when the move display button is pressed
    /// </summary>
    public void MoveDisplay()
    {
        if (!moving)
        {
            moving = true;
            if (displayUp)
            {
                StartCoroutine("MovePanelDown");
                displayUp = !displayUp;
            }
            else
            {
                StartCoroutine("MovePanelUp");
                displayUp = !displayUp;
            }
        }
    }

    /// <summary>
    /// Moves the tools panel up over a short time
    /// </summary>
    /// <returns></returns>
    public IEnumerator MovePanelUp()
    {
        yield return new WaitForSeconds(0.006f);

        if(movingPanel.GetComponent<HorizontalLayoutGroup>().padding.bottom < 0)
        {
            movingPanel.GetComponent<HorizontalLayoutGroup>().padding.bottom += 1;
            movingPanel.GetComponent<HorizontalLayoutGroup>().enabled = false;
            movingPanel.GetComponent<HorizontalLayoutGroup>().enabled = true;
            StartCoroutine("MovePanelUp");
        }
        else
        {
            moveButtonTransform.Rotate(new Vector3(0, 0, 180));
            Image i = moveButtonTransform.gameObject.transform.parent.gameObject.GetComponent<Image>();
            i.color = new Color(i.color.r, i.color.g, i.color.b, 1);
            i = moveButtonTransform.gameObject.gameObject.GetComponent<Image>();
            i.color = new Color(i.color.r, i.color.g, i.color.b, 1);
            moving = false;
        }
    }

    /// <summary>
    /// Moves the tools panel down over a short time
    /// </summary>
    /// <returns></returns>
    public IEnumerator MovePanelDown()
    {
        yield return new WaitForSecondsRealtime(0.006f);

        if (movingPanel.GetComponent<HorizontalLayoutGroup>().padding.bottom > -75)
        {
            movingPanel.GetComponent<HorizontalLayoutGroup>().padding.bottom -= 1;
            movingPanel.GetComponent<HorizontalLayoutGroup>().enabled = false;
            movingPanel.GetComponent<HorizontalLayoutGroup>().enabled = true;
            StartCoroutine("MovePanelDown");
        }
        else
        {
            moveButtonTransform.Rotate(new Vector3(0, 0, -180));
            Image i = moveButtonTransform.gameObject.transform.parent.gameObject.GetComponent<Image>();
            i.color = new Color(i.color.r, i.color.g, i.color.b, 0.25f);
            i = moveButtonTransform.gameObject.gameObject.GetComponent<Image>();
            i.color = new Color(i.color.r, i.color.g, i.color.b, 0.5f);
            moving = false;
        }
    }
}
