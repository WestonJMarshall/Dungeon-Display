using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveableLightGraphics : MonoBehaviour
{
    public GameObject moveableObject;

    public void OnMouseDown()
    {
        moveableObject.GetComponent<MoveObject>().Clicked();
    }
}
