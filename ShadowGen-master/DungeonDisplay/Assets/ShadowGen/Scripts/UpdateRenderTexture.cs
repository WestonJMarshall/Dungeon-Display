using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateRenderTexture : MonoBehaviour
{
    private static RenderTexture renderTexture = null;
    private Camera lightCamera;
    private Vector2 resolution;

    [SerializeField]
    private List<GameObject> shadows = null;

    void Awake()
    {
        lightCamera = GetComponent<Camera>();
        resolution = new Vector2(Screen.width, Screen.height);
        UpdateTextureSize();
    }

    private void Update()
    {
        if (resolution.x != Screen.width || resolution.y != Screen.height)
        {
            UpdateTextureSize();

            resolution.x = Screen.width;
            resolution.y = Screen.height;
        }
    }

    private void UpdateTextureSize()
    {
        if (lightCamera.targetTexture != null)
        {
            lightCamera.targetTexture.Release();
        }

        renderTexture = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, -1);
        lightCamera.targetTexture = renderTexture;

        for(int i = 0; i < shadows.Count; i++)
        {
            shadows[i].GetComponent<MeshRenderer>().material.SetTexture("_Texture2D_LightMask", renderTexture);
        }
    }
}
