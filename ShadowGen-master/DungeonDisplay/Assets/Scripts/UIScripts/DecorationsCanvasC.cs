using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DecorationsCanvasC : MonoBehaviour
{
    private void Update()
    {        
        //Find Farthest Value
        if (Screen.width / 1020.0f < Screen.height / 630.0f)
        {
            GetComponent<CanvasScaler>().scaleFactor = Screen.width / 1020.0f;
        }
        else
        {
            GetComponent<CanvasScaler>().scaleFactor = Screen.height / 630.0f;
        }
    }
}
