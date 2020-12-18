using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateRenderTexture : MonoBehaviour
{
    [SerializeField]
    private RenderTexture renderTexture;
    private Vector2Int screenSize;

    void Awake()
    {
        UpdateTextureSize();
    }

    private void UpdateTextureSize()
    {
        screenSize = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        renderTexture.width = screenSize.x;
        renderTexture.height = screenSize.y;
    }
}
