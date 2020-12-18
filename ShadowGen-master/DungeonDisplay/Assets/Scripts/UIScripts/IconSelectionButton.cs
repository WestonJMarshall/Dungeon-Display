using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconSelectionButton : MonoBehaviour
{
    public string buttonFilePath = "";
    public void Hovering()
    {
        GetComponentInParent<IconSelectCanvasScript>().hoveredButton = gameObject;
    }
}
