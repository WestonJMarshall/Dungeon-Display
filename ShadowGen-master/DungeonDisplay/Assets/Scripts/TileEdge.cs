using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TileEdge : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            Manager.Instance.inspectingItem = transform.parent.gameObject;
            Manager.Instance.UpdateInspector();
        }
    }
}
