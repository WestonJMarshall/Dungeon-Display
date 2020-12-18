using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FloatingTextButton : MonoBehaviour
{
    private void Start()
    {
        GetComponentInChildren<Text>().GetComponentInParent<Image>().enabled = false;
        GetComponentInChildren<Text>().enabled = false;
    }

    public void MouseEnter()
    {
        GetComponentInChildren<Text>().GetComponentInParent<Image>().enabled = true;
        GetComponentInChildren<Text>().enabled = true;
    }

    public void MouseExit()
    {
        GetComponentInChildren<Text>().GetComponentInParent<Image>().enabled = false;
        GetComponentInChildren<Text>().enabled = false;
    }
}
