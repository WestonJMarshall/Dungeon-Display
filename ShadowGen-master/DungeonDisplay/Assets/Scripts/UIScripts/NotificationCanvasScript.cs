using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotificationCanvasScript : MonoBehaviour
{
    public Text notificationText;

    public void CloseSelf()
    {
        Destroy(gameObject);
    }
}
