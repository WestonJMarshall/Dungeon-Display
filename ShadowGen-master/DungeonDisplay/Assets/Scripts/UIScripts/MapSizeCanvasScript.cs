using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapSizeCanvasScript : MonoBehaviour
{
    public void OnClose()
    {
        gameObject.SetActive(false);
        InfoLogCanvasScript.SendInfoMessage("Loading Dungeon", UnityEngine.Color.black, 9, false);
        SceneManager.LoadScene(1);
    }
}
