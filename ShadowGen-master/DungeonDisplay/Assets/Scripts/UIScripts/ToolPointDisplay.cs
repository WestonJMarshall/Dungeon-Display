using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolPointDisplay : MonoBehaviour
{
    public void OnValueAltered()
    {
        string input = GetComponent<InputField>().text;
        float parsedValue = 0.0f;
        float.TryParse(input, out parsedValue);

        if (transform.parent.name == "VerticalFormattingA")
        {
            if (GetComponentInParent<ShadowTool>() != null)
            {
                GetComponentInParent<ShadowTool>().PointAltered(parsedValue, GetComponentInParent<ShadowTool>().xPoints.IndexOf(gameObject), true);
            }
            else
            {
                GetComponentInParent<LightTool>().PointAltered(parsedValue, GetComponentInParent<LightTool>().xPoints.IndexOf(gameObject), true);
            }
        }
        else
        {
            if (GetComponentInParent<ShadowTool>() != null)
            {
                GetComponentInParent<ShadowTool>().PointAltered(parsedValue, GetComponentInParent<ShadowTool>().yPoints.IndexOf(gameObject), false);
            }
            else
            {
                GetComponentInParent<LightTool>().PointAltered(parsedValue, GetComponentInParent<LightTool>().xPoints.IndexOf(gameObject), false);
            }
        }
    }
}
