using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rectangle : Shape
{
    [SerializeField]
    protected Vector2 extents = Vector2.one;

    #region Properties

    /// <summary>
    /// The half width and half height of the rectangle
    /// </summary>
    public Vector2 Extents
    {
        get { return extents; }
        set { extents = value; }
    }

    #endregion

    #region Rectangle

    public void Setup(Vector2 position, Vector2 size, float angle)
    {
        Location = position;
        extents = size * 0.5f;
        Angle = angle;

        Setup();
    }

    public void Setup()
    {
        //Setup bounding sphere
        boundingSphereCenter = Location;
        boundingSphereRadius = Mathf.Sqrt(extents.x * extents.x + extents.y + extents.y);

        points = new List<Vector2>();
        edges = new List<Edge>();

        //Setup the rectangle
        //Set points
        Points.Add(new Vector2(-Extents.x, Extents.y));
        Points.Add(new Vector2(Extents.x, Extents.y));
        Points.Add(new Vector2(Extents.x, -Extents.y));
        points.Add(new Vector2(-Extents.x, -Extents.y));

        //Set Edges
        Edges.Add(new Edge(points[0], points[1]));
        Edges.Add(new Edge(points[1], points[2]));
        Edges.Add(new Edge(points[2], points[3]));
        Edges.Add(new Edge(points[3], points[0]));

        //Set Normals
        for (int i = 0; i < Edges.Count; i++)
        {
            Edges[i].normal = (((Edges[i].startPoint + Edges[i].endPoint) * 0.5f) - Location).normalized;
        }
    }

    public override bool ContainsPoint(Vector2 point)
    {
        //TODO: apply angles to calculate bounds better

        return (point.x > Location.x - extents.x && point.y < Location.x + extents.x && point.y > Location.y - extents.y && point.y < Location.y + extents.y);
    }

    #endregion
}
