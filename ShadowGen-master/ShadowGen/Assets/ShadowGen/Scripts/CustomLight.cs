using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CustomLight : MonoBehaviour
{
    public bool drawShadows = false;
    const float MARGIN_OF_ERROR = 0.01f;

    private float intesity;
    private Color lightColor;

    [SerializeField]
    public List<Shape> shapes;

    [SerializeField]
    public List<Vector2> points;
    public List<Edge> edges;

    public List<Vector2> drawnLight;
    public List<Vector2> intersectPoints;

    [SerializeField]
    public Vector2 centerPrevious;
    public Vector2 center;

    public Vector2 location;
    public Vector2 lastLocation;
    public float rotation;
    public float lastRotation;
    public float scale;
    public float lastScale;

    public bool boundingSphereVisible = true;
    public float boundingSphereRadius;
    public Vector2 boundingSphereCenter;

    public bool moveAreaOnTransform = true;
    public bool moveCenterOnTransform = true;

    private Manager manager;

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
    public Vector2 LastLocation
    {
        get { return lastLocation; }
        set { lastLocation = value; }
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
    // Start is called before the first frame update
    void Awake()
    {
        CompileEdgeList();

        GenerateBoundingSphere();

        location = transform.position;
        lastLocation = location;

        drawnLight = new List<Vector2>();
        intersectPoints = new List<Vector2>();
    }

    private void Start()
    {
        manager = Manager.Instance;

        if (points.Count > 0)
        {
            CompileEdgeList();
            BuildLight();
        }
    }

    private void OnDrawGizmos()
    {
        if(points == null)
        {
            points = new List<Vector2>();
        }
        if (drawnLight == null)
        {
            drawnLight = new List<Vector2>();
        }

        for (int i = 0; i < points.Count; i++)
        {
            //Draw Edges
            Gizmos.color = Color.cyan;
            if (i != points.Count - 1) { Gizmos.DrawLine(points[i], points[i + 1]); }
            else { Gizmos.DrawLine(points[i], points[0]); }

            //Draw Points (Vertices)
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(points[i], 0.15f);
        }

        foreach (Vector2 v in drawnLight)
        {
            //Draw the points that create the drawn shape
            Gizmos.color = new Color(0.35f,0.35f,0.60f,0.85f);
            Gizmos.DrawSphere(v, 0.20f);
        }

        if (boundingSphereVisible)
        {
            Gizmos.color = Color.blue;
            //Draw the bounding sphere
            List<Vector2> spherePoints = new List<Vector2>();
            float rotNum = (2 * Mathf.PI) / 32;
            for (int i = 0; i < 32; i++)
            {
                spherePoints.Add(boundingSphereCenter + new Vector2(Mathf.Cos(i * rotNum) * boundingSphereRadius, Mathf.Sin(i * rotNum) * boundingSphereRadius));
            }
            for (int i = 0; i < spherePoints.Count; i++)
            {
                if (i == spherePoints.Count - 1)
                {
                    Gizmos.DrawLine(spherePoints[i], spherePoints[0]);
                }
                else
                {
                    Gizmos.DrawLine(spherePoints[i], spherePoints[i + 1]);
                }
            }
            Gizmos.DrawSphere(boundingSphereCenter, 0.30f);
        }

        //Draw the center of the light
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(center, 0.25f);
    }

    // Update is called once per frame
    void Update()
    {
        lastLocation = location;
        location = transform.position;

        bool generateNewLightArea = false;

        //Check if this light has moved
        if (location != lastLocation)
        {
            //Add the movement of the light to its points
            for (int i = 0; i < points.Count; i++)
            {
                points[i] -= lastLocation - location;
            }
        }

        //A new light area will only be generated if something has changed
        if (location != lastLocation || centerPrevious != center || rotation != lastRotation || scale != lastScale) { generateNewLightArea = true; }
        foreach (Shape s in shapes)
        {
            if (s.Location != s.LastLocation || s.rotation != s.lastRotation || s.scale != s.lastScale) { generateNewLightArea = true; }
        }
        if (generateNewLightArea)
        {
            if (points.Count > 0)
            {
                CompileEdgeList();
                BuildLight();
            }
        }
    }
    #endregion

    #region Methods

    public void BuildLight()
    {
        //The points that are currently being worked on / added to
        List<Edge> lightAreaDivision;

        //Handle each shadow this light is affecting seperatly, build up a light area as you deal with each one
        foreach (Shape s in shapes) //Each object that will cast a shadow
        {
            s.BuildShadow(this);

            if(s.compiledShadowEdges == null)
            {
                break;
            }

            lightAreaDivision = new List<Edge>();

            bool countUp = true; //Direction that we should go on the current line
            bool lineType = true; //true = light line
            bool completed = false; //The current loop is done and we can go to the next shape

            int loopLimiter = 0; //Prevents being stuck in the while loop

            #region Find Intersection Points And Starting Edge
            //All of the points where the shadow and the light intersect
            intersectPoints = new List<Vector2>();

            //These list keeps track of the edges that intersection points are on
            List<Edge> intersectPointsPlacement = new List<Edge>();

            //The points that are adjacent to the intersection point on the light line
            List<Edge> lightPointsUp = new List<Edge>();
            List<Edge> lightPointsDown = new List<Edge>();

            for (int i = 0; i < s.compiledShadowEdges.Count; i++)
            {
                for (int c = 0; c < edges.Count; c++)
                {
                    if (s.compiledShadowEdges[i].EdgeIntersection(edges[c]))
                    {
                        intersectPoints.Add(s.compiledShadowEdges[i].EdgeIntersectionPoint(edges[c]));
                        intersectPointsPlacement.Add(edges[c]);                  
                    }
                }
            }

            List<Vector2> intersectPointsTemporary = new List<Vector2>();
            List<Edge> intersectPointsPlacementTemporary = new List<Edge>(); ;

            //Check if two of the intersection points are along the same shadow line (center -> point have same slope)
            if (intersectPoints.Count > 2)
            {
                Vector2 slopeA = Vector2.zero;
                Vector2 slopeB = Vector2.zero;
                for (int i = 0; i < intersectPoints.Count; i++)
                {
                    for (int c = i; c < intersectPoints.Count; c++)
                    {
                        if(i != c)
                        {
                            slopeA = (intersectPoints[i] - center).normalized;
                            slopeB = (intersectPoints[c] - center).normalized;
                            if (Vector2.Distance(slopeA, slopeB) <= MARGIN_OF_ERROR * 2.0f)
                            {
                                intersectPointsTemporary.Add(intersectPoints[c]);
                                intersectPointsPlacementTemporary.Add(intersectPointsPlacement[c]);
                            }
                        }
                    }
                }
                foreach (Vector2 v in intersectPointsTemporary)
                {
                    intersectPoints.Remove(v);
                }
                foreach (Edge e in intersectPointsPlacementTemporary)
                {
                    intersectPointsPlacement.Remove(e);
                }
            }

            //Make sure each intersection point is actually on the light line

            intersectPointsTemporary = intersectPoints;

            //If there are no intersections, there is nothing to do and we must exit
            //If this is reached, there was probably an error
            if (intersectPoints.Count == 0)
            {
                break;
            }

            lightPointsUp.Add(new Edge(intersectPoints[0], intersectPointsPlacement[0].endPoint)); //End point in second slot counts up
            lightPointsDown.Add(new Edge(intersectPoints[0], intersectPointsPlacement[0].startPoint, true)); //Start point in second slot counts down

            //if the closest point is pretty much a light point, change the light points
            for (int c = 0; c < edges.Count; c++)
            {
                if (Vector2.Distance(edges[c].startPoint, intersectPoints[0]) < MARGIN_OF_ERROR * 2.0f)
                {
                    lightPointsUp.RemoveAt(0);
                    lightPointsUp.Add(new Edge(intersectPoints[0], edges[c].endPoint));
                }
                if (Vector2.Distance(edges[c].endPoint, intersectPoints[0]) < MARGIN_OF_ERROR * 2.0f)
                {
                    lightPointsDown.RemoveAt(0);
                    lightPointsDown.Add(new Edge(intersectPoints[0], edges[c].startPoint));
                }
            }

            //Edges from the light's center to the intersection-adjacent points on the light line
            Edge centerToDownPoint = new Edge(center, lightPointsDown[0].endPoint);
            Edge centerToUpPoint = new Edge(center, lightPointsUp[0].endPoint);

            //These two points are very close to the intersection point
            //Whichever point is NOT in the shadow determines the direction to loop
            Vector2 testUpPoint = intersectPoints[0] + ((centerToUpPoint.endPoint - intersectPoints[0]).normalized * 0.15f);
            Vector2 testDownPoint = intersectPoints[0] + ((centerToDownPoint.endPoint - intersectPoints[0]).normalized * 0.15f);

            //Check if testDownPoint is within the triangle
            if (Shape.IsPointInTriangleArray(testDownPoint, s.compiledShadowPoints))
            {
                lightAreaDivision.Add(lightPointsUp[0]);
                countUp = true;
            }
            //Check if testUpPoint is within the triangle
            if (Shape.IsPointInTriangleArray(testUpPoint, s.compiledShadowPoints))
            {
                lightAreaDivision.Add(lightPointsDown[0]);
                countUp = false;
            }

            //Most likely an error is this is being hit
            if (lightAreaDivision.Count == 0 || lightAreaDivision.Count == 2)
            {
                lightAreaDivision = new List<Edge>();
                //Try again with a higher testing range

                //These two points are very close to the intersection point
                //Whichever point is NOT in the shadow determines the direction to loop
                testUpPoint = intersectPoints[0] + ((centerToUpPoint.endPoint - intersectPoints[0]).normalized * 1.25f);
                testDownPoint = intersectPoints[0] + ((centerToDownPoint.endPoint - intersectPoints[0]).normalized * 1.25f);

                //Check if testDownPoint is within the triangle
                if (Shape.IsPointInTriangleArray(testDownPoint, s.compiledShadowPoints))
                {
                    lightAreaDivision.Add(lightPointsUp[0]);
                    countUp = true;
                }
                //Check if testUpPoint is within the triangle
                if (Shape.IsPointInTriangleArray(testUpPoint, s.compiledShadowPoints))
                {
                    lightAreaDivision.Add(lightPointsDown[0]);
                    countUp = false;
                }
                if (lightAreaDivision.Count == 0)
                {
                    break;
                }
            }
            #endregion

            bool mustIntersect = false;

            if(lightAreaDivision.Count == 2)
            {
                lightAreaDivision.RemoveAt(0);
            }

            #region Loop Until The Light Area Edge List is Complete
            //Loop around the shadow and light, building a light area
            while (!completed)
            {
                #region Special check for if an intersection point and a light area point share the same coordinates
                if (mustIntersect)
                {
                    lightAreaDivision.RemoveAt(lightAreaDivision.Count - 1);
                    lightAreaDivision[lightAreaDivision.Count - 1].endPoint = lightAreaDivision[lightAreaDivision.Count - 1].endPoint + ((lightAreaDivision[lightAreaDivision.Count - 1].endPoint - lightAreaDivision[lightAreaDivision.Count - 1].startPoint) * 1.5f);
                }

                mustIntersect = false;
                //Special case where the last point is an intersection point
                for (int i = 0; i < intersectPoints.Count; i++)
                {
                    if (Vector2.Distance(intersectPoints[i], lightAreaDivision[lightAreaDivision.Count - 1].endPoint) < MARGIN_OF_ERROR)
                    {
                        mustIntersect = true;
                    }
                }
                #endregion

                if (lineType) //Your a light line looking for a shadow line
                {
                    bool proceed = true;
                    if (!countUp) //If you are counting down the light line (last end , previous start)
                    {
                        if (lightAreaDivision.Count != 1)
                        {
                            for (int i = 0; i < s.compiledShadowEdges.Count; i++)
                            {
                                if (s.compiledShadowEdges[i].EdgeIntersection(lightAreaDivision[lightAreaDivision.Count - 1]))
                                {
                                    mustIntersect = false;
                                    //Check if the intersection point is a point on the light shape
                                    for (int c = 0; c < points.Count;c++)
                                    {
                                        if(Vector2.Distance(points[c], s.compiledShadowEdges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1])) < MARGIN_OF_ERROR)
                                        {

                                        }
                                    }

                                    //Check if there are more than one intersection points
                                    for (int x = 0; x < s.compiledShadowEdges.Count; x++)
                                    {
                                        if (x != i)
                                        {
                                            if (s.compiledShadowEdges[x].EdgeIntersection(lightAreaDivision[lightAreaDivision.Count - 1]))
                                            {
                                                if (
                                                    Mathf.Abs(Vector2.Distance(s.compiledShadowEdges[x].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]),
                                                    lightAreaDivision[lightAreaDivision.Count - 1].startPoint))
                                                    <
                                                    Mathf.Abs(Vector2.Distance(s.compiledShadowEdges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]),
                                                    lightAreaDivision[lightAreaDivision.Count - 1].startPoint))
                                                    && Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].startPoint, s.compiledShadowEdges[x].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1])) > MARGIN_OF_ERROR)
                                                {
                                                    i = x;
                                                }
                                            }
                                        }
                                    }
                                    Vector2 lineIntersection = s.compiledShadowEdges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]);

                                    //Select closest intersection point
                                    Vector2 closestIntersection = new Vector2(int.MaxValue, int.MaxValue);
                                    bool properIntersection = false;
                                    for (int c = 0; c < intersectPoints.Count; c++)
                                    {
                                        if (Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].startPoint, intersectPoints[c]) > MARGIN_OF_ERROR)
                                        {
                                            if (Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].startPoint, intersectPoints[c]) <= Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].startPoint, closestIntersection))
                                            {
                                                Vector2 slopeA = (lightAreaDivision[lightAreaDivision.Count - 1].startPoint - lightAreaDivision[lightAreaDivision.Count - 1].endPoint).normalized;
                                                Vector2 slopeB = (lightAreaDivision[lightAreaDivision.Count - 1].startPoint - intersectPoints[c]).normalized;
                                                if (Vector2.Distance(slopeA, slopeB) <= 0.1f)
                                                {
                                                    closestIntersection = intersectPoints[c];
                                                    properIntersection = true;
                                                }
                                            }
                                        }
                                    }
                                    if (!properIntersection) { continue; }

                                    //Change the endpoint before switching lines
                                    lightAreaDivision[lightAreaDivision.Count - 1].endPoint = closestIntersection;

                                    //If the first and last point are the same, then we have done a full loop and are done with this shape
                                    if (Mathf.Abs(Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].endPoint, lightAreaDivision[0].startPoint)) <= MARGIN_OF_ERROR) { completed = true; proceed = false; break; }

                                    //Switching to shadow, which way should we go
                                    //Find which adjacent point is in the light area

                                    List<Edge> shadowPointsUp = new List<Edge>();
                                    List<Edge> shadowPointsDown = new List<Edge>();

                                    //Find direction on light to go

                                    //Find the line up and down
                                    if (intersectPoints.Count > 0)
                                    {
                                        shadowPointsUp.Add(new Edge(s.compiledShadowEdges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]), s.compiledShadowEdges[i].endPoint)); //End point in second slot counts up
                                        shadowPointsDown.Add(new Edge(s.compiledShadowEdges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]), s.compiledShadowEdges[i].startPoint, true)); //Start point in second slot counts down
                                    }

                                    //if the closest point is pretty much a shadow point, change the shadow points
                                    for (int c = 0; c < s.compiledShadowEdges.Count; c++)
                                    {
                                        if (Vector2.Distance(s.compiledShadowEdges[c].startPoint, closestIntersection) < MARGIN_OF_ERROR * 2.0f)
                                        {
                                            shadowPointsUp.RemoveAt(0);
                                            shadowPointsUp.Add(new Edge(s.compiledShadowEdges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]), s.compiledShadowEdges[c].endPoint));
                                        }
                                        if (Vector2.Distance(s.compiledShadowEdges[c].endPoint, closestIntersection) < MARGIN_OF_ERROR * 2.0f)
                                        {
                                            shadowPointsDown.RemoveAt(0);
                                            shadowPointsDown.Add(new Edge(s.compiledShadowEdges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]), s.compiledShadowEdges[c].startPoint));
                                        }
                                    }

                                    centerToDownPoint = new Edge(center, shadowPointsDown[0].endPoint);
                                    centerToUpPoint = new Edge(center, shadowPointsUp[0].endPoint);

                                    testUpPoint = closestIntersection + ((centerToUpPoint.endPoint - closestIntersection).normalized * 0.15f);
                                    testDownPoint = closestIntersection + ((centerToDownPoint.endPoint - closestIntersection).normalized * 0.15f);

                                    List<Vector2> testPoints = new List<Vector2>();
                                    foreach (Edge e in edges)
                                    {
                                        testPoints.Add(new Vector2(e.startPoint.x, e.startPoint.y));
                                    }

                                    int lightCountBefore = lightAreaDivision.Count;

                                    //Check if testDownPoint is within the triangle
                                    if (Shape.IsPointInTriangleArray(testUpPoint, testPoints))
                                    {
                                        lightAreaDivision.Add(shadowPointsUp[0]);
                                        countUp = true;
                                    }
                                    //Check if testUpPoint is within the triangle
                                    if (Shape.IsPointInTriangleArray(testDownPoint, testPoints))
                                    {
                                        lightAreaDivision.Add(shadowPointsDown[0]);
                                        countUp = false;
                                    }

                                    //If something went wrong while finding direction, keep testing
                                    float testDifference = 0.25f;
                                    while (lightCountBefore - lightAreaDivision.Count == -2)
                                    {
                                        lightAreaDivision.RemoveAt(lightAreaDivision.Count - 1);
                                        lightAreaDivision.RemoveAt(lightAreaDivision.Count - 1);

                                        testUpPoint = closestIntersection + ((centerToUpPoint.endPoint - closestIntersection).normalized * testDifference);
                                        testDownPoint = closestIntersection + ((centerToDownPoint.endPoint - closestIntersection).normalized * testDifference);

                                        //Check if testDownPoint is within the triangle
                                        if (Shape.IsPointInTriangleArray(testUpPoint, testPoints))
                                        {
                                            lightAreaDivision.Add(shadowPointsUp[0]);
                                            countUp = true;
                                        }
                                        //Check if testUpPoint is within the triangle
                                        if (Shape.IsPointInTriangleArray(testDownPoint, testPoints))
                                        {
                                            lightAreaDivision.Add(shadowPointsDown[0]);
                                            countUp = false;
                                        }

                                        testDifference += 0.25f;
                                        if (testDifference > 10.0f)
                                        {
                                            break;
                                        }
                                    }

                                    lineType = !lineType;
                                    break;
                                }
                            }
                        }
                        if (proceed) //We are just continuing to follow the same loop
                        {
                            for (int i = 0; i < edges.Count; i++) //Counting down
                            {
                                //Find the next point on the loop
                                if (Mathf.Abs(Vector2.Distance(edges[i].endPoint, lightAreaDivision[lightAreaDivision.Count - 1].endPoint)) < MARGIN_OF_ERROR && Mathf.Abs(Vector2.Distance(edges[i].endPoint, edges[i].startPoint)) > MARGIN_OF_ERROR)
                                {
                                    if (Mathf.Abs(Vector2.Distance(edges[i].endPoint, edges[i].startPoint)) > MARGIN_OF_ERROR)
                                    {
                                        lightAreaDivision.Add(new Edge(lightAreaDivision[lightAreaDivision.Count - 1].endPoint, edges[i].startPoint));
                                        break;
                                    }
                                    else
                                    {
                                        Debug.Log("Edge with length ~0 detected");
                                    }
                                }
                            }
                        }
                    }
                    else //Counting up light line
                    {
                        if (lightAreaDivision.Count != 1)
                        {
                            for (int i = 0; i < s.compiledShadowEdges.Count; i++)
                            {
                                if (s.compiledShadowEdges[i].EdgeIntersection(lightAreaDivision[lightAreaDivision.Count - 1]))
                                {
                                    mustIntersect = false;
                                    //Check if there are more than one intersection points
                                    for (int x = 0; x < s.compiledShadowEdges.Count; x++)
                                    {
                                        if (x != i)
                                        {
                                            if (s.compiledShadowEdges[x].EdgeIntersection(lightAreaDivision[lightAreaDivision.Count - 1]))
                                            {
                                                if (
                                                    Mathf.Abs(Vector2.Distance(s.compiledShadowEdges[x].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]),
                                                    lightAreaDivision[lightAreaDivision.Count - 1].startPoint))
                                                    <
                                                    Mathf.Abs(Vector2.Distance(s.compiledShadowEdges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]),
                                                    lightAreaDivision[lightAreaDivision.Count - 1].startPoint))
                                                    && Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].startPoint, s.compiledShadowEdges[x].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1])) > MARGIN_OF_ERROR)
                                                {
                                                    i = x;
                                                }
                                            }
                                        }
                                    }
                                    Vector2 lineIntersection = s.compiledShadowEdges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]);

                                    //Select closest intersection point
                                    Vector2 closestIntersection = new Vector2(int.MaxValue, int.MaxValue);
                                    bool properIntersection = false;
                                    for (int c = 0; c < intersectPoints.Count; c++)
                                    {
                                        if (Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].startPoint, intersectPoints[c]) > MARGIN_OF_ERROR)
                                        {
                                            if (Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].startPoint, intersectPoints[c]) <= Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].startPoint, closestIntersection))
                                            {
                                                Vector2 slopeA = (lightAreaDivision[lightAreaDivision.Count - 1].startPoint - lightAreaDivision[lightAreaDivision.Count - 1].endPoint).normalized;
                                                Vector2 slopeB = (lightAreaDivision[lightAreaDivision.Count - 1].startPoint - intersectPoints[c]).normalized;
                                                if (Vector2.Distance(slopeA, slopeB) <= 0.1f)
                                                {
                                                    closestIntersection = intersectPoints[c];
                                                    properIntersection = true;
                                                }
                                            }
                                        }
                                    }
                                    if (!properIntersection) { continue; }

                                    //Change the endpoint before switching lines
                                    lightAreaDivision[lightAreaDivision.Count - 1].endPoint = closestIntersection;

                                    //If the first and last point are the same, then we have done a full loop and are done with this shape
                                    if (Mathf.Abs(Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].endPoint, lightAreaDivision[0].startPoint)) <= MARGIN_OF_ERROR) { completed = true; proceed = false; break; }

                                    //Switching to shadow, which way should we go
                                    //Find which adjacent point is in the light area

                                    List<Edge> shadowPointsUp = new List<Edge>();
                                    List<Edge> shadowPointsDown = new List<Edge>();

                                    //Find direction on light to go
                                    //Find the line up and down
                                    if (intersectPoints.Count > 0)
                                    {
                                        shadowPointsUp.Add(new Edge(s.compiledShadowEdges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]), s.compiledShadowEdges[i].endPoint)); //End point in second slot counts up
                                        shadowPointsDown.Add(new Edge(s.compiledShadowEdges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]), s.compiledShadowEdges[i].startPoint, true)); //Start point in second slot counts down
                                    }

                                    //if the closest point is pretty much a shadow point, change the shadow points
                                    for(int c = 0; c < s.compiledShadowEdges.Count; c++)
                                    {
                                        if(Vector2.Distance(s.compiledShadowEdges[c].startPoint, closestIntersection) < MARGIN_OF_ERROR * 2.0f)
                                        {
                                            shadowPointsUp.RemoveAt(0);
                                            shadowPointsUp.Add(new Edge(s.compiledShadowEdges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]), s.compiledShadowEdges[c].endPoint));
                                        }
                                        if (Vector2.Distance(s.compiledShadowEdges[c].endPoint, closestIntersection) < MARGIN_OF_ERROR * 2.0f)
                                        {
                                            shadowPointsDown.RemoveAt(0);
                                            shadowPointsDown.Add(new Edge(s.compiledShadowEdges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]), s.compiledShadowEdges[c].startPoint));
                                        }
                                    }

                                    centerToDownPoint = new Edge(center, shadowPointsDown[0].endPoint);
                                    centerToUpPoint = new Edge(center, shadowPointsUp[0].endPoint);

                                    testUpPoint = closestIntersection + ((centerToUpPoint.endPoint - closestIntersection).normalized * 0.15f);
                                    testDownPoint = closestIntersection + ((centerToDownPoint.endPoint - closestIntersection).normalized * 0.15f);

                                    List<Vector2> testPoints = new List<Vector2>();
                                    foreach(Edge e in edges)
                                    {
                                        testPoints.Add(new Vector2(e.startPoint.x, e.startPoint.y));
                                    }

                                    int lightCountBefore = lightAreaDivision.Count;

                                    //Check if testDownPoint is within the triangle
                                    if (Shape.IsPointInTriangleArray(testUpPoint, testPoints))
                                    {
                                        lightAreaDivision.Add(shadowPointsUp[0]);
                                        countUp = true;
                                    }
                                    //Check if testUpPoint is within the triangle
                                    if (Shape.IsPointInTriangleArray(testDownPoint, testPoints))
                                    {
                                        lightAreaDivision.Add(shadowPointsDown[0]);
                                        countUp = false;
                                    }

                                    float testDifference = 0.25f;

                                    while(lightCountBefore - lightAreaDivision.Count == -2 || lightCountBefore - lightAreaDivision.Count == 0)
                                    {
                                        lightAreaDivision.RemoveAt(lightAreaDivision.Count - 1);
                                        lightAreaDivision.RemoveAt(lightAreaDivision.Count - 1);

                                        testUpPoint = closestIntersection + ((centerToUpPoint.endPoint - closestIntersection).normalized * testDifference);
                                        testDownPoint = closestIntersection + ((centerToDownPoint.endPoint - closestIntersection).normalized * testDifference);

                                        //Check if testDownPoint is within the triangle
                                        if (Shape.IsPointInTriangleArray(testUpPoint, testPoints))
                                        {
                                            lightAreaDivision.Add(shadowPointsUp[0]);
                                            countUp = true;
                                        }
                                        //Check if testUpPoint is within the triangle
                                        if (Shape.IsPointInTriangleArray(testDownPoint, testPoints))
                                        {
                                            lightAreaDivision.Add(shadowPointsDown[0]);
                                            countUp = false;
                                        }

                                        testDifference += 0.25f;
                                        if(testDifference > 10.0f)
                                        {
                                            break;
                                        }
                                    }

                                    lineType = !lineType;
                                    proceed = false;
                                    break;
                                }
                            }
                        }
                        if (proceed)
                        { 
                            //Counting up
                            for (int i = 0; i < edges.Count; i++)
                            {
                                if (Mathf.Abs(Vector2.Distance(edges[i].startPoint, lightAreaDivision[lightAreaDivision.Count - 1].endPoint)) < MARGIN_OF_ERROR)
                                {
                                    if(Mathf.Abs(Vector2.Distance(edges[i].endPoint, edges[i].startPoint)) > MARGIN_OF_ERROR)
                                    {
                                        lightAreaDivision.Add(new Edge(lightAreaDivision[lightAreaDivision.Count - 1].endPoint, edges[i].endPoint));
                                        break;
                                    }
                                    else
                                    {
                                        Debug.Log("Edge with length ~0 detected");
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    bool proceed = true;
                    if (!countUp)
                    {
                        if (lightAreaDivision.Count != 1)
                        {
                            for (int i = 0; i < edges.Count; i++)
                            {
                                if (lightAreaDivision[lightAreaDivision.Count - 1].EdgeIntersection(edges[i]))
                                {
                                    mustIntersect = false;

                                    //Check that the intersection point has actually been found
                                    Vector2 lineIntersection = edges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]);

                                    //Select closest intersection point
                                    Vector2 closestIntersection = new Vector2(int.MaxValue, int.MaxValue);
                                    bool properIntersection = false;
                                    for (int c = 0; c < intersectPoints.Count; c++)
                                    {
                                        if (Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].startPoint, intersectPoints[c]) > MARGIN_OF_ERROR)
                                        {
                                            if (Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].startPoint, intersectPoints[c]) <= Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].startPoint, closestIntersection))
                                            {
                                                Vector2 slopeA = (lightAreaDivision[lightAreaDivision.Count - 1].startPoint - lightAreaDivision[lightAreaDivision.Count - 1].endPoint).normalized;
                                                Vector2 slopeB = (lightAreaDivision[lightAreaDivision.Count - 1].startPoint - intersectPoints[c]).normalized;
                                                if (Vector2.Distance(slopeA, slopeB) <= MARGIN_OF_ERROR * 2.0f)
                                                {
                                                    closestIntersection = intersectPoints[c];
                                                    properIntersection = true;
                                                }
                                            }
                                        }
                                    }
                                    if (!properIntersection) { continue; }

                                    lightAreaDivision[lightAreaDivision.Count - 1].endPoint = closestIntersection;

                                    if (Mathf.Abs(Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].endPoint, lightAreaDivision[0].startPoint)) <= MARGIN_OF_ERROR) { completed = true; proceed = false; break; }

                                    lightPointsUp = new List<Edge>();
                                    lightPointsDown = new List<Edge>();

                                    int index = 1;
                                    for(int x = 0; x < intersectPoints.Count; x++)
                                    {
                                        if(Mathf.Abs(Vector2.Distance(intersectPoints[x], closestIntersection)) <= MARGIN_OF_ERROR)
                                        {
                                            index = x;
                                        }
                                    }

                                    //Find direction on light to go
                                    if (intersectPoints.Count > 0)
                                    {
                                        lightPointsUp.Add(new Edge(edges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]), intersectPointsPlacement[index].endPoint)); //End point in second slot counts up
                                        lightPointsDown.Add(new Edge(edges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]), intersectPointsPlacement[index].startPoint, true)); //Start point in second slot counts down
                                    }

                                    centerToDownPoint = new Edge(center, lightPointsDown[0].endPoint);
                                    centerToUpPoint = new Edge(center, lightPointsUp[0].endPoint);

                                    testUpPoint = closestIntersection + ((centerToUpPoint.endPoint - closestIntersection) * 0.05f);
                                    testDownPoint = closestIntersection + ((centerToDownPoint.endPoint - closestIntersection) * 0.05f);

                                    //Check if testDownPoint is within the triangle
                                    if (Shape.IsPointInTriangleArray(testDownPoint, s.compiledShadowPoints))
                                    {
                                        lightAreaDivision.Add(lightPointsUp[0]);
                                        countUp = true;
                                    }

                                    //Check if testUpPoint is within the triangle
                                    if (Shape.IsPointInTriangleArray(testUpPoint, s.compiledShadowPoints))
                                    {
                                        lightAreaDivision.Add(lightPointsDown[0]);
                                        countUp = false;
                                    }

                                    lineType = !lineType;
                                    proceed = false;
                                    break;
                                }
                            }
                        }
                        if (proceed)
                        {
                            for (int i = 0; i < s.compiledShadowEdges.Count; i++) //Counting down
                            {
                                if (s.compiledShadowEdges[i].endPoint == lightAreaDivision[lightAreaDivision.Count - 1].endPoint)
                                {
                                    if (Mathf.Abs(Vector2.Distance(s.compiledShadowEdges[i].endPoint, s.compiledShadowEdges[i].startPoint)) > MARGIN_OF_ERROR)
                                    {
                                        lightAreaDivision.Add(new Edge(lightAreaDivision[lightAreaDivision.Count - 1].endPoint, s.compiledShadowEdges[i].startPoint));
                                        break;
                                    }
                                    else
                                    {
                                        Debug.Log("Edge with length ~0 detected");
                                    }
                                }
                            }
                        }
                    }
                    else //Counting Up
                    {
                        if (lightAreaDivision.Count != 1)
                        {
                            for (int i = 0; i < edges.Count; i++)
                            {
                                if (lightAreaDivision[lightAreaDivision.Count - 1].EdgeIntersection(edges[i]))
                                {
                                    mustIntersect = false;

                                    //Check that the intersection point has actually been found
                                    Vector2 lineIntersection = edges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]);

                                    //Select closest intersection point
                                    Vector2 closestIntersection = new Vector2(int.MaxValue, int.MaxValue);
                                    bool properIntersection = false;
                                    for (int c = 0; c < intersectPoints.Count; c++)
                                    {
                                        if (Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].startPoint, intersectPoints[c]) > MARGIN_OF_ERROR)
                                        {
                                            if (Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].startPoint, intersectPoints[c]) <= Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].startPoint, closestIntersection))
                                            {
                                                Vector2 slopeA = (lightAreaDivision[lightAreaDivision.Count - 1].startPoint - lightAreaDivision[lightAreaDivision.Count - 1].endPoint).normalized;
                                                Vector2 slopeB = (lightAreaDivision[lightAreaDivision.Count - 1].startPoint - intersectPoints[c]).normalized;
                                                if (Vector2.Distance(slopeA, slopeB) <= MARGIN_OF_ERROR * 2.0f)
                                                {
                                                    closestIntersection = intersectPoints[c];
                                                    properIntersection = true;
                                                }
                                            }
                                        }
                                    }
                                    if (!properIntersection) { continue; }

                                    lightAreaDivision[lightAreaDivision.Count - 1].endPoint = closestIntersection;

                                    if (Mathf.Abs(Vector2.Distance(lightAreaDivision[lightAreaDivision.Count - 1].endPoint, lightAreaDivision[0].startPoint)) <= MARGIN_OF_ERROR) { completed = true; proceed = false; break; }

                                    int index = 1;
                                    for (int x = 0; x < intersectPoints.Count; x++)
                                    {
                                        if (Mathf.Abs(Vector2.Distance(intersectPoints[x], closestIntersection)) <= MARGIN_OF_ERROR)
                                        {
                                            index = x;
                                        }
                                    }

                                    lightPointsUp = new List<Edge>();
                                    lightPointsDown = new List<Edge>();

                                    //Find direction on light to go
                                    if (intersectPoints.Count > 0)
                                    {
                                        lightPointsUp.Add(new Edge(edges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]), intersectPointsPlacement[index].endPoint)); //End point in second slot counts up
                                        lightPointsDown.Add(new Edge(edges[i].EdgeIntersectionPoint(lightAreaDivision[lightAreaDivision.Count - 1]), intersectPointsPlacement[index].startPoint, true)); //Start point in second slot counts down
                                    }

                                    centerToDownPoint = new Edge(center, lightPointsDown[0].endPoint);
                                    centerToUpPoint = new Edge(center, lightPointsUp[0].endPoint);

                                    testUpPoint = closestIntersection + ((centerToUpPoint.endPoint - closestIntersection) * 0.05f);
                                    testDownPoint = closestIntersection + ((centerToDownPoint.endPoint - closestIntersection) * 0.05f);

                                    //Check if testDownPoint is within the triangle
                                    if (Shape.IsPointInTriangleArray(testDownPoint, s.compiledShadowPoints))
                                    {
                                        lightAreaDivision.Add(lightPointsUp[0]);
                                        countUp = true;
                                    }

                                    //Check if testUpPoint is within the triangle
                                    if (Shape.IsPointInTriangleArray(testUpPoint, s.compiledShadowPoints))
                                    {
                                        lightAreaDivision.Add(lightPointsDown[0]);
                                        countUp = false;
                                    }

                                    lineType = !lineType;
                                    break;
                                }
                            }
                        }
                        if (proceed)
                        {
                            //Counting up
                            for (int i = 0; i < s.compiledShadowEdges.Count; i++)
                            {
                                if (s.compiledShadowEdges[i].startPoint == lightAreaDivision[lightAreaDivision.Count - 1].endPoint)
                                {
                                    if (Mathf.Abs(Vector2.Distance(s.compiledShadowEdges[i].endPoint, s.compiledShadowEdges[i].startPoint)) > MARGIN_OF_ERROR)
                                    {
                                        lightAreaDivision.Add(new Edge(lightAreaDivision[lightAreaDivision.Count - 1].endPoint, s.compiledShadowEdges[i].endPoint));
                                        break;
                                    }
                                    else
                                    {
                                        Debug.Log("Edge with length ~0 detected");
                                    }
                                }
                            }
                        }
                    }
                }
                //Prevent overflow error
                loopLimiter++;
                if(loopLimiter > 100) 
                { 
                    completed = true;
                }
            }
            #endregion

            //Clean up lightAreaDivision list
            List<Edge> cleanLightAreaDivision = new List<Edge>();
            //If an edge has a very short length, delete it
            for (int i = 0; i < lightAreaDivision.Count; i++)
            {
                if (Vector2.Distance(lightAreaDivision[i].startPoint, lightAreaDivision[i].endPoint) < 0.01f)
                {
                    if (cleanLightAreaDivision.Count != 0)
                    {
                        cleanLightAreaDivision[cleanLightAreaDivision.Count - 1].endPoint = lightAreaDivision[i].endPoint;
                    }
                }
                else
                {
                    cleanLightAreaDivision.Add(lightAreaDivision[i]);
                }
            }

            //Finished, now prepare the edges to be drawn, or for the next shape to use the updated edge list
            edges = new List<Edge>();
            foreach (Edge e in cleanLightAreaDivision)
            {
                edges.Add(new Edge(e.startPoint, e.endPoint));
            }
        }

        //Setup the list of points to be drawn
        drawnLight = new List<Vector2>();
        foreach (Edge e in edges)
        {
            drawnLight.Add(e.startPoint);
        }

        //Reset the edge list
        CompileEdgeList();

        DrawLight();
    }

    public int GetLoopedListDifference(int start, int end, int max, bool countUp)
    {
        if(countUp)
        {
            if(end < start)
            {
                return (max - start) + end;
            }
            else
            {
                return end - start;
            }
        }
        else
        {
            if (end > start)
            {
                return start + (max - end);
            }
            else
            {
                return start - end;
            }
        }
    }

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

    public void DrawLight()
    {
        Vector2[] vertices2D = new Vector2[drawnLight.Count];

        for (int c = 0; c < drawnLight.Count; c++)
        {
            vertices2D[c] = drawnLight[c];
        }

        Vector3[] vertices3D = System.Array.ConvertAll<Vector2, Vector3>(vertices2D, v => v);
        for(int i = 0; i < vertices3D.Length; i++)
        {
            vertices3D[i] -= new Vector3(boundingSphereCenter.x, boundingSphereCenter.y, 0.0f);
        }
        vertices3D = Rotate(vertices3D, boundingSphereCenter, -transform.rotation.eulerAngles.z);

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

        if(drawShadows)
        {
            DrawShadow();
        }
        else
        {
            RemoveShadowDraw();
        }
    }

    public void DrawShadow()
    {
        foreach (Shape s in shapes)
        {
            s.DrawShadows();
        }
    }

    public void RemoveShadowDraw()
    {
        foreach (Shape s in shapes)
        {
            s.compiledShadowPoints = new List<Vector2>();
            s.DrawShadows();
        }
    }

    /// <summary>
    /// Updates the list of shapes when the light changes
    /// Called initially at start up
    /// Called when the light moves
    /// Called when a shape in the list moves
    /// </summary>
    public void OnChange(List<Shape> inShapes)
    {
        shapes = new List<Shape>();

        foreach (Shape shape in inShapes)
        {
            if (Vector2.Distance(shape.boundingSphereCenter, boundingSphereCenter) <= shape.boundingSphereRadius + boundingSphereRadius)
            {
                //More expensive check to see if the shape is actually within the light area
                //Check if any of the shape points are within the light area
                if(Shape.IsPointInTriangleArray(shape.Points, points))
                {
                    shapes.Add(shape);
                }
            }
        }
    }

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

    public void Spherize()
    {
        GenerateBoundingSphere();

        float rotNum = (2 * Mathf.PI) / points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            points[i] = boundingSphereCenter + new Vector2(Mathf.Cos(i * rotNum) * boundingSphereRadius, Mathf.Sin(i * rotNum) * boundingSphereRadius);
        }

        GenerateBoundingSphere();
    }

    public void FocusPointsAboutCenter()
    {
        GenerateBoundingSphere();

        Vector2 vectorChange = new Vector2(transform.position.x, transform.position.y) - boundingSphereCenter;
        for (int i = 0; i < points.Count; i++)
        {
            points[i] += vectorChange;
        }

        location = transform.position;
        lastLocation = transform.position;

        GenerateBoundingSphere();
    }

    public void Translate(Vector2 translation)
    {
        if(moveAreaOnTransform)
        {
            transform.position += new Vector3(translation.x, translation.y, 0.0f);
            boundingSphereCenter += translation;
        }

        if(moveCenterOnTransform)
        {
            centerPrevious = center;
            center += translation;
        }
    }

    public void Rotate(Vector2 a, float c) //Rotate points about 'a' point by 'c' degrees
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
        lastRotation = rotation;
        rotation = transform.rotation.eulerAngles.z;
    }


    public List<Vector2> Rotate(List<Vector2> vectors, Vector2 a, float c) //Rotate points about 'a' point by 'c' degrees
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
        return vectors;
    }

    public Vector2[] Rotate(Vector2[] vectors, Vector2 a, float c) //Rotate points about 'a' point by 'c' degrees
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
        return vectors;
    }

    public Vector3[] Rotate(Vector3[] vectors, Vector3 a, float c) //Rotate points about 'a' point by 'c' degrees
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
        return vectors;
    }

    public void Scale(Vector2 a, float c) //Scale points about 'a' point by 'c' length (1.0f = no scale change)
    {
        //Vectors from 'a' to point
        for (int v = 0; v < points.Count; v++)
        {
            Vector2 l = points[v] - a;

            //Set the new points created by the scaled vector
            points[v] = a + (l * c);
        }
        GenerateBoundingSphere();
        lastScale = scale;
        scale *= c;
    }

    #endregion
}
