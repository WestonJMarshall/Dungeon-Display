using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class FreeLightArea : MonoBehaviour
{
    public float boundingSphereRadius;
    public Vector2 boundingSphereCenter;
    public List<Vector2> points;
    public bool functional = true;

    private void Awake()
    {
        gameObject.layer = 8;

        points = new List<Vector2>
        {
            new Vector2(-1, -1),
            new Vector2(-1, 1),
            new Vector2(1, 1),
            new Vector2(1, -1)
        };

        Spherize();

        for(int i = 0; i < points.Count; i++)
        {
            points[i] += (Vector2)Camera.main.ScreenToWorldPoint(new Vector2(Screen.width / 2, Screen.height / 2));
        }
    }

    /// <summary>
    /// Creates the mesh that will be displayed for this light
    /// </summary>
    public void DrawLight()
    {
        if (functional)
        {
            if (points.Count < 3) { return; }

            Vector2[] vertices2D = new Vector2[points.Count];

            for (int c = 0; c < points.Count; c++)
            {
                vertices2D[c] = points[c];
            }

            Vector3[] vertices3D = System.Array.ConvertAll<Vector2, Vector3>(vertices2D, v => v);

            // Use the triangulator to get indices for creating triangles
            Triangulator triangulator = new Triangulator(vertices2D);
            int[] indices = triangulator.Triangulate();

            // Create the mesh
            Mesh mesh = new Mesh
            {
                vertices = vertices3D,
                triangles = indices,
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Set up game object with mesh;
            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            meshRenderer.material = new Material(Shader.Find("Sprites/Default"));

            MeshFilter filter = gameObject.GetComponent<MeshFilter>();
            filter.mesh = mesh;
            gameObject.GetComponent<MeshRenderer>().materials[0].color = Color.white;
        }
    }

    public void ClearDrawnLight()
    {
        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshRenderer.material.color = new Color(1,1,1,0);
    }

    /// <summary>
    /// Creates a bounding sphere to simplify calculations in other parts of the code
    /// </summary>
    public void GenerateBoundingSphere()
    {
        //Find the center 
        boundingSphereCenter = Vector2.zero;
        for (int i = 0; i < points.Count; i++)
        {
            boundingSphereCenter += points[i];
        }
        boundingSphereCenter /= points.Count;

        //Find the farthest point from the center
        boundingSphereRadius = Vector2.Distance(points[0], boundingSphereCenter);
        for (int i = 1; i < points.Count; i++)
        {
            float distanceToCheck = Vector2.Distance(points[i], boundingSphereCenter);
            if (boundingSphereRadius < distanceToCheck)
            {
                boundingSphereRadius = distanceToCheck;
            }
        }
    }

    /// <summary>
    /// Makes the points in the light be arranged in a circle
    /// </summary>
    public void Spherize()
    {
        GenerateBoundingSphere();

        float rotNum = (2 * Mathf.PI) / points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            points[i] = boundingSphereCenter + new Vector2(Mathf.Cos((i * rotNum) + (Mathf.PI / 4.0f)) * boundingSphereRadius, Mathf.Sin((i * rotNum) + (Mathf.PI / 4.0f)) * boundingSphereRadius);
        }

        GenerateBoundingSphere();
    }
}
