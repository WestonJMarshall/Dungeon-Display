using UnityEngine;

public class TileSetSelectionButton : MonoBehaviour
{
    public string path;
    public void Hovering()
    {
        GetComponentInParent<TileSetSelectCanvasScript>().hoveredButton = gameObject;
    }
}
