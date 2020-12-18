using UnityEngine;

public class DeleteToolPointButton : MonoBehaviour
{
    public void Hovering()
    {
        if (GetComponentInParent<ShadowTool>() != null)
        {
            GetComponentInParent<ShadowTool>().hoveredButton = gameObject;
        }
        else
        {
            GetComponentInParent<LightTool>().hoveredButton = gameObject;
        }
    }
}
