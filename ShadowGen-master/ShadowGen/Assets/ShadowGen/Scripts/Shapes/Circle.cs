using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Circle : Shape
{
    [SerializeField]
    protected float radius = 1;
    [SerializeField]
    protected int resolution = 15;

    #region Properties

    /// <summary>
    /// The radius of the circle
    /// </summary>
    public float Radius
    {
        get { return radius; }
        set { radius = value; }
    }

    /// <summary>
    /// The number of points that make up the circle
    /// </summary>
    public int Resolution
    {
        get { return resolution; }
        set { resolution = value; }
    }

    #endregion

    #region Circle

    public void Setup(Vector2 position, float radius, int resolution)
    {
        Location = position;
        this.radius = radius;
        this.resolution = resolution;
        
        Setup();
    }

    public void Setup()
    {
        //Setup bounding sphere
        boundingSphereCenter = Location;
        boundingSphereRadius = radius;

        //Reset lists or create them if they dont already exist
        edges = new List<Edge>();
        points = new List<Vector2>();

        //Find points
        float step = (Mathf.PI * 2) / resolution;

        for (int i = 0; i < resolution; i++)
        {
            float angle = step * i;
            Points.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }

        //Setup edges
        for (int i = 0; i < Points.Count - 1; i++)
        {
            Edges.Add(new Edge(Points[i], Points[i + 1]));
        }
        Edges.Add(new Edge(Points[Points.Count - 1], Points[0])); //Setup last edge

        //Setup normals
        for (int i = 0; i < Edges.Count; i++)
        {
            Edges[i].normal = (((Edges[i].startPoint + Edges[i].endPoint) * 0.5f) - Location).normalized;
        }
    }

    public override bool ContainsPoint(Vector2 point)
    {
        float radiusSqrd = radius * radius;
        Vector2 direction = point - Location;
        float distanceSqrd = direction.x * direction.x + direction.y + direction.y;

        return (distanceSqrd < radiusSqrd);
    }

    #endregion
}
