using UnityEngine;
using UnityEngine.UI;

public class CanvasRescaler : MonoBehaviour
{
    [SerializeField]
    private float screenScale = 1;

    private float baseWidth = 0;
    private float baseHeight = 0;

    void Start()
    {
        baseWidth = GetComponentInParent<GridLayoutGroup>().cellSize.x;
        baseHeight = GetComponentInParent<GridLayoutGroup>().cellSize.y;
    }

    public void Update()
    {
        //Find Farthest Value
        if(Screen.width / baseWidth < Screen.height / baseHeight)
        {
            transform.localScale = new Vector3(Screen.width / baseWidth, Screen.width / baseWidth, 1.0f);
            transform.localScale *= screenScale;
        }
        else
        {
            transform.localScale = new Vector3(Screen.height / baseHeight, Screen.height / baseHeight, 1.0f);
            transform.localScale *= screenScale;
        }
    }
}
