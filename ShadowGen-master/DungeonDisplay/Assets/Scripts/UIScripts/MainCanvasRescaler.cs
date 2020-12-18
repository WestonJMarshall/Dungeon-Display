using UnityEngine;
using UnityEngine.UI;

public class MainCanvasRescaler : MonoBehaviour
{
    void Update()
    {
        if (Screen.width <= 780)
        {
            transform.localScale = new Vector3(Screen.width / 780.0f, Screen.width / 780.0f, 1.0f);
            float a = ((float)Screen.height / Screen.width);
            float b = (780.0f - Screen.width) * -0.5f;
            int i = (int)(a * b);
            GetComponent<HorizontalLayoutGroup>().padding.bottom = i;

            i = (int)(780.0f * (1.0f - (Screen.width / 780.0f)));
            GetComponent<HorizontalLayoutGroup>().padding.left = (int)(i * -0.5f);
        }
        else
        {
            transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

            GetComponent<HorizontalLayoutGroup>().padding.bottom = 0;
            GetComponent<HorizontalLayoutGroup>().padding.left = 0;
        }
    }
}
