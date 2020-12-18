using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TileDataCruncher
{
    public int calculationResult = -1;

    public Tile tile;
    private Vector3 tileLocation;
    private Bounds tileBounds;
    private CustomLight _light;

    private ManualResetEvent _doneEvent;

    public TileDataCruncher(Tile _tile, Vector3 _tileLocation, Bounds _tileBounds, CustomLight light, ManualResetEvent doneEvent)
    {
        _doneEvent = doneEvent;
        _light = light;
        tile = _tile;
        tileLocation = _tileLocation;
        tileBounds = _tileBounds;
    }

    public void ThreadPoolCallback(object state)
    {
        calculationResult = 1;
        if (!(Vector2.Distance(_light.center, tileLocation) > _light.boundingSphereRadius + 1))
        {
            Vector2 ExtendedA = new Vector2(tileBounds.center.x + ((tileBounds.min.x - tileBounds.center.x) * 1.15f), tileBounds.center.y + ((tileBounds.min.y - tileBounds.center.y) * 1.15f));
            Vector2 ExtendedB = new Vector2(tileBounds.center.x + ((tileBounds.max.x - tileBounds.center.x) * 1.15f), tileBounds.center.y + ((tileBounds.min.y - tileBounds.center.y) * 1.15f));
            Vector2 ExtendedC = new Vector2(tileBounds.center.x + ((tileBounds.max.x - tileBounds.center.x) * 1.15f), tileBounds.center.y + ((tileBounds.max.y - tileBounds.center.y) * 1.15f));
            Vector2 ExtendedD = new Vector2(tileBounds.center.x + ((tileBounds.min.x - tileBounds.center.x) * 1.15f), tileBounds.center.y + ((tileBounds.max.y - tileBounds.center.y) * 1.15f));
            Vector2 ExtendedE = tileBounds.center - ((tileBounds.center - (Vector3)_light.center).normalized * 0.8f);

            bool p1 = true, p2 = true, p3 = true, p4 = true, p5 = true;

            foreach (Shape s in _light.shapes)
            {
                List<Vector2> pointsToTest = new List<Vector2>();
                if (s.compiledShadowEdges != null && s.functional)
                {
                    foreach (Edge e in s.compiledShadowEdges)
                    {
                        pointsToTest.Add(e.startPoint);
                    }

                    Vector2[] vertices2D = new Vector2[pointsToTest.Count];

                    for (int c = 0; c < pointsToTest.Count; c++)
                    {
                        vertices2D[c] = pointsToTest[c];
                    }

                    // Use the triangulator to get indices for creating triangles
                    Triangulator triangulator = new Triangulator(vertices2D);
                    int[] indices = triangulator.Triangulate();

                    if (p1 && Shape.IsPointInTriangleArrayPreCalcVerts(ExtendedA, vertices2D, indices)) { p1 = false; }
                    if (p2 && Shape.IsPointInTriangleArrayPreCalcVerts(ExtendedB, vertices2D, indices)) { p2 = false; }
                    if (p3 && Shape.IsPointInTriangleArrayPreCalcVerts(ExtendedC, vertices2D, indices)) { p3 = false; }
                    if (p4 && Shape.IsPointInTriangleArrayPreCalcVerts(ExtendedD, vertices2D, indices)) { p4 = false; }
                    if (p5 && Shape.IsPointInTriangleArrayPreCalcVerts(ExtendedE, vertices2D, indices)) { p5 = false; }
                }
            }
            if (p1 || p2 || p3 || p4 || p5)
            {
                calculationResult = 0;
            }
        }
        _doneEvent.Set();
    }
}

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CustomLight : MonoBehaviour
{
    #region Public Variables
    public bool drawShadows = true;
    public bool functional = true;
    const float MARGIN_OF_ERROR = 0.01f;


    private float intesity = 0;
    private Color lightColor = Color.white;

    [SerializeField]
    public List<Shape> shapes;

    [SerializeField]
    public List<Vector2> points;
    public List<Edge> edges;

    public List<Vector2> drawnLight;
    public List<Vector2> bufferedDrawnLight;
    public List<Vector2> intersectPoints;

    [SerializeField]
    public Vector2 center;

    public bool hasChanged = false;

    public Vector2 location;

    public bool boundingSphereVisible = true;
    public float boundingSphereRadius;
    public Vector2 boundingSphereCenter;

    public bool moveAreaOnTransform = true;
    public bool moveCenterOnTransform = true;

    public bool drawGizmos = true;

    public List<Vector2> displayingPointsR;
    public List<Vector2> displayingPointsB;

    private Manager manager;
    #endregion

    #region Properties
    public float Intensity
    {
        get { return intesity; }
        set { Intensity = value; }
    }
    public List<Vector2> Points
    {
        get { return points; }
        set { points = value; }
    }
    public Vector2 Location
    {
        get { return location; }
        set { location = value; }
    }
    public Vector2 Center
    {
        get { return center; }
        set { transform.position = new Vector2(value.x, value.y); }
    }
    public Color LightColor
    {
        get { return lightColor; }
        set { lightColor = value; }
    }
    #endregion

    #region Unity Methods
    void Awake()
    {
        CompileEdgeList();

        GenerateBoundingSphere();

        location = transform.position;

        drawnLight = new List<Vector2>();
        intersectPoints = new List<Vector2>();
        displayingPointsR = new List<Vector2>();
        displayingPointsB = new List<Vector2>();

        Manager.Instance.AssignShapesSpecific(this);
    }

    private void Start()
    {
        manager = Manager.Instance;

        if (points.Count > 0)
        {
            CompileEdgeList();
            PrepareAndBuildLight();
        }
    }

    void Update()
    {
        if (Vector2.Distance(center, (Vector2)transform.parent.position) > MARGIN_OF_ERROR * 10.0f)
        {
            Translate((Vector2)transform.parent.position - center);
        }
    }

    public void UpdateLoop()
    {
        if(Vector2.Distance(center , (Vector2)transform.parent.position) > MARGIN_OF_ERROR * 10.0f)
        {
            Translate((Vector2)transform.parent.position - center);
        }
    }

    private void OnWillRenderObject()
    {
        gameObject.transform.position = Vector3.zero;
    }
    #endregion

    #region Methods

    /// <summary>
    /// Makes edges based on this light's points
    /// </summary>
    public void CompileEdgeList()
    {
        edges = new List<Edge>();

        for (int i = 0; i <= points.Count - 1; i++)
        {
            if (i <= points.Count - 2)
            {
                Edge e = new Edge(points[i], points[i + 1]);
                edges.Add(e);
            }
            else
            {
                Edge e = new Edge(points[i], points[0]);
                edges.Add(e);
            }
        }
    }

    /// <summary>
    /// Makes sure the light is ready before building
    /// </summary>
    public void PrepareAndBuildLight()
    {
        if(manager == null) { manager = Manager.Instance; }
        if (points.Count > 0)
        {
            CompileEdgeList();
            manager.AssignShapesSpecific(this);
            BuildLightForShader();
        }
    }

    public void BuildLightForShader()
    {
        drawnLight = points;
        if (!functional) { drawnLight = new List<Vector2>(); }
        List<Vector4> organizedShadowTriangles = new List<Vector4>();

        int i = 0;
        for (i = 0; i < shapes.Count; i++)
        {
            if (!shapes[i].functional) { continue; }
            shapes[i].BuildShadow(this);
            //Triangulate
            if(shapes[i].compiledShadowPoints == null) { continue; }
            Vector2[] vertices2D = new Vector2[shapes[i].compiledShadowPoints.Count];

            for (int j = 0; j < shapes[i].compiledShadowPoints.Count; j++)
            {
                vertices2D[j] = shapes[i].compiledShadowPoints[j];
            }

            Vector3[] vertices3D = Array.ConvertAll<Vector2, Vector3>(vertices2D, v => v);

            // Use the triangulator to get indices for creating triangles
            Triangulator triangulator = new Triangulator(vertices2D);
            int[] indices = triangulator.Triangulate();

            //Order based on indeces
            for (int j = 0; j < indices.Length && organizedShadowTriangles.Count < 1332; j++)
            {
                organizedShadowTriangles.Add(new Vector4(vertices2D[indices[j]].x, vertices2D[indices[j]].y, 0, 0));
            }
        }

        for (i = organizedShadowTriangles.Count; i < 1332; i++)
        {
            organizedShadowTriangles.Add(new Vector4(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue));
        }

        List<Vector4> organizedBlockingTriangles = new List<Vector4>();

        //Create a list of non-occluded blocking tiles
        List<Tile> visibleTiles = new List<Tile>();

        ManualResetEvent[] doneEvents = new ManualResetEvent[8];
        TileDataCruncher[] tileDataCrunchArray = new TileDataCruncher[8];

        int iterationCount = 0;
        for (i = 0; i < Manager.Instance.LightBlockTiles.Count; i++)
        {
            if (i % doneEvents.Length == 0 && i != 0)
            {
                if (!doneEvents.Any(d => d == null))
                {
                    WaitHandle.WaitAll(doneEvents);
                }
                for (int j = 0; j < doneEvents.Length; j++)
                {
                    if (tileDataCrunchArray[j].calculationResult == 0)
                    {
                        visibleTiles.Add(tileDataCrunchArray[j].tile);
                    }
                }
                iterationCount++;
            }

            doneEvents[i - (iterationCount * doneEvents.Length)] = new ManualResetEvent(false);
            TileDataCruncher tdc = new TileDataCruncher(Manager.Instance.LightBlockTiles[i], Manager.Instance.LightBlockTiles[i].transform.position, Manager.Instance.LightBlockTiles[i].GetComponent<BoxCollider2D>().bounds, this, doneEvents[i - (iterationCount * doneEvents.Length)]);
            tileDataCrunchArray[i - (iterationCount * doneEvents.Length)] = tdc;
            ThreadPool.QueueUserWorkItem(tdc.ThreadPoolCallback, i);
        }
        if (!doneEvents.Any(d => d == null))
        {
            WaitHandle.WaitAll(doneEvents);
        }

        int valuesLeft = doneEvents.Length;
        for(i = 0; i < valuesLeft; i++)
        {
            for (int j = 0; j < valuesLeft; j++)
            {
                if (tileDataCrunchArray[j] != null && tileDataCrunchArray[j].calculationResult == 0)
                {
                    visibleTiles.Add(tileDataCrunchArray[j].tile);
                }
            }
        }

        foreach (Tile t in visibleTiles)
        {
            Bounds tileBounds = t.GetComponent<BoxCollider2D>().bounds;
            Vector2 ExtendedA = new Vector2(tileBounds.center.x + ((tileBounds.min.x - tileBounds.center.x) * 1.0f), tileBounds.center.y + ((tileBounds.min.y - tileBounds.center.y) * 1.0f));
            Vector2 ExtendedB = new Vector2(tileBounds.center.x + ((tileBounds.max.x - tileBounds.center.x) * 1.0f), tileBounds.center.y + ((tileBounds.min.y - tileBounds.center.y) * 1.0f));
            Vector2 ExtendedC = new Vector2(tileBounds.center.x + ((tileBounds.max.x - tileBounds.center.x) * 1.0f), tileBounds.center.y + ((tileBounds.max.y - tileBounds.center.y) * 1.0f));
            Vector2 ExtendedD = new Vector2(tileBounds.center.x + ((tileBounds.min.x - tileBounds.center.x) * 1.0f), tileBounds.center.y + ((tileBounds.max.y - tileBounds.center.y) * 1.0f));

            organizedBlockingTriangles.Add(ExtendedA);
            organizedBlockingTriangles.Add(ExtendedB);
            organizedBlockingTriangles.Add(ExtendedD);

            organizedBlockingTriangles.Add(ExtendedB);
            organizedBlockingTriangles.Add(ExtendedC);
            organizedBlockingTriangles.Add(ExtendedD);
        }

        for (i = organizedBlockingTriangles.Count; i < 1998; i++)
        {
            organizedBlockingTriangles.Add(new Vector4(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue));
        }

        DrawLight(organizedShadowTriangles, organizedBlockingTriangles);
    }

    public void DrawLight(List<Vector4> shadowVertices, List<Vector4> blockingVertices)
    {
        Vector2[] vertices2D = new Vector2[drawnLight.Count];

        for (int c = 0; c < drawnLight.Count; c++)
        {
            vertices2D[c] = drawnLight[c];
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
        meshRenderer.material = new Material(Shader.Find("Unlit/ShadowCutLight"));

        MeshFilter filter = gameObject.GetComponent<MeshFilter>();
        filter.mesh = mesh;
        gameObject.GetComponent<MeshRenderer>().materials[0].color = Color.white;

        //Send the shadow data to the shader
        MaterialPropertyBlock materialProperty = new MaterialPropertyBlock();
        materialProperty.SetVectorArray("_ShadowArrayA", shadowVertices.GetRange(0,666));
        materialProperty.SetVectorArray("_ShadowArrayB", shadowVertices.GetRange(666, 666));
        materialProperty.SetVectorArray("_blockingArrayA", blockingVertices.GetRange(0, 666));
        materialProperty.SetVectorArray("_blockingArrayB", blockingVertices.GetRange(666, 666));
        materialProperty.SetVectorArray("_blockingArrayC", blockingVertices.GetRange(1332, 666));
        gameObject.GetComponent<Renderer>().SetPropertyBlock(materialProperty);

        gameObject.transform.position = Vector3.zero;
    }

    #region Shape Dynamics

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
            points[i] = boundingSphereCenter + new Vector2(Mathf.Cos(i * rotNum) * boundingSphereRadius, Mathf.Sin(i * rotNum) * boundingSphereRadius);
        }
        
        GenerateBoundingSphere();
        
        PrepareAndBuildLight();
    }

    /// <summary>
    /// Will translate the light's points to surround the bounding sphere's center
    /// </summary>
    public void FocusPointsAboutCenter()
    {
        GenerateBoundingSphere();
        
        Vector2 vectorChange = new Vector2(transform.position.x, transform.position.y) - boundingSphereCenter;
        for (int i = 0; i < points.Count; i++)
        {
            points[i] += vectorChange;
        }
        
        location += vectorChange;
        
        GenerateBoundingSphere();
        
        PrepareAndBuildLight();
    }

    /// <summary>
    /// Will move the light and all of its points
    /// </summary>
    /// <param name="translation">Distance to move</param>
    public void Translate(Vector2 translation)
    {
        if (moveAreaOnTransform)
        {
            for (int i = 0; i < points.Count; i++)
            {
                points[i] += translation;
            }
            GenerateBoundingSphere();
        }

        if (moveCenterOnTransform)
        {
            center = boundingSphereCenter;
        }

        if (Vector2.Distance(center, (Vector2)transform.parent.position) > MARGIN_OF_ERROR * 10.0f)
        {
            TranslateWithoutBuilding((Vector2)transform.parent.position - center);
        }

        CompileEdgeList();
        GenerateBoundingSphere();
        PrepareAndBuildLight();
    }

    /// <summary>
    /// Will translate the light but will not rebuild the light's mesh
    /// </summary>
    /// <param name="translation">Distance to move</param>
    public void TranslateWithoutBuilding(Vector2 translation)
    {
        if (moveAreaOnTransform)
        {
            for (int i = 0; i < points.Count; i++)
            {
                points[i] += translation;
            }
            GenerateBoundingSphere();
        }

        if (moveCenterOnTransform)
        {
            center = boundingSphereCenter;
        }

        CompileEdgeList();
        GenerateBoundingSphere();
    }

    /// <summary>
    /// Rotate light's points about 'a' point by 'c' degrees
    /// </summary>
    /// <param name="a">Point to rotate about</param>
    /// <param name="c">Degrees to rotate by</param>
    public void Rotate(Vector2 a, float c) 
    {
        //Vectors from 'a' to point
        for (int v = 0; v < points.Count; v++)
        {
            Vector2 x = points[v] - a;

            //new vectors after rotating by c degrees
            x = new Vector2(x.x * Mathf.Cos(c * Mathf.Deg2Rad) - x.y * Mathf.Sin(c * Mathf.Deg2Rad), x.x * Mathf.Sin(c * Mathf.Deg2Rad) + x.y * Mathf.Cos(c * Mathf.Deg2Rad));

            //Set the new points created by the rotated vector
            points[v] = a + x;
        }
        transform.Rotate(new Vector3(0.0f, 0.0f, c));

        PrepareAndBuildLight();
    }

    public List<Vector2> Rotate(List<Vector2> vectors, Vector2 a, float c) 
    {
        //Vectors from 'a' to point
        for (int v = 0; v < vectors.Count; v++)
        {
            Vector2 x = vectors[v] - a;

            //new vectors after rotating by c degrees
            x = new Vector2(x.x * Mathf.Cos(c * Mathf.Deg2Rad) - x.y * Mathf.Sin(c * Mathf.Deg2Rad), x.x * Mathf.Sin(c * Mathf.Deg2Rad) + x.y * Mathf.Cos(c * Mathf.Deg2Rad));

            //Set the new points created by the rotated vector
            vectors[v] = a + x;
        }
        PrepareAndBuildLight();

        return vectors;
    }

    public Vector2[] Rotate(Vector2[] vectors, Vector2 a, float c) 
    {
        //Vectors from 'a' to point
        for (int v = 0; v < vectors.Length; v++)
        {
            Vector2 x = vectors[v] - a;

            //new vectors after rotating by c degrees
            x = new Vector2(x.x * Mathf.Cos(c * Mathf.Deg2Rad) - x.y * Mathf.Sin(c * Mathf.Deg2Rad), x.x * Mathf.Sin(c * Mathf.Deg2Rad) + x.y * Mathf.Cos(c * Mathf.Deg2Rad));

            //Set the new points created by the rotated vector
            vectors[v] = a + x;
        }
        PrepareAndBuildLight();

        return vectors;
    }

    public Vector3[] Rotate(Vector3[] vectors, Vector3 a, float c) 
    {
        //Vectors from 'a' to point
        for (int v = 0; v < vectors.Length; v++)
        {
            Vector3 x = vectors[v] - a;

            //new vectors after rotating by c degrees
            x = new Vector3(x.x * Mathf.Cos(c * Mathf.Deg2Rad) - x.y * Mathf.Sin(c * Mathf.Deg2Rad), x.x * Mathf.Sin(c * Mathf.Deg2Rad) + x.y * Mathf.Cos(c * Mathf.Deg2Rad), 0.0f);

            //Set the new points created by the rotated vector
            vectors[v] = a + x;
        }
        PrepareAndBuildLight();

        return vectors;
    }

    /// <summary>
    /// Scale light's points about 'a' point by 'c' length (1.0f = no scale change)
    /// </summary>
    /// <param name="a">Point to expand from</param>
    /// <param name="c">Proportion to scale by</param>
    public void Scale(Vector2 a, float c) 
    {
        //Vectors from 'a' to point
        for (int v = 0; v < points.Count; v++)
        {
            Vector2 l = points[v] - a;

            //Set the new points created by the scaled vector
            points[v] = a + (l * c);
        }
        GenerateBoundingSphere();
        center = boundingSphereCenter;
        CompileEdgeList();
        PrepareAndBuildLight();
    }

    #endregion

    #endregion
}