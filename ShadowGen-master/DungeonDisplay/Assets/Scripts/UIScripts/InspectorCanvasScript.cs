using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Drawing;
using UnityEngine.Experimental.Rendering.Universal;
using System.Text;

public class InspectorCanvasScript : MonoBehaviour
{
    public GameObject inspectingItem;
    public Text nameText;
    public GameObject IconSelectionCanvas;
    public GameObject inspectorSubElementButtonPrefab;
    public GameObject inspectorSubElementInputPrefab;
    public GameObject canvasBody;
    public Sprite highlightedItemSprite;
    public GameObject inspectingItemHighlight;

    public List<GameObject> inspectorSubElements;

    public void SetupInformation()
    {
        CloseIconSelect();

        //Highlight selected object
        if (inspectingItemHighlight != null) { Destroy(inspectingItemHighlight); }
        inspectingItemHighlight = new GameObject("tileHighlight");
        inspectingItemHighlight.transform.position = inspectingItem.transform.position;
        inspectingItemHighlight.transform.position = new Vector3(inspectingItemHighlight.transform.position.x, inspectingItemHighlight.transform.position.y, -3.0f);
        inspectingItemHighlight.transform.SetParent(inspectingItem.transform);
        inspectingItemHighlight.AddComponent<SpriteRenderer>();
        inspectingItemHighlight.GetComponent<SpriteRenderer>().sprite = highlightedItemSprite;


        for (int i = 0; i < inspectorSubElements.Count; i++)
        {
            Destroy(inspectorSubElements[i]);
        }
        inspectorSubElements = new List<GameObject>();
        Tile inspectingTile;
        inspectingItem.TryGetComponent(out inspectingTile);

        #region Tile Inspector
        if (inspectingTile != null)
        {
            nameText.text = inspectingTile.Type.ToString();

            //Element 1
            if (File.Exists("GameLoadedAssets\\Sprites\\UIIcons\\IconSwapIcon.png"))
            {
                inspectorSubElements.Add(Instantiate(inspectorSubElementButtonPrefab, canvasBody.transform));
                inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<Text>().text = "Change Sprite";
                inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<Button>().onClick.AddListener(IconSelect);
                AssignSpriteToInspectorElement("GameLoadedAssets\\Sprites\\UIIcons\\IconSwapIcon.png", inspectorSubElements.Count - 1);
            }

            if (inspectingTile.Type == TileTypes.Door || inspectingTile.Type == TileTypes.SecretDoor || inspectingTile.Type == TileTypes.LockedDoor || inspectingTile.Type == TileTypes.TrappedDoor)
            {
                if (File.Exists("GameLoadedAssets\\Sprites\\UIIcons\\DoorToggleIcon.png"))
                {
                    //Element 2
                    inspectorSubElements.Add(Instantiate(inspectorSubElementButtonPrefab, canvasBody.transform));
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<Text>().text = "Toggle Door";
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<Button>().onClick.AddListener(DoorClicked);
                    AssignSpriteToInspectorElement("GameLoadedAssets\\Sprites\\UIIcons\\DoorToggleIcon.png", inspectorSubElements.Count - 1);
                }
            }
        }
        #endregion
        else
        {
            #region MoveObject Inspector
            MoveObject inspectingMoveObject;
            inspectingItem.TryGetComponent(out inspectingMoveObject);

            inspectingItemHighlight.transform.localScale = inspectingMoveObject.GetComponentInChildren<SpriteRenderer>().transform.localScale;

            if (inspectingMoveObject != null)
            {
                if (inspectingMoveObject.lightObject)
                {
                    nameText.text = "Light Caster";

                    //Element 1
                    if (File.Exists("GameLoadedAssets\\Sprites\\UIIcons\\IconSwapIcon.png"))
                    {
                        inspectorSubElements.Add(Instantiate(inspectorSubElementButtonPrefab, canvasBody.transform));
                        inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<Text>().text = "Change Sprite";
                        inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<Button>().onClick.AddListener(IconSelect);
                        AssignSpriteToInspectorElement("GameLoadedAssets\\Sprites\\UIIcons\\IconSwapIcon.png", inspectorSubElements.Count - 1);
                    }
                    //Element 2
                    if (File.Exists("GameLoadedAssets\\Sprites\\UIIcons\\MoveLightToggleIcon.png"))
                    {
                        inspectorSubElements.Add(Instantiate(inspectorSubElementButtonPrefab, canvasBody.transform));
                        inspectorSubElements[1].GetComponentInChildren<Text>().text = "Toggle Light";
                        inspectorSubElements[1].GetComponentInChildren<Button>().onClick.AddListener(LightToggled);
                        AssignSpriteToInspectorElement("GameLoadedAssets\\Sprites\\UIIcons\\MoveLightToggleIcon.png", inspectorSubElements.Count - 1);
                    }
                    //Element 3
                    inspectorSubElements.Add(Instantiate(inspectorSubElementInputPrefab, canvasBody.transform));
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentsInChildren<Text>()[1].text = "Light Area" + "\n" + "(In Tiles)";
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<InputField>().onValueChanged.AddListener(LightScaleChanged);
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<InputField>().text = (FindTileRadius() + 0.0095f).ToString().Substring(0,4);
                    //Element 4
                    inspectorSubElements.Add(Instantiate(inspectorSubElementInputPrefab, canvasBody.transform));
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentsInChildren<Text>()[1].text = "Rotation" + "\n" + "(0 to 360)";
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<InputField>().onValueChanged.AddListener(ObjectRotationChanged);
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<InputField>().text = inspectingItem.GetComponentInChildren<SpriteRenderer>().transform.localRotation.eulerAngles.z.ToString();
                    //Element 5
                    inspectorSubElements.Add(Instantiate(inspectorSubElementInputPrefab, canvasBody.transform));
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentsInChildren<Text>()[1].text = "Scale" + "\n" + "(0.25 to 9)";
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<InputField>().onValueChanged.AddListener(ObjectScaleChanged);
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<InputField>().text = inspectingItem.GetComponentInChildren<SpriteRenderer>().transform.localScale.x.ToString();
                    //Element 6
                    if (File.Exists("GameLoadedAssets\\Sprites\\UIIcons\\LightRemoveIcon.png"))
                    {
                        inspectorSubElements.Add(Instantiate(inspectorSubElementButtonPrefab, canvasBody.transform));
                        inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<Text>().text = "Delete";
                        inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<Button>().onClick.AddListener(DeleteLight);
                        AssignSpriteToInspectorElement("GameLoadedAssets\\Sprites\\UIIcons\\LightRemoveIcon.png", inspectorSubElements.Count - 1);
                    }
                }
                else
                {
                    nameText.text = "Object";

                    //Element 1
                    if (File.Exists("GameLoadedAssets\\Sprites\\UIIcons\\IconSwapIcon.png"))
                    {
                        inspectorSubElements.Add(Instantiate(inspectorSubElementButtonPrefab, canvasBody.transform));
                        inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<Text>().text = "Change Sprite";
                        inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<Button>().onClick.AddListener(IconSelect);
                        AssignSpriteToInspectorElement("GameLoadedAssets\\Sprites\\UIIcons\\IconSwapIcon.png", inspectorSubElements.Count - 1);
                    }
                    //Element 2
                    inspectorSubElements.Add(Instantiate(inspectorSubElementInputPrefab, canvasBody.transform));
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentsInChildren<Text>()[1].text = "Rotation" + "\n" + "(0 to 360)";
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<InputField>().onValueChanged.AddListener(ObjectRotationChanged);
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<InputField>().text = inspectingItem.GetComponentInChildren<SpriteRenderer>().transform.localRotation.eulerAngles.z.ToString();
                    //Element 3
                    inspectorSubElements.Add(Instantiate(inspectorSubElementInputPrefab, canvasBody.transform));
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentsInChildren<Text>()[1].text = "Scale" + "\n" + "(0 to 9)";
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<InputField>().onValueChanged.AddListener(ObjectScaleChanged);
                    inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<InputField>().text = inspectingItem.GetComponentInChildren<SpriteRenderer>().transform.localScale.x.ToString();
                    //Element 4
                    if (File.Exists("GameLoadedAssets\\Sprites\\UIIcons\\LightRemoveIcon.png"))
                    {
                        inspectorSubElements.Add(Instantiate(inspectorSubElementButtonPrefab, canvasBody.transform));
                        inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<Text>().text = "Delete";
                        inspectorSubElements[inspectorSubElements.Count - 1].GetComponentInChildren<Button>().onClick.AddListener(DeleteLight);
                        AssignSpriteToInspectorElement("GameLoadedAssets\\Sprites\\UIIcons\\LightRemoveIcon.png", inspectorSubElements.Count - 1);
                    }
                }
            }
            #endregion
            else
            {

            }
        }
        GetComponent<GridLayoutGroup>().cellSize = new Vector2(GetComponent<GridLayoutGroup>().cellSize.x, 100 + (inspectorSubElements.Count * 75));
    }
    public void DoorClicked()
    {
        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"doc{inspectingItem.GetComponent<Tile>().index.x}|{inspectingItem.GetComponent<Tile>().index.y}"));
        }
        else
        {
            if (inspectingItem.GetComponent<Tile>().shadowObject != null)
            {
                ComplexShape shadow;
                if (inspectingItem.GetComponent<Tile>().shadowObject.TryGetComponent(out shadow))
                {
                    if (shadow.functional)
                    {
                        inspectingItem.GetComponent<Tile>().spriteRenderer.color = new UnityEngine.Color(inspectingItem.GetComponent<Tile>().spriteRenderer.color.r, inspectingItem.GetComponent<Tile>().spriteRenderer.color.g, inspectingItem.GetComponent<Tile>().spriteRenderer.color.b, 0.35f);
                        inspectingItem.transform.position = new Vector3(inspectingItem.transform.position.x, inspectingItem.transform.position.y, 1.0f);
                    }
                    else
                    {
                        inspectingItem.GetComponent<Tile>().spriteRenderer.color = new UnityEngine.Color(inspectingItem.GetComponent<Tile>().spriteRenderer.color.r, inspectingItem.GetComponent<Tile>().spriteRenderer.color.g, inspectingItem.GetComponent<Tile>().spriteRenderer.color.b, 1.0f);
                        inspectingItem.transform.position = new Vector3(inspectingItem.transform.position.x, inspectingItem.transform.position.y, -1.0f);
                    }
                    shadow.functional = !shadow.functional;
                    Manager.Instance.FindElementsToManage();
                    Manager.Instance.AssignShapes();
                    Manager.Instance.BuildAllLights();
                }
            }
        }
    }
    public static void ServerDoorClicked(GameObject door)
    {
        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            if (door.GetComponent<Tile>().shadowObject != null)
            {
                ComplexShape shadow;
                if (door.GetComponent<Tile>().shadowObject.TryGetComponent(out shadow))
                {
                    if (shadow.functional)
                    {
                        door.GetComponent<Tile>().spriteRenderer.color = new UnityEngine.Color(door.GetComponent<Tile>().spriteRenderer.color.r, door.GetComponent<Tile>().spriteRenderer.color.g, door.GetComponent<Tile>().spriteRenderer.color.b, 0.35f);
                        door.transform.position = new Vector3(door.transform.position.x, door.transform.position.y, 1.0f);
                    }
                    else
                    {
                        door.GetComponent<Tile>().spriteRenderer.color = new UnityEngine.Color(door.GetComponent<Tile>().spriteRenderer.color.r, door.GetComponent<Tile>().spriteRenderer.color.g, door.GetComponent<Tile>().spriteRenderer.color.b, 1.0f);
                        door.transform.position = new Vector3(door.transform.position.x, door.transform.position.y, -1.0f);
                    }
                    shadow.functional = !shadow.functional;
                    Manager.Instance.FindElementsToManage();
                    Manager.Instance.AssignShapes();
                    Manager.Instance.BuildAllLights();
                }
            }
        }
    }
    public void LightToggled()
    {
        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"ovc{inspectingItem.transform.position.x}|{inspectingItem.transform.position.y}|X|X|X|{1}"));
        }
        else
        {
            inspectingItem.GetComponentInChildren<CustomLight>().functional = !inspectingItem.GetComponentInChildren<CustomLight>().functional;
            if (inspectingItem.GetComponentInChildren<Light2D>().pointLightInnerRadius > 0.001f)
            {
                inspectingItem.GetComponentInChildren<Light2D>().pointLightInnerRadius = 0;
            }
            else
            {
                inspectingItem.GetComponentInChildren<Light2D>().pointLightInnerRadius = float.MaxValue;
            }
            Manager.Instance.FindElementsToManage();
            Manager.Instance.AssignShapes();
            Manager.Instance.BuildAllLights();
        }
    }

    public void DeleteLight()
    {
        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"odl{inspectingItem.transform.position.x}|{inspectingItem.transform.position.y}"));
        }
        else
        {
            Destroy(inspectingItem);
            if (inspectingItem.GetComponent<MoveObject>().lightObject) { Manager.Instance.FindElementsToManage(); }
            Destroy(inspectingItemHighlight);
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }

    public static void ServerDeleteLight(GameObject deleteItem, GameObject tileHighlight)
    {
        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            Destroy(deleteItem);
            if (deleteItem.GetComponent<MoveObject>().lightObject) { Manager.Instance.FindElementsToManage(); }
            Destroy(tileHighlight);
        }
    }

    public void IconSelect()
    {
        IconSelectionCanvas.SetActive(!IconSelectionCanvas.activeSelf);
        if(IconSelectionCanvas.activeSelf)
        {
            IconSelectionCanvas.GetComponent<IconSelectCanvasScript>().inputObject = inspectingItem;
            IconSelectionCanvas.GetComponent<IconSelectCanvasScript>().InitializeIconCanvas();
        }

        Manager.Instance.CloseLightMenu();
        Manager.Instance.CloseShadowMenu();
    }

    public void CloseIconSelect()
    {
        if (IconSelectionCanvas.activeSelf)
        {
            IconSelectionCanvas.SetActive(!IconSelectionCanvas.activeSelf);
            Manager.Instance.CloseLightMenu();
            Manager.Instance.CloseShadowMenu();
        }
    }

    public void LightScaleChanged(string scale)
    {
        float convertedScale = -1;
        float.TryParse(scale, out convertedScale);
        if(convertedScale > 18) { convertedScale = 18; }
        float scaleMultiplier = convertedScale / inspectingItem.GetComponentInChildren<CustomLight>().boundingSphereRadius;
        if (convertedScale > 0 && scaleMultiplier > 0.05f)
        {
            if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
            {
                NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"ovc{inspectingItem.transform.position.x}|{inspectingItem.transform.position.y}|{convertedScale}|X|X|X"));
            }
            else
            {
                //Find how much the area needs to be scaled to have the inputed range
                inspectingItem.GetComponentInChildren<CustomLight>().Scale(inspectingItem.transform.position, scaleMultiplier);
                inspectingItem.GetComponentInChildren<Light2D>().pointLightOuterRadius = inspectingItem.GetComponentInChildren<CustomLight>().boundingSphereRadius * 1.65f;
                Manager.Instance.FindElementsToManage();
                Manager.Instance.AssignShapes();
                inspectingItem.GetComponentInChildren<CustomLight>().PrepareAndBuildLight();
            }
        }
    }

    public void ObjectRotationChanged(string degrees)
    {
        int convertedDegrees = -1;
        int.TryParse(degrees, out convertedDegrees);
        if(inspectingItem.GetComponentInChildren<SpriteRenderer>().transform.localRotation.eulerAngles.z == convertedDegrees) { return; }
        if (convertedDegrees >= 0 && convertedDegrees <= 360)
        {
            if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
            {
                NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"ovc{inspectingItem.transform.position.x}|{inspectingItem.transform.position.y}|X|{convertedDegrees}|X|X"));
            }
            else
            {
                inspectingItem.GetComponentInChildren<SpriteRenderer>().transform.localRotation = Quaternion.Euler(inspectingItem.GetComponentInChildren<SpriteRenderer>().transform.localRotation.eulerAngles.x, inspectingItem.GetComponentInChildren<SpriteRenderer>().transform.localRotation.eulerAngles.y, convertedDegrees);
            }
        }
    }

    public void ObjectScaleChanged(string scale)
    {
        float convertedScale = -1;
        float.TryParse(scale, out convertedScale);
        if (inspectingItem.GetComponentInChildren<SpriteRenderer>().transform.localScale.x == convertedScale) { return; }
        inspectingItemHighlight.transform.localScale = new Vector3(convertedScale, convertedScale, convertedScale);
        if (convertedScale >= 0.25f && convertedScale <= 9)
        {
            if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
            {
                NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"ovc{inspectingItem.transform.position.x}|{inspectingItem.transform.position.y}|X|X|{convertedScale}|X"));
            }
            else
            {
                inspectingItem.GetComponentInChildren<SpriteRenderer>().transform.localScale = new Vector3(convertedScale, convertedScale, inspectingItem.GetComponentInChildren<SpriteRenderer>().transform.localScale.z);
            }
        }
    }

    public float FindTileRadius()
    {
        float tileRadius = -1;
        tileRadius = inspectingItem.GetComponentInChildren<CustomLight>().boundingSphereRadius;
        return tileRadius;
    }

    public void CloseInspector()
    {
        if(IconSelectionCanvas.activeSelf)
        {
            IconSelectionCanvas.SetActive(false);
        }
        Destroy(inspectingItemHighlight);
        gameObject.SetActive(false);
    }

    private void AssignSpriteToInspectorElement(string imagePath, int elementIndex)
    {
        //Get and assign sprite
        FileStream s = new FileStream(imagePath, FileMode.Open);
        Bitmap image = new Bitmap(s);
        int textureWidth = image.Width;
        int textureHeight = image.Height;
        s.Close();
        Texture2D inspectorSubElementTexture = new Texture2D(textureWidth, textureHeight);
        inspectorSubElementTexture.LoadImage(File.ReadAllBytes(imagePath));
        Sprite inspectorSubElementSprite = Sprite.Create(inspectorSubElementTexture, new Rect(Vector2.zero, new Vector2(textureWidth, textureHeight)), Vector2.zero);
        inspectorSubElements[elementIndex].GetComponentInChildren<Button>().GetComponent<UnityEngine.UI.Image>().sprite = inspectorSubElementSprite;
    }
}


