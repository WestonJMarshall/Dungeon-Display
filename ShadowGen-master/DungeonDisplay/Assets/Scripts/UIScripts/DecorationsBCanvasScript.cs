using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class DecorationsBCanvasScript : MonoBehaviour
{
    public Text textToFade = null;
    public Image imageToFade = null;
    public GameObject canvasC;
    void Awake()
    {
        StartCoroutine("WaitForFadeOut");
    }

    public IEnumerator WaitForFadeOut()
    {
        yield return new WaitForSeconds(0.4f);

        StartCoroutine("FadeOut");
    }

    public IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(0.09f);

        textToFade.text = textToFade.text.Substring(0, textToFade.text.Length - 1);

        if (textToFade.text.Length > 0)
        {
            StartCoroutine("FadeOut");
        }
        else
        {
            StartCoroutine("FadeOutImage");
        }
    }


    public IEnumerator FadeOutImage()
    {
        yield return new WaitForFixedUpdate();

        imageToFade.color = new Color(imageToFade.color.r, imageToFade.color.g, imageToFade.color.b, imageToFade.color.a - 0.016f);

        if (imageToFade.color.a > 0.0001f)
        {
            StartCoroutine("FadeOutImage");
        }
        else
        {
            Destroy(gameObject);
            Destroy(canvasC);
        }
    }
}
