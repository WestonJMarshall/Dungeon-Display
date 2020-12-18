using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class InfoLogCanvasScript : MonoBehaviour
{
    #region Singlton
    private static InfoLogCanvasScript instance;

    public static InfoLogCanvasScript Instance { get { return instance; } }
    #endregion

    public static InfoLogCanvasScript InfoLog { get { return GameObject.Find("InfoLogCanvas").GetComponent<InfoLogCanvasScript>(); } }

    public delegate void MessageDelegate(string message, Color color, int fontSize = 9, bool sendThroughServer = true);

    public static MessageDelegate SendInfoMessage;

    public GameObject lowerButton;
    public GameObject logBaseStructure;
    public GameObject inputsStructure;
    public InputField inputField;

    public Transform elementParentTransform;

    public GameObject infoLogElementPrefab;

    List<GameObject> elements;

    void Awake()
    {
        elements = new List<GameObject>();

        StartCoroutine("FadeOutChat");
    }

    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        SendInfoMessage = Instance.Message;

        Image[] startingImages = elementParentTransform.gameObject.GetComponentsInChildren<Image>();
        foreach(Image i in startingImages)
        {
            elements.Add(i.gameObject);
        }
        ResizeElements();
    }

    private void ResizeElements()
    {
        float fullDif = 0;

        Canvas.ForceUpdateCanvases();
        foreach (GameObject g in elements)
        {
            try
            {
                IList<UIVertex> verts = g.GetComponentInChildren<Text>().cachedTextGenerator.verts;
                verts.OrderBy(vert => vert.position.y);

                float dif = verts.First().position.y - verts.Last().position.y;
                g.GetComponent<RectTransform>().sizeDelta = new Vector2(g.GetComponent<RectTransform>().sizeDelta.x, dif + 10);

                fullDif += dif + 10;
            }
            catch
            {

            }
        }
        Canvas.ForceUpdateCanvases();

        if (fullDif >= 440)
        {
            while (fullDif >= 440)
            {
                IList<UIVertex> verts = elements[0].GetComponentInChildren<Text>().cachedTextGenerator.verts;
                verts.OrderBy(vert => vert.position.y);
                float dif = verts.First().position.y - verts.Last().position.y;
                fullDif -= dif + 10;

                DestroyImmediate(elements[0]);
                elements.RemoveAt(0);
            }
            ResizeElements();
        }
    }

    public void MinusButtonPressed()
    {
        if (GetComponent<CanvasScaler>().scaleFactor > 0.6f)
        {
            GetComponent<CanvasScaler>().scaleFactor -= 0.25f;
        }
    }
    public void PlusButtonPressed()
    {
        if (GetComponent<CanvasScaler>().scaleFactor < 1.9f)
        {
            GetComponent<CanvasScaler>().scaleFactor += 0.25f;
        }
    }
    public void LowerButtonPressed()
    {
        if (logBaseStructure.GetComponent<GridLayoutGroup>().padding.bottom > -290)
        {
            logBaseStructure.GetComponent<GridLayoutGroup>().padding.bottom = -300;
            inputsStructure.GetComponent<GridLayoutGroup>().padding.bottom = -326;
            inputsStructure.GetComponent<GridLayoutGroup>().enabled = false;
            inputsStructure.GetComponent<GridLayoutGroup>().enabled = true;
            logBaseStructure.GetComponent<GridLayoutGroup>().enabled = false;
            logBaseStructure.GetComponent<GridLayoutGroup>().enabled = true;
            lowerButton.GetComponent<RectTransform>().localPosition = new Vector3(lowerButton.GetComponent<RectTransform>().localPosition.x, lowerButton.GetComponent<RectTransform>().localPosition.y + 26);
            lowerButton.transform.rotation = Quaternion.Euler(0, 0, 180);
        }
        else
        {
            logBaseStructure.GetComponent<GridLayoutGroup>().padding.bottom = 0;
            inputsStructure.GetComponent<GridLayoutGroup>().padding.bottom = 0;
            inputsStructure.GetComponent<GridLayoutGroup>().enabled = false;
            inputsStructure.GetComponent<GridLayoutGroup>().enabled = true;
            logBaseStructure.GetComponent<GridLayoutGroup>().enabled = false;
            logBaseStructure.GetComponent<GridLayoutGroup>().enabled = true;
            lowerButton.GetComponent<RectTransform>().localPosition = new Vector3(lowerButton.GetComponent<RectTransform>().localPosition.x, lowerButton.GetComponent<RectTransform>().localPosition.y - 26);
            lowerButton.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    public void MessageSend()
    {
        if (inputField.text.Length > 0)
        {
            if(inputField.text.Length > 300)
            {
                inputField.text = inputField.text.Substring(0, 300);
            }
            if (SteamClient.IsLoggedOn)
            {
                Message(SteamClient.Name + ": " + inputField.text, new Color(0.05f,0.17f,0.4f), 15);
                inputField.text = "";
            }
            else
            {
                Message(inputField.text, Color.blue, 15);
                inputField.text = "";
            }
        }
    }

    public void MouseOverChat()
    {
        StopCoroutine("FadeOutChat");
        StopCoroutine("WaitForFadeOutChat");
        StartCoroutine("FadeInChat");
    }

    public void MouseNotOverChat()
    {
        StopCoroutine("FadeInChat");
        StartCoroutine("WaitForFadeOutChat");
    }

    public IEnumerator WaitForFadeOutChat()
    {
        yield return new WaitForSecondsRealtime(1);
        StartCoroutine("FadeOutChat");
    }

    public IEnumerator FadeOutChat()
    {
        yield return new WaitForFixedUpdate();
        var images = logBaseStructure.GetComponentsInChildren<Image>();
        if (images[0].color.a > 0.35f)
        {
            foreach (Image i in images)
            {
                i.color = new Color(i.color.r, i.color.g, i.color.b, i.color.a - 0.063f);
            }
            StartCoroutine("FadeOutChat");
        }
        else
        {
            foreach (Image i in images)
            {
                i.color = new Color(i.color.r, i.color.g, i.color.b, 0.35f);
            }
        }
    }

    public IEnumerator FadeInChat()
    {
        yield return new WaitForFixedUpdate();
        var images = logBaseStructure.GetComponentsInChildren<Image>();
        if (images[0].color.a < 0.91f)
        {
            foreach (Image i in images)
            {
                i.color = new Color(i.color.r, i.color.g, i.color.b, i.color.a + 0.063f);
            }
            StartCoroutine("FadeInChat");
        }
        else
        {
            foreach (Image i in images)
            {
                i.color = new Color(i.color.r, i.color.g, i.color.b, 0.91f);
            }
        }
    }

    public unsafe void Message(string message, Color color, int fontSize = 9, bool sendThroughServer = true)
    {
        try
        {
            if (sendThroughServer && NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
            {
                NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes("msg" + message));
            }
            else
            {
                elements.Add(Instantiate(infoLogElementPrefab, elementParentTransform));
                elements[elements.Count - 1].GetComponent<RectTransform>().sizeDelta = new Vector2(elements[elements.Count - 1].GetComponent<RectTransform>().sizeDelta.x, 50);
                elements[elements.Count - 1].GetComponentInChildren<Text>().text = message;
                elements[elements.Count - 1].GetComponentInChildren<Text>().fontSize = fontSize;
                elements[elements.Count - 1].GetComponentInChildren<Text>().color = color;

                ResizeElements();

                elements[elements.Count - 1].transform.localScale = new Vector3(1, 0);

                StartCoroutine("NewMessageHighlight", elements[elements.Count - 1]);
            }
        }
        catch { }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) { MessageSend(); }
    }

    private IEnumerator NewMessageHighlight(GameObject element)
    {
        yield return new WaitForFixedUpdate();

        if (element != null)
        {
            element.transform.localScale = new Vector3(1, element.transform.localScale.y + 0.05f);

            if (element.transform.localScale.y < 1)
            {
                StartCoroutine("NewMessageHighlight", element);
            }
            else
            {
                element.transform.localScale = new Vector3(1, 1);
                ResizeElements();
            }
        }
    }
}
