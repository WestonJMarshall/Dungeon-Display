using UnityEngine;

public class DragPoint : MonoBehaviour
{
    public GameObject linkedTool;
    public bool typeShadow = true;

    private void OnMouseDrag()
    {
        if (typeShadow)
        {
            linkedTool.GetComponent<ShadowTool>().pauseUpdates = true;
            linkedTool.GetComponent<ShadowTool>().DragPoint();
        }
        else
        {
            linkedTool.GetComponent<LightTool>().pauseUpdates = true;
            linkedTool.GetComponent<LightTool>().DragPoint();
        }
    }

    private void OnMouseUp()
    {
        if (typeShadow)
        {
            linkedTool.GetComponent<ShadowTool>().pauseUpdates = false;
        }
        else
        {
            linkedTool.GetComponent<LightTool>().pauseUpdates = false;
        }
    }

    private void OnMouseEnter()
    {
        if (typeShadow)
        {
            linkedTool.GetComponent<ShadowTool>().currentDragPoint = gameObject;
        }
        else
        {
            linkedTool.GetComponent<LightTool>().currentDragPoint = gameObject;
        }
    }
}
