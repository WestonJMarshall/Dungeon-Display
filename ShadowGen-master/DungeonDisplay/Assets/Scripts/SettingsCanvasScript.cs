using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsCanvasScript : MonoBehaviour
{
    public Text saveText;

    public GameObject serverButton;
    public Sprite serverStartSprite;
    public Sprite serverCloseSprite;

    public void ExitProgram()
    {
        if (NetworkingManager.Instance.networkingState == NetworkingState.IsServer)
        {
            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"rtm"));
        }

        NetworkingManager.Instance.DeatachFromOnline();
        Application.Quit();
    }

    public void ReturnToMainMenu()
    {
        if (NetworkingManager.Instance.networkingState == NetworkingState.IsServer)
        {
            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"rtm"));
        }
        else
        {
            NetworkingManager.Instance.DeatachFromOnline();
            SceneManager.LoadScene(0);
        }
    }

    public static void ServerReturnToMainMenu()
    {
        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            NetworkingManager.Instance.DeatachFromOnline();
            SceneManager.LoadScene(0);
        }
    }

    public static void ServerReturnToMainMenuNoDetatch()
    {
        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            SceneManager.LoadScene(0);
        }
    }

    public void SaveMap()
    {
        if (saveText.text == "")
        {
            Manager.Instance.SaveMap("UnnamedSave");
        }
        else
        {
            Manager.Instance.SaveMap(saveText.text);
        }
    }

    public void ServerStart()
    {
        if(serverButton.GetComponent<Image>().sprite == serverStartSprite)
        {
            serverButton.GetComponent<Image>().sprite = serverCloseSprite;
            serverButton.GetComponentInChildren<Text>().text = "Close Server";
        }
        else
        {
            serverButton.GetComponent<Image>().sprite = serverStartSprite;
            serverButton.GetComponentInChildren<Text>().text = "Start Server";
        }
        var sbp = NetworkingManager.Instance.ServerButtonPressed();
    }

    public void InviteFriend()
    {
        NetworkingManager.Instance.InviteToCurrentLobbyButton();
    }

    public void PlusButton()
    {
        CanvasScaler[] cs = FindObjectsOfType<CanvasScaler>();
        foreach(CanvasScaler c in cs)
        {
            if(c.gameObject.name != "InfoLogCanvas")
            {
                if(c.scaleFactor < 2.9f)
                {
                    c.scaleFactor += 0.25f;
                }
            }
        }
    }

    public void MinusButton()
    {
        CanvasScaler[] cs = FindObjectsOfType<CanvasScaler>();
        foreach (CanvasScaler c in cs)
        {
            if (c.gameObject.name != "InfoLogCanvas")
            {
                if (c.scaleFactor > 0.3f)
                {
                    c.scaleFactor -= 0.25f;
                }
            }
        }
    }
}
