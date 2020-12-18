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
        double m1, c1, m2, c2;
        double x1, y1, x2, y2;
        double x3, y3, x4, y4;
        double dx, dy;
        double intersectionX, intersectionY;

        x1 = startPoint.x;
        y1 = startPoint.y;
        x2 = endPoint.x;
        y2 = endPoint.y;

        dx = x2 - x1;
        dy = y2 - y1;

        bool v1 = false, v2 = false, h1 = false, h2 = false;

        if (Mathf.Abs((float)dx) < 0.001f) { v1 = true; dx = 0.0000000001f; }
        if (Mathf.Abs((float)dy) < 0.001f) { h1 = true; }

        m1 = dy / dx;

        c1 = y1 - m1 * x1;

        x3 = edge.startPoint.x;
        y3 = edge.startPoint.y;
        x4 = edge.endPoint.x;
        y4 = edge.endPoint.y;

        dx = x4 - x3;
        dy = y4 - y3;

        if (Mathf.Abs((float)dx) < 0.001f) { v2 = true; dx = 0.0000000001f; }
        if (Mathf.Abs((float)dy) < 0.001f) { h2 = true; }

        m2 = dy / dx;

        c2 = y3 - m2 * x3;

        intersectionX = (c2 - c1) / (m1 - m2);
        intersectionY = m1 * intersectionX + c1;

        //Special cases for horizontal & vertical lines
        if (h1) { intersectionY = y1; }
        else if (h2) { intersectionY = y4; }
        if (v1) { intersectionX = x1; }
        else if (v2) { intersectionX = x4; }

        Vector2 intersection = new Vector2((float)intersectionX, (float)intersectionY);

        return intersection;
    }

    /// <summary>
    /// Return true if this edge and the input edge intersect
    /// </summary>
    /// <param name="edge">edge to test intersection against</param>
    /// <returns></returns>
    public bool EdgeIntersection(Edge edge)
    {
        double m1, c1, m2, c2;
        double x1, y1, x2, y2;
        double x3, y3, x4, y4;
        double dx, dy;
        double intersectionX, intersectionY;

        x1 = startPoint.x;
        y1 = startPoint.y;
        x2 = endPoint.x;
        y2 = endPoint.y;

        dx = x2 - x1;
        dy = y2 - y1;

        bool v1 = false, v2 = false, h1 = false, h2 = false;

        if (Mathf.Abs((float)dx) < 0.001f) { v1 = true; dx = 0.0000000001f; }
        if (Mathf.Abs((float)dy) < 0.001f) { h1 = true; }

        m1 = dy / dx;

        c1 = y1 - m1 * x1;

        x3 = edge.startPoint.x;
        y3 = edge.startPoint.y;
        x4 = edge.endPoint.x;
        y4 = edge.endPoint.y;

        dx = x4 - x3;
        dy = y4 - y3;

        if (Mathf.Abs((float)dx) < 0.001f) { v2 = true; dx = 0.0000000001f; }
        if (Mathf.Abs((float)dy) < 0.001f) { h2 = true; }

        m2 = dy / dx;

        c2 = y3 - m2 * x3;

        intersectionX = (c2 - c1) / (m1 - m2);
        intersectionY = m1 * intersectionX + c1;

        //Special cases for horizontal & vertical lines
        if (h1) { intersectionY = y1; }
        else if (h2) { intersectionY = y4; }
        if (v1) { intersectionX = x1; }
        else if (v2) { intersectionX = x4; }

        Vector2 intersection = new Vector2((float)intersectionX, (float)intersectionY);

        //If that point is within the length of the two lines on the triangle, they are colliding
        if (LinePointTest((float)x1, (float)y1, (float)x2, (float)y2, (float)intersectionX, (float)intersectionY))
        {
            if(LiberalLinePointTest((float)x3, (float)y3, (float)x4, (float)y4, (float)intersectionX, (float)intersectionY))
            {
                return true;
            }
        }
        if (LinePointTest((float)x3, (float)y3, (float)x4, (float)y4, (float)intersectionX, (float)intersectionY))
        {
            if (LiberalLinePointTest((float)x1, (float)y1, (float)x2, (float)y2, (float)intersectionX, (float)intersectionY))
            {
                return true;
            }
        }
        return false;
    }

    public bool PointIntersection(Vector2 point)
    {
        float x1, y1, x2, y2;

        x1 = startPoint.x;
        y1 = startPoint.y;
        x2 = endPoint.x;
        y2 = endPoint.y;

        //If that point is within the length of the two lines on the triangle, they are colliding
        if (LinePointTest(x1, y1, x2, y2, point.x, point.y))
        {
            return true;
        }
        return false;
    }

    public bool ExactPointIntersection(Vector2 point)
    {
        float x1, y1, x2, y2;

        x1 = startPoint.x;
        y1 = startPoint.y;
        x2 = endPoint.x;
        y2 = endPoint.y;

        //If that point is within the length of the two lines on the triangle, they are colliding
        if (ExactLinePointTest(x1, y1, x2, y2, point.x, point.y))
        {
            return true;
        }
        return false;
    }

    public bool NearExactPointIntersection(Vector2 point)
    {
        float x1, y1, x2, y2;

        x1 = startPoint.x;
        y1 = startPoint.y;
        x2 = endPoint.x;
        y2 = endPoint.y;

        //If that point is within the length of the two lines on the triangle, they are colliding
        if (NearExactLinePointTest(x1, y1, x2, y2, point.x, point.y))
        {
            return true;
        }
        return false;
    }

    public bool PrecisePointIntersection(Vector2 point)
    {
        float x1, y1, x2, y2;

        x1 = startPoint.x;
        y1 = startPoint.y;
        x2 = endPoint.x;
        y2 = endPoint.y;

        //If that point is within the length of the two lines on the triangle, they are colliding
        if (PreciseLinePointTest(x1, y1, x2, y2, point.x, point.y))
        {
            return true;
        }
        return false;
    }

    public bool LiberalPointIntersection(Vector2 point)
    {
        float x1, y1, x2, y2;

        x1 = startPoint.x;
        y1 = startPoint.y;
        x2 = endPoint.x;
        y2 = endPoint.y;

        //If that point is within the length of the two lines on the triangle, they are colliding
        if (LiberalLinePointTest(x1, y1, x2, y2, point.x, point.y))
        {
            return true;
        }
        return false;
    }

    public bool LinePointTest(float x1, float y1, float x2, float y2, float px, float py)
    {
        float AB = Mathf.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        float AP = Mathf.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
        float PB = Mathf.Sqrt((x2 - px) * (x2 - px) + (y2 - py) * (y2 - py));
        if (AB == AP + PB || Mathf.Abs(AB - (AP + PB)) < MARGIN_OF_ERROR * 8.0f)
        {
            return true;
        }
        return false;
    }

    public bool PreciseLinePointTest(float x1, float y1, float x2, float y2, float px, float py)
    {
        float AB = Mathf.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        float AP = Mathf.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
        float PB = Mathf.Sqrt((x2 - px) * (x2 - px) + (y2 - py) * (y2 - py));
        if (AB == AP + PB || Mathf.Abs(AB - (AP + PB)) < MARGIN_OF_ERROR * 1.0f)
        {
            return true;
        }
        return false;
    }

    public bool NearExactLinePointTest(float x1, float y1, float x2, float y2, float px, float py)
    {
        float AB = Mathf.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        float AP = Mathf.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
        float PB = Mathf.Sqrt((x2 - px) * (x2 - px) + (y2 - py) * (y2 - py));
        if (AB == AP + PB || Mathf.Abs(AB - (AP + PB)) < MARGIN_OF_ERROR * 0.35f)
        {
            return true;
        }
        return false;
    }

    public bool ExactLinePointTest(float x1, float y1, float x2, float y2, float px, float py)
    {
        float AB = Mathf.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        float AP = Mathf.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
        float PB = Mathf.Sqrt((x2 - px) * (x2 - px) + (y2 - py) * (y2 - py));
        if (AB == AP + PB || Mathf.Abs(AB - (AP + PB)) == 0)
        {
            return true;
        }
        return false;
    }

    public bool LiberalLinePointTest(float x1, float y1, float x2, float y2, float px, float py)
    {
        float AB = Mathf.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        float AP = Mathf.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
        float PB = Mathf.Sqrt((x2 - px) * (x2 - px) + (y2 - py) * (y2 - py));
        if (AB == AP + PB || Mathf.Abs(AB - (AP + PB)) < MARGIN_OF_ERROR * 50.0f)
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
