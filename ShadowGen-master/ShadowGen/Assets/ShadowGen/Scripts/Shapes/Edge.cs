using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edge
{
    public Vector2 startPoint;
    public Vector2 endPoint;
    public Vector2 normal;

    const float MARGIN_OF_ERROR = 0.001f;

    #region Constructors
    public Edge(Vector2 _startPoint, Vector2 _endPoint)
    {
        startPoint = _startPoint;
        endPoint = _endPoint;
        Vector2 slope = startPoint - endPoint;
        normal = new Vector2(slope.y, -slope.x);
    }

    public Edge(Vector2 _startPoint, Vector2 _endPoint, Vector2 _normal)
    {
        startPoint = _startPoint;
        endPoint = _endPoint;
        normal = _normal;
    }

    public Edge(Vector2 _startPoint, Vector2 _endPoint, bool reverse)
    {
        startPoint = _startPoint;
        endPoint = _endPoint;
        if(reverse)
        {
            Vector2 slope = startPoint - endPoint;
            normal = new Vector2(-slope.y, slope.x);
        }
        else
        {
            Vector2 slope = startPoint - endPoint;
            normal = new Vector2(slope.y, -slope.x);
        }
    }
    #endregion

    #region Methods
    static public List<Edge> SplitEdge(Edge edge)
    {
        if(edge.startPoint != null)
        {
            List<Edge> edges = new List<Edge>();
            edges.Add(new Edge(edge.startPoint, edge.startPoint + ((edge.endPoint - edge.startPoint) / 2.0f)));
            edges.Add(new Edge(edge.startPoint + ((edge.endPoint - edge.startPoint) / 2.0f), edge.endPoint));

            return edges;
        }
        else
        {
            return new List<Edge>();
        }
    }

    static public List<Edge> SplitEdgeReversed(Edge edge)
    {
        if (edge.startPoint != null)
        {
            List<Edge> edges = new List<Edge>();
            edges.Add(new Edge(edge.startPoint + ((edge.endPoint - edge.startPoint) / 2.0f), edge.endPoint));
            edges.Add(new Edge(edge.startPoint, edge.startPoint + ((edge.endPoint - edge.startPoint) / 2.0f)));

            return edges;
        }
        else
        {
            return new List<Edge>();
        }
    }

    static public List<Edge> SplitEdgeList(List<Edge> edges)
    {
        List<Edge> newEdgeList = new List<Edge>();

        for(int i = 0; i < edges.Count; i++)
        {
            newEdgeList.AddRange(SplitEdge(edges[i]));
        }
        return newEdgeList;
    }

    static public List<Edge> SplitEdgeListReversed(List<Edge> edges)
    {
        List<Edge> newEdgeList = new List<Edge>();

        for (int i = 0; i < edges.Count; i++)
        {
            newEdgeList.AddRange(SplitEdgeReversed(edges[i]));
        }
        return newEdgeList;
    }

    /// <summary>
    /// Return the intersection point if this edge and the input edge intersect
    /// </summary>
    /// <param name="edge">edge to test intersection against</param>
    /// <returns></returns>
    public Vector2 EdgeIntersectionPoint(Edge edge)
    {
        float m1, c1, m2, c2;
        float x1, y1, x2, y2;
        float x3, y3, x4, y4;
        float dx, dy;
        float intersectionX, intersectionY;

        x1 = startPoint.x;
        y1 = startPoint.y;
        x2 = endPoint.x;
        y2 = endPoint.y;

        dx = x2 - x1;
        dy = y2 - y1;

        bool v1 = false, v2 = false, h1 = false, h2 = false;

        if (Mathf.Abs(dx) < 0.001f) { v1 = true; dx = 0.0001f; }
        if (Mathf.Abs(dy) < 0.001f) { h1 = true; }

        m1 = dy / dx;

        c1 = y1 - m1 * x1;

        x3 = edge.startPoint.x;
        y3 = edge.startPoint.y;
        x4 = edge.endPoint.x;
        y4 = edge.endPoint.y;

        dx = x4 - x3;
        dy = y4 - y3;

        if (Mathf.Abs(dx) < 0.001f) { v2 = true; dx = 0.0001f; }
        if (Mathf.Abs(dy) < 0.001f) { h2 = true; }

        m2 = dy / dx;

        c2 = y3 - m2 * x3;

        intersectionX = (c2 - c1) / (m1 - m2);
        intersectionY = m1 * intersectionX + c1;

        //Special cases for horizontal & vertical lines
        if (h1) { intersectionY = y1; }
        else if (h2) { intersectionY = y4; }
        if (v1) { intersectionX = x1; }
        else if (v2) { intersectionX = x4; }

        Vector2 intersection = new Vector2(intersectionX, intersectionY);

        return intersection;
    }

    /// <summary>
    /// Return true if this edge and the input edge intersect
    /// </summary>
    /// <param name="edge">edge to test intersection against</param>
    /// <returns></returns>
    public bool EdgeIntersection(Edge edge)
    {
        float m1, c1, m2, c2;
        float x1, y1, x2, y2;
        float x3, y3, x4, y4;
        float dx, dy;
        float intersectionX, intersectionY;

        x1 = startPoint.x;
        y1 = startPoint.y;
        x2 = endPoint.x;
        y2 = endPoint.y;

        dx = x2 - x1;
        dy = y2 - y1;

        bool v1 = false, v2 = false, h1 = false, h2 = false;

        if (Mathf.Abs(dx) < 0.001f) { v1 = true; dx = 0.0001f; }
        if (Mathf.Abs(dy) < 0.001f) { h1 = true; }

        m1 = dy / dx;

        c1 = y1 - m1 * x1;

        x3 = edge.startPoint.x;
        y3 = edge.startPoint.y;
        x4 = edge.endPoint.x;
        y4 = edge.endPoint.y;

        dx = x4 - x3;
        dy = y4 - y3;

        if (Mathf.Abs(dx) < 0.001f) { v2 = true; dx = 0.0001f; }
        if (Mathf.Abs(dy) < 0.001f) { h2 = true; }

        m2 = dy / dx;

        c2 = y3 - m2 * x3;

        intersectionX = (c2 - c1) / (m1 - m2);
        intersectionY = m1 * intersectionX + c1;

        //Special cases for horizontal & vertical lines
        if (h1) { intersectionY = y1; }
        else if (h2) { intersectionY = y4; }
        if (v1) { intersectionX = x1; }
        else if (v2) { intersectionX = x4; }

        Vector2 intersection = new Vector2(intersectionX, intersectionY);

        //If that point is within the length of the two lines on the triangle, they are colliding
        if(LinePointTest(x1, y1, x2, y2, intersectionX, intersectionY) && LinePointTest(x3, y3, x4, y4, intersectionX, intersectionY))
        {
            return true;
        }
        return false;
    }

    public bool LinePointTest(float x1, float y1, float x2, float y2, float px, float py)
    {
        float left, top, right, bottom; // Bounding Box For Line Segment

        // For Bounding Box
        if (x1 < x2)
        {
            left = x1;
            right = x2;
        }
        else
        {
            left = x2;
            right = x1;
        }

        if (y1 > y2)
        {
            top = y1;
            bottom = y2;
        }
        else
        {
            top = y2;
            bottom = y1;
        }

        if ((px + MARGIN_OF_ERROR) >= left && (px - MARGIN_OF_ERROR) <= right &&
                (py - MARGIN_OF_ERROR) <= top && (py + MARGIN_OF_ERROR) >= bottom)
        {
            return true;
        }
        else
        {
            return false;
        }

    }
    #endregion
}
