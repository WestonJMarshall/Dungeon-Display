using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

//[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
abstract public class Shape : MonoBehaviour
{
    public event EventHandler OnCompileEdgeList;

    [SerializeField]
    public List<Vector2> points;
    public List<Edge> edges;

    public List<Vector2> compiledShadowPoints;
    public List<Edge> compiledShadowEdges;

    public bool boundingSphereVisible = true;
    public float boundingSphereRadius;
    public Vector2 boundingSphereCenter;

    public Vector2 location;
    public Vector2 lastLocation;
    public float rotation;
    public float lastRotation;
    public float scale;
    public float lastScale;

    public bool drawGizmos = true;

    public Vector2 closestPointToLight;

    public bool functional = true;

    #region Properties

    /// <summary>
    /// A 2D representation of the center of the object
    /// </summary>
    public Vector2 Location
    {
        get { return location; }
        set { location = value; }
    }

    /// <summary>
    /// A 2D representation of the previous center of the object
    /// </summary>
    public Vector2 LastLocation
    {
        get { return lastLocation; }
        set { lastLocation = value; }
    }

    /// <summary>
    /// A list of the edges that make up this shape
    /// </summary>
    public List<Edge> Edges
    {
        get { return edges; }
    }

    /// <summary>
    /// A list of all of the points that make up the edges of the shape
    /// </summary>
    public List<Vector2> Points
    {
        get { return points; }
    }

    /// <summary>
    /// A float representing the rotation of the shape in degrees
    /// </summary>
    public float Angle
    {
        get { return transform.rotation.eulerAngles.z; }
        set
        {
            transform.rotation = Quaternion.Euler(0, 0, value);
        }
    }

    #endregion

    #region Unity Functions
    protected virtual void Awake()
    {
        Manager.Instance.FindElementsToManage();
        
        if (points == null)
        {
            points = new List<Vector2>();
            CompileEdgeList();
        }
        else
        {
            CompileEdgeList();
            GenerateBoundingSphere();
        }

        location = transform.position;
        lastLocation = location;

        if (points.Count > 0) { Manager.Instance.AssignShapes(); }
    }

    void Update()
    {
        lastLocation = location;
        location = transform.position;

        //Check if this shape has moved
        if (location != lastLocation)
        {
            //Add the movement of the light to its points
            for (int i = 0; i < points.Count; i++)
            {
                points[i] -= lastLocation - location;
            }
            GenerateBoundingSphere();
        }
    }

    /// <summary>
    /// Draw the shape
    /// </summary>
    protected virtual void OnDrawGizmos()
    {
        if (drawGizmos)
        {
            if (points == null)
            {
                points = new List<Vector2>();
            }

            if (edges == null)
            {
                CompileEdgeList();
            }
    
            for (int i = 0; i < edges.Count; i++)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(edges[i].startPoint, edges[i].endPoint);
    
               // Gizmos.color = Color.yellow;
               // Gizmos.DrawLine((edges[i].startPoint + edges[i].endPoint) / 2.0f, ((edges[i].startPoint + edges[i].endPoint) / 2.0f) + edges[i].normal);
    
                //Gizmos.color = Color.cyan;
                //Gizmos.DrawSphere(edges[i].startPoint, 0.15f);
            }
    
            //if (boundingSphereVisible)
            //{
            //    Gizmos.color = Color.blue;
            //    //Draw the bounding sphere
            //    List<Vector2> spherePoints = new List<Vector2>();
            //    float rotNum = (2 * Mathf.PI) / 32;
            //    for (int i = 0; i < 32; i++)
            //    {
            //        spherePoints.Add(boundingSphereCenter + new Vector2(Mathf.Cos(i * rotNum) * boundingSphereRadius, Mathf.Sin(i * rotNum) * boundingSphereRadius));
            //    }
            //    for (int i = 0; i < spherePoints.Count; i++)
            //    {
            //        if (i == spherePoints.Count - 1)
            //        {
            //            Gizmos.DrawLine(spherePoints[i], spherePoints[0]);
            //        }
            //        else
            //        {
            //            Gizmos.DrawLine(spherePoints[i], spherePoints[i + 1]);
            //        }
            //    }
            //    Gizmos.DrawSphere(boundingSphereCenter, 0.30f);
            //}
        }
    }

    #endregion

    #region Shape

    /// <summary>
    /// Setup the edge list using all of this shapes points
    /// </summary>
    public void CompileEdgeList()
    {
        edges = new List<Edge>();

        if(Points.Count > 0)
        {
            for (int i = 0; i <= points.Count - 1; i++)
            {
                if (i <= points.Count - 2)
                {
                    Edge e = new Edge(points[i], points[i + 1]);
                    e.normal = new Vector2((points[i + 1] - points[i]).normalized.y, -(points[i + 1] - points[i]).normalized.x);
                    edges.Add(e);
                }
                else
                {
                    Edge e = new Edge(points[i], points[0]);
                    e.normal = new Vector2((points[0] - points[i]).normalized.y, -(points[0] - points[i]).normalized.x);
                    edges.Add(e);
                }
            }
        }

        //Call an event
        OnCompileEdgeList?.Invoke(this, EventArgs.Empty);
    }

    public virtual void BuildShadow(CustomLight light)
    {
        compiledShadowPoints = new List<Vector2>();
        compiledShadowEdges = new List<Edge>();
    }

    /// <summary>
    /// Draws the shadow shape created by a light for this shape
    /// </summary>
    public void DrawShadows()
    {
            Vector2[] vertices2D = new Vector2[compiledShadowPoints.Count];

            for(int c = 0; c < compiledShadowPoints.Count; c++)
            {
                vertices2D[c] = compiledShadowPoints[c];
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
            gameObject.GetComponent<MeshRenderer>().materials[0].color = Color.black;
    }

    /// <summary>
    /// Takes all of the points in a light shape and finds the two farthest ones
    /// </summary>
    /// <param name="light">light to be evaluated</param>
    /// <returns></returns>
    public int[] FindFarthestPoints(CustomLight light) //Depricated
    {
        //Assign angles to all points
        List<float> pointAngles = new List<float>();
        int lowestAngle = 0;
        int highestAngle = 0;
        float maxDist = -100.0f;

        foreach (Vector2 point in Points)
        {
            float radianDif = Mathf.Atan2(point.y - light.Center.y, point.x - light.Center.x);
            pointAngles.Add(radianDif);
        }

        for (int i = 0; i < pointAngles.Count; i++)
        {
            for (int c = 0; c < pointAngles.Count; c++)
            {
                if (i != c)
                {
                    if (Difference(pointAngles[i], pointAngles[c]) > maxDist)
                    {
                        lowestAngle = i;
                        highestAngle = c;
                        maxDist = Difference(pointAngles[i], pointAngles[c]);
                    }
                }
            }
        }

        int[] furthestIndex = new int[2];
        furthestIndex[0] = lowestAngle;
        furthestIndex[1] = highestAngle;

        return furthestIndex;
    }

    /// <summary>
    /// Find the angle difference in radians between two angles
    /// </summary>
    /// <param name="a">First Angle</param>
    /// <param name="b">Second Angle</param>
    /// <returns></returns>
    private float Difference(float a, float b)
    {
        if (a < 0) { a = (Mathf.PI * 2) + a; }
        if (b < 0) { b = (Mathf.PI * 2) + b; }

        if (a <= Mathf.PI / 2.0f)
        {
            if (b > (Mathf.PI * 3.0f) / 2.0f)
            {
                b = (Mathf.PI * 2.0f) - b;
                return a + b;
            }
        }
        if (a > (Mathf.PI * 3.0f) / 2.0f)
        {
            if (b <= Mathf.PI / 2.0f)
            {
                a = (Mathf.PI * 2.0f) - a; 
                return a + b;
            }
        }

        float temp = Mathf.Abs(Mathf.Abs(a) - Mathf.Abs(b));

        if(temp > Mathf.PI) { temp -= Mathf.PI; }
        return temp;
    }

    /// <summary>
    /// Returns whether or not a point is contained within the shape
    /// </summary>
    /// <param name="point">The point to check</param>
    /// <returns>Whether or not the point is in the shape</returns>
    public abstract bool ContainsPoint(Vector2 point);

    /// <summary>
    /// Checks if a ray collides with the shape and returns the edge that was collided with
    /// </summary>
    /// <param name="ray">The edge that the ray collides with, null if it doesn't collide with the shape</param>
    /// <returns>The edge that the ray collided with or null if it doesn't collide</returns>
    public virtual Edge Raycast(Ray ray)
    {
        Vector2 relativeOrigin = (Vector2)ray.origin - location;
        Dictionary<Vector2, float> projectionMults = new Dictionary<Vector2, float>(new Vector2Comparer());

        //Project the shape saving vertex results and check for overlap
        Vector2 projLine = new Vector2(-1 * ray.direction.y, ray.direction.x).normalized;
        bool hasNegative = false;
        bool hasPositive = false;
        foreach(Vector2 vertex in points)
        {
            Vector2 distance = vertex - (Vector2)ray.origin;
            float mult = Vector2.Dot(projLine, distance);

            if(mult < 0)
            {
                hasNegative = true;
            }

            if(mult > 0)
            {
                hasPositive = true;
            }

            projectionMults.Add(vertex, mult);
        }

        //If we don't have at least one positive and one negative the ray does not intersect at all
        if(!hasNegative || !hasPositive)
        {
            return null;
        }

        List<Edge> intersections = new List<Edge>();

        //Check for overlapping edges
        foreach(Edge edge in edges)
        {
            //Edges overlap if they have one positive and one negative mult
            float product = projectionMults[edge.startPoint] * projectionMults[edge.endPoint];

            /*If the edge has one of each the product of the two multipliers will be negative
             *  positive * positive = positive
             *  positive * negative = negative
             *  negative * negative = positive
            */
            if (product < 0)
            {
                intersections.Add(edge);
            }
        }

        //Find which edge was hit first
        if(intersections.Count == 1)
        {
            Debug.DrawLine(intersections[0].startPoint, intersections[0].endPoint, Color.red, 5);
            return intersections[0];
        }

        Edge closest = intersections[0];
        Vector2 edgeDirection = intersections[0].endPoint - intersections[0].startPoint;
        //proj of rayOrigin onto edge
        Vector2 rayProjection = intersections[0].startPoint + (Vector2.Dot(edgeDirection, relativeOrigin - intersections[0].startPoint) / Vector2.Dot(edgeDirection, edgeDirection)) * edgeDirection;
        //direction from ray to ray projection
        Vector2 projDistance = relativeOrigin - rayProjection;
        //projection of ray direction onto ray projection
        float closestDistance = Mathf.Abs(Vector2.Dot(projDistance, ray.direction) / Vector2.Dot(projDistance, projDistance));

        for(int i = 1; i < intersections.Count; i++)
        {
            edgeDirection = intersections[i].endPoint - intersections[i].startPoint;
            //proj of rayOrigin onto edge
            rayProjection = intersections[i].startPoint + (Vector2.Dot(edgeDirection, relativeOrigin - intersections[i].startPoint) / Vector2.Dot(edgeDirection, edgeDirection)) * edgeDirection;
            //direction from ray to ray projection
            projDistance = relativeOrigin - rayProjection;
            //projection of ray direction onto ray projection
            float sqrDistance = Mathf.Abs(Vector2.Dot(projDistance, ray.direction) / Vector2.Dot(projDistance, projDistance));

            if (sqrDistance < closestDistance)
            {
                closestDistance = sqrDistance;
                closest = edges[i];
            }
        }

        Debug.DrawLine(ray.origin, ray.origin + ray.direction * closestDistance, Color.green, 5);
        Debug.DrawLine(closest.startPoint, closest.endPoint, Color.red);

        return closest;
    }

    /// <summary>
    /// Finds the shapes edge that is nearest to the point
    /// </summary>
    /// <param name="point">The point to check distance from</param>
    /// <returns>The edge that is closest to the given point</returns>
    public virtual Edge NearestEdge(Vector2 point)
    {
        //Get point relative to center
        point -= location;

        //Find the closest point on the shape to the point
        Vector2 closestPoint = points[0];
        
        Vector2 distance = points[0] - point;
        float closestDistance = distance.x * distance.x + distance.y * distance.y;

        for(int i = 1; i < points.Count; i++)
        {
            distance = points[i] - point;
            float currentDistance = distance.x * distance.x + distance.y * distance.y;

            if(currentDistance < closestDistance)
            {
                closestDistance = currentDistance;
                closestPoint = points[i];
            }
        }

        Edge edge1 = edges[0];
        Edge edge2 = edges[0];

        //Find the edges that contain that point
        for(int i = 0; i < edges.Count; i++)
        {
            if(edges[i].startPoint == closestPoint)
            {
                edge1 = edges[i];

                if(i == 0)
                {
                    edge2 = edges[edges.Count - 1];
                }
                else
                {
                    edge2 = edges[i - 1];
                }
            }
        }

        //Find which edge is closer
        float edge1Distance = edge1.endPoint.x * edge1.endPoint.x + edge1.endPoint.y * edge1.endPoint.y;
        float edge2Distance = edge2.endPoint.x * edge2.endPoint.x + edge2.endPoint.y * edge2.endPoint.y;

        if(edge1Distance < edge2Distance)
        {
            return edge1;
        }

        return edge2;
    }

    /// <summary>
    /// Finds the min and max angle of the shape around a circle
    /// </summary>
    /// <param name="center">The center of the circle to project onto</param>
    /// <param name="radius">The maximum distance from the center to count in the projection</param>
    /// <returns>A Vector2 representing the min and max angle around the circle contained by the shape (Min, Max)</returns>
    public virtual Vector2 CircularProject(Vector2 center, float radius)
    {
        //Get the center relative to the shapes position
        center -= location;

        Vector2 minMax = new Vector2(-1, -1);

        for(int i = 0; i < Points.Count; i++)
        {
            //Make sure the point is in the radius of the circle
            Vector2 direction = Points[i] - center;
            float squareDistance = direction.x * direction.x + direction.y * direction.y;

            if(squareDistance <= radius * radius)
            {
                //Project onto the shape
                float currentAngle = GetProjectionAngle(center, Points[i]);

                //Update min and max angles
                if(currentAngle < minMax.x)
                {
                    minMax.x = currentAngle;
                }
                else if(currentAngle > minMax.y)
                {
                    minMax.y = currentAngle;
                }
                else if(minMax.x == -1) //If this is the first point in the radius set both min and max
                {
                    minMax.x = currentAngle;
                    minMax.y = currentAngle;
                }
            }
        }

        //TODO: If the shape intersects with the circle find the points of intersection and add those angles as possible min and max

        return minMax;
    }

    /// <summary>
    /// Creates an edge along the shape representing it's projection onto a line perpendicular to the given ray
    /// </summary>
    /// <param name="ray">The direction perpendicular to the line we are projecting onto</param>
    /// <returns>An edge representing the projection of the shape onto the line</returns>
    public virtual Edge Project(Ray ray)
    {
        //Find the line to project onto perpendicular to the given direction
        Vector2 axis = new Vector2(ray.direction.y, -ray.direction.x);

        //Find the min and max points to the projection
        float currentMult = GetProjectionMult(axis, points[0]);

        Vector2 minMax = new Vector2(currentMult, currentMult);

        for (int i = 1; i < points.Count; i++)
        {
            currentMult = GetProjectionMult(axis, points[i]);

            if(currentMult < minMax.x)
            {
                minMax.x = currentMult;
            }
            else if(currentMult > minMax.y)
            {
                minMax.y = currentMult;
            }
        }

        //Create the new edge
        return new Edge(axis * minMax.x, axis * minMax.y);
    }

    /// <summary>
    /// Finds the axis multiplier to reach the projection of the given point onto the given axis
    /// </summary>
    /// <param name="axis">The axis to project onto</param>
    /// <param name="Point">The point to project</param>
    /// <returns></returns>
    static float GetProjectionMult(Vector2 axis, Vector2 Point)
    {
        return Vector2.Dot(axis, Point) / Vector2.Dot(axis, axis);
    }

    /// <summary>
    /// Returns the angle of the given point around the center point
    /// </summary>
    /// <param name="center">The center of the circle to project onto</param>
    /// <param name="point">The point to project</param>
    /// <returns>The angle of the point around the center</returns>
    static float GetProjectionAngle(Vector2 center, Vector2 point)
    {
        //Get point relative to the center
        point -= center;

        //Get the angle of the point
        return Mathf.Atan2(point.y, point.x);
    }

    public void GenerateBoundingSphere()
    {
        //Find the center 
        if (points.Count > 0)
        {
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
        else
        {
            boundingSphereCenter = Vector2.zero;
            boundingSphereRadius = 0;
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

        if(Manager.Instance != null)
        {
            Manager.Instance.BuildAllLights();
        }
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

        if (Manager.Instance != null)
        {
            Manager.Instance.BuildAllLights();
        }
    }

    public void Translate(Vector2 translation)
    {
        transform.position += new Vector3(translation.x, translation.y, 0.0f);
        boundingSphereCenter += translation;

        if (Manager.Instance != null)
        {
            Manager.Instance.BuildAllLights();
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

        if (Manager.Instance != null)
        {
            Manager.Instance.BuildAllLights();
        }
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

        if (Manager.Instance != null)
        {
            Manager.Instance.BuildAllLights();
        }
    }

    public static bool IsPointInTriangleArray(Vector2 point, List<Vector2> trianglePoints)
    {
        //Check which point is outside of the test triangle
        Vector2[] vertices2D = new Vector2[trianglePoints.Count];

        for (int c = 0; c < trianglePoints.Count; c++)
        {
            vertices2D[c] = trianglePoints[c];
        }

        // Use the triangulator to get indices for creating triangles
        Triangulator triangulator = new Triangulator(vertices2D);
        int[] indices = triangulator.Triangulate();

        //Check each triangle of the shadow
        for (int x = 0; x < indices.Length - 2; x += 3)
        {
            //Create a triangle
            Vector2 tPointA = vertices2D[indices[x]];
            Vector2 tPointB = vertices2D[indices[x + 1]];
            Vector2 tPointC = vertices2D[indices[x + 2]];

            //Check if testDownPoint is within the triangle
            if (Triangulator.PointInTriangle(point, tPointB, tPointC, tPointA))
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsPointInTriangleArrayPreCalcVerts(Vector2 point, Vector2[] vertices2D, int[] indices)
    {
        //Check each triangle of the shadow
        for (int x = 0; x < indices.Length - 2; x += 3)
        {
            //Create a triangle
            Vector2 tPointA = vertices2D[indices[x]];
            Vector2 tPointB = vertices2D[indices[x + 1]];
            Vector2 tPointC = vertices2D[indices[x + 2]];

            //Check if testDownPoint is within the triangle
            if (Triangulator.PointInTriangle(point, tPointB, tPointC, tPointA))
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsPointInTriangleArray(List<Vector2> points, List<Vector2> trianglePoints)
    {
        //Check which point is outside of the test triangle
        Vector2[] vertices2D = new Vector2[trianglePoints.Count];

        for (int c = 0; c < trianglePoints.Count; c++)
        {
            vertices2D[c] = trianglePoints[c];
        }

        // Use the triangulator to get indices for creating triangles
        Triangulator triangulator = new Triangulator(vertices2D);
        int[] indices = triangulator.Triangulate();

        //Check each triangle of the shadow
        for (int x = 0; x < indices.Length - 2; x += 3)
        {
            //Create a triangle
            Vector2 tPointA = vertices2D[indices[x]];
            Vector2 tPointB = vertices2D[indices[x + 1]];
            Vector2 tPointC = vertices2D[indices[x + 2]];

            foreach(Vector2 point in points)
            {
                //Check if testDownPoint is within the triangle
                if (Triangulator.PointInTriangle(point, tPointB, tPointC, tPointA))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool TriangleAreaTest(Vector2 a, Vector2 b, Vector2 c)
    {
        if(Vector2.Distance(a,b) < 0.0001f || Vector2.Distance(a, c) < 0.0001f || Vector2.Distance(c, b) < 0.0001f)
        {
            return false;
        }
        Vector2 sa = (a - b).normalized;
        Vector2 sb = (b - c).normalized;
        Vector2 sc = (c - a).normalized;
        if (Vector2.Distance(sa, sb) < 0.00005f
            || Vector2.Distance(sc, sb) < 0.00005f
            || Vector2.Distance(sa, sc) < 0.00005f
            || Vector2.Distance(-sa, sb) < 0.00005f
            || Vector2.Distance(-sb, sc) < 0.00005f
            || Vector2.Distance(-sc, sa) < 0.00005f)
        {
            return false;
        }

        return true;
    }

    public static bool IsPointInTriangleArray(object data)
    {
        Vector2 point = ((Tuple<Vector2, List<Vector2>>)data).Item1;
        List<Vector2> trianglePoints = ((Tuple<Vector2, List<Vector2>>)data).Item2;

        //Check which point is outside of the test triangle
        Vector2[] vertices2D = new Vector2[trianglePoints.Count];

        for (int c = 0; c < trianglePoints.Count; c++)
        {
            vertices2D[c] = trianglePoints[c];
        }

        // Use the triangulator to get indices for creating triangles
        Triangulator triangulator = new Triangulator(vertices2D);
        int[] indices = triangulator.Triangulate();

        //Check each triangle of the shadow
        for (int x = 0; x < indices.Length - 2; x += 3)
        {
            //Create a triangle
            Vector2 tPointA = vertices2D[indices[x]];
            Vector2 tPointB = vertices2D[indices[x + 1]];
            Vector2 tPointC = vertices2D[indices[x + 2]];

            //Check if testDownPoint is within the triangle
            if (Triangulator.PointInTriangle(point, tPointB, tPointC, tPointA))
            {
                return true;
            }
        }
        return false;
    }

    #endregion
}
