using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ComplexShape : Shape
{
    const float MARGIN_OF_ERROR = 0.0001f;

    #region Methods
    public override void BuildShadow(CustomLight light)
    {
        CompileEdgeList();

        bool stable = true;
        bool lineError = false;
        List<int> concavePointStatesSpecific = new List<int>();

        CalculatePointsList(light, out concavePointStatesSpecific, out stable, out lineError);

        #region Stable Shadow Creation
        if (stable)
        {
            //Is there a chain between the two 2 values?
            //Find a 2 value
            int startPointIndex = 0;
            bool countUp = true;
            for (int i = 0; i < concavePointStatesSpecific.Count; i++)
            {
                if (concavePointStatesSpecific[i] == 2)
                {
                    startPointIndex = i;
                    break;
                }
            }
            //check which direction is only 0s between them
            bool down = true;
            int calcLimit = 0;
            for (int i = startPointIndex + 1; true; i++)
            {
                calcLimit++;
                if(calcLimit > 1000) { break; }
                if (i == startPointIndex) { break; }
                if (i > concavePointStatesSpecific.Count - 1) { i = 0; }
                if (concavePointStatesSpecific[i] == 2)
                {
                    countUp = false;
                    down = false;
                    break;
                }
                if (concavePointStatesSpecific[i] != 0)
                {
                    countUp = true;
                    down = false;
                    break;
                }
            }
            if (down) { countUp = false; }

            //Add the first two infinite points
            Vector2 infinitePoint;
            Vector2 extendedInfinitePoint;

            infinitePoint = points[startPointIndex] + ((points[startPointIndex] - light.Center) * 1000);

            //Add a point that is far along the general direction of the shape
            Vector2 distanceVector = Vector2.zero;

            List<Vector2> distanceVectors = new List<Vector2>();

            for (int i = 0; i < concavePointStatesSpecific.Count; i++)
            {
                if (concavePointStatesSpecific[i] == 2)
                {
                    distanceVectors.Add(points[i]);
                }
            }

            Vector2 halfDistanceVector = Vector2.zero;
            if (distanceVectors.Count == 2)
            {
                halfDistanceVector = distanceVectors[0] + ((distanceVectors[1] - distanceVectors[0]) / 2.0f);
            }

            //Check the raycast
            Edge lineToCheckA = new Edge(halfDistanceVector, halfDistanceVector + ((halfDistanceVector - light.Center) * 1000));
            Edge lineToCheckB = new Edge(light.Center, halfDistanceVector);

            bool reversedDirection = true;
            int primaryLineCrosses = 0;

            foreach (Edge e in edges)
            {
                if (lineToCheckB.EdgeIntersection(e))
                {
                    primaryLineCrosses++;
                }
            }
            if (primaryLineCrosses > 1)
            {
                reversedDirection = false;
            }

            foreach (Edge e in edges)
            {
                if (lineToCheckA.EdgeIntersection(e))
                {
                    reversedDirection = false;
                    break;
                }
            }

            distanceVector = halfDistanceVector - light.Center;
            if (reversedDirection)
            {
                distanceVector *= -1;
            }
            if(lineError)
            {
                distanceVector = halfDistanceVector - light.Center;
            }

            distanceVector = distanceVector.normalized;

            infinitePoint = infinitePoint.normalized * 1000;
            extendedInfinitePoint = infinitePoint + (distanceVector * 1000);

            List<Edge> concaveEdges = new List<Edge>();

            //Add the first two edges using the points we found
            concaveEdges.Add(new Edge(extendedInfinitePoint, infinitePoint));
            concaveEdges.Add(new Edge(infinitePoint, points[startPointIndex]));

            int limiter = 0;

            bool addPrimaryEdge = true;

            //Loop until you reach the other infinite point (there can only ever be two)
            if (countUp)
            {
                for (int i = startPointIndex + 1; true; i++)
                {
                    //If the limit is reached, there was probably an error
                    limiter++;
                    if (limiter > 1500) { break; }

                    if (i > concavePointStatesSpecific.Count - 1) { i = 0; }

                    if (concavePointStatesSpecific[i] == 0)
                    {
                        //Do the math for that one infinite point -> 0 point adjacent point
                        for (int c = i + 1; true; c++)
                        {
                            limiter++;
                            if (limiter > 150) { break; }
                            if (c > concavePointStatesSpecific.Count - 1) { c = 0; }
                            if (concavePointStatesSpecific[c] == 3 || concavePointStatesSpecific[c] == 1) //Reached a non-infinite point
                            {
                                //Raycast
                                List<Edge> hitEdges = new List<Edge>();
                                Edge edgeToTest = new Edge(points[c], points[c] + ((points[c] - light.Center) * 1000));

                                foreach (Edge e in edges)
                                {
                                    if (Vector2.Distance(e.startPoint, edgeToTest.startPoint) > MARGIN_OF_ERROR && Vector2.Distance(e.endPoint, edgeToTest.startPoint) > MARGIN_OF_ERROR)
                                    {
                                        if (edgeToTest.EdgeIntersection(e))
                                        {
                                            hitEdges.Add(e);
                                        }
                                    }
                                }

                                //Test which edge is closest
                                Edge closestEdge = hitEdges.Count > 0 ? hitEdges[0] : edges[0];
                                if (hitEdges.Count > 1)
                                {
                                    foreach (Edge e in hitEdges)
                                    {
                                        if (Vector2.Distance(edgeToTest.EdgeIntersectionPoint(e), points[c]) < Vector2.Distance(edgeToTest.EdgeIntersectionPoint(closestEdge), points[c]))
                                        {
                                            closestEdge = e;
                                        }
                                    }
                                }

                                //Add the raycast line and then the  rest of the edge line
                                Edge firstEdge = new Edge(edgeToTest.EdgeIntersectionPoint(closestEdge), points[c]);
                                Edge secondEdge = new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, firstEdge.startPoint);

                                concaveEdges.Add(secondEdge);
                                concaveEdges.Add(firstEdge);

                                i = c;
                                break;
                            }
                            else if (concavePointStatesSpecific[c] == 2)
                            {
                                break;
                            }
                        }
                        //Return to the standard loop
                        continue;
                    }
                    if (concavePointStatesSpecific[i] == 1)
                    {
                        if (addPrimaryEdge)
                        {
                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[i]));
                        }
                        else
                        {
                            addPrimaryEdge = true;
                        }
                        continue;
                    }
                    if (concavePointStatesSpecific[i] == 2)
                    {
                        if (addPrimaryEdge)
                        {
                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[i]));
                        }
                        else
                        {
                            addPrimaryEdge = true;
                        }

                        infinitePoint = points[i] + ((points[i] - light.Center) * 1000);
                        infinitePoint = infinitePoint.normalized * 1000;
                        extendedInfinitePoint = infinitePoint + (distanceVector * 1000);

                        //Add the second to last two edges using the points we found
                        concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, infinitePoint));
                        concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, extendedInfinitePoint));

                        //Complete the loop
                        concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, concaveEdges[0].startPoint));

                        //The shadow creation process is done now
                        break;
                    }
                    if (concavePointStatesSpecific[i] == 3)
                    {
                        //Add the point to the edge list first
                        if (!addPrimaryEdge)
                        {
                            addPrimaryEdge = true;
                        }
                        else
                        {
                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[i]));


                            limiter = 0;
                            //Find the next non-zero value
                            for (int c = i + 1; true; c++)
                            {
                                limiter++;
                                if (limiter > concavePointStatesSpecific.Count) { break; }

                                if (c > concavePointStatesSpecific.Count - 1)
                                {
                                    c = 0;
                                }
                                Edge edgeToTest = new Edge(points[i], points[i] + ((points[i] - light.Center) * 1000));

                                if (concavePointStatesSpecific[c] != 0)
                                {
                                    Vector2 attachPoint = Vector2.zero;

                                    if (c == 0)
                                    {
                                        if (edgeToTest.EdgeIntersection(new Edge(points[c], points[concavePointStatesSpecific.Count - 1])))
                                        {
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, edgeToTest.EdgeIntersectionPoint(new Edge(points[c], points[concavePointStatesSpecific.Count - 1]))));
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[c]));
                                            i = c - 1;
                                            addPrimaryEdge = false;
                                            break;
                                        }
                                        else if (edgeToTest.PointIntersection(points[concavePointStatesSpecific.Count - 1])) //Check for previous point intersection
                                        {
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[concavePointStatesSpecific.Count - 1]));
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[c]));
                                            i = c - 1;
                                            addPrimaryEdge = false;
                                            break;
                                        }
                                        else
                                        {
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[c]));
                                            i = c - 1;
                                            addPrimaryEdge = false;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (edgeToTest.EdgeIntersection(new Edge(points[c], points[c - 1])))
                                        {
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, edgeToTest.EdgeIntersectionPoint(new Edge(points[c], points[c - 1]))));
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[c]));
                                            i = c - 1;
                                            addPrimaryEdge = false;
                                            break;
                                        }
                                        else if (edgeToTest.PointIntersection(points[c - 1])) //Check for previous point intersection
                                        {
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[c - 1]));
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[c]));
                                            i = c - 1;
                                            addPrimaryEdge = false;
                                            break;
                                        }
                                        else
                                        {
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[c]));
                                            i = c - 1;
                                            addPrimaryEdge = false;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else //Counting down
            {
                for (int i = startPointIndex - 1; true; i--)
                {
                    limiter++;
                    if (limiter > 1500) { break; }

                    if (i < 0) { i = concavePointStatesSpecific.Count - 1; }

                    if (concavePointStatesSpecific[i] == 0)
                    {
                        //Do the math for that one infinite point -> 0 point adjacent point
                        for (int c = i - 1; true; c--)
                        {
                            limiter++;
                            if (limiter > 150) { break; }
                            if (c < 0) { c = concavePointStatesSpecific.Count - 1; }
                            if (concavePointStatesSpecific[c] == 3 || concavePointStatesSpecific[c] == 1) //Reached a non-infinite point
                            {
                                //Raycast
                                List<Edge> hitEdges = new List<Edge>();
                                Edge edgeToTest = new Edge(points[c], points[c] + ((points[c] - light.Center) * 1000));

                                foreach (Edge e in edges)
                                {
                                    if (Vector2.Distance(e.startPoint, edgeToTest.startPoint) > MARGIN_OF_ERROR && Vector2.Distance(e.endPoint, edgeToTest.startPoint) > MARGIN_OF_ERROR)
                                    {
                                        if (edgeToTest.EdgeIntersection(e))
                                        {
                                            hitEdges.Add(e);
                                        }
                                    }
                                }

                                //Test which edge is closest
                                Edge closestEdge = hitEdges.Count > 0 ? hitEdges[0] : edges[0];
                                if (hitEdges.Count > 1)
                                {
                                    foreach (Edge e in hitEdges)
                                    {
                                        if (Vector2.Distance(edgeToTest.EdgeIntersectionPoint(e), points[c]) < Vector2.Distance(edgeToTest.EdgeIntersectionPoint(closestEdge), points[c]))
                                        {
                                            closestEdge = e;
                                        }
                                    }
                                }

                                //Add the raycast line and then the  rest of the edge line
                                Edge firstEdge = new Edge(edgeToTest.EdgeIntersectionPoint(closestEdge), points[c]);
                                Edge secondEdge = new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, firstEdge.startPoint);

                                concaveEdges.Add(secondEdge);
                                concaveEdges.Add(firstEdge);

                                i = c;
                                break;
                            }
                            else if (concavePointStatesSpecific[c] == 2)
                            {
                                break;
                            }
                        }
                        //Return to the standard loop
                        continue;
                    }
                    if (concavePointStatesSpecific[i] == 1)
                    {
                        if (addPrimaryEdge)
                        {
                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[i]));
                        }
                        else
                        {
                            addPrimaryEdge = true;
                        }
                        continue;
                    }
                    if (concavePointStatesSpecific[i] == 2)
                    {
                        //Add the point to the edge list first
                        if (addPrimaryEdge)
                        {
                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[i]));
                        }
                        else
                        {
                            addPrimaryEdge = true;
                        }

                        infinitePoint = points[i] + ((points[i] - light.Center) * 1000);
                        infinitePoint = infinitePoint.normalized * 1000;
                        extendedInfinitePoint = infinitePoint + (distanceVector * 1000);

                        //Add the second to last two edges using the points we found
                        concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, infinitePoint));
                        concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, extendedInfinitePoint));

                        //Complete the loop
                        concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, concaveEdges[0].startPoint));

                        //The shadow creation process is done now
                        break;
                    }
                    if (concavePointStatesSpecific[i] == 3)
                    {
                        //Add the point to the edge list first
                        if (!addPrimaryEdge)
                        {
                            addPrimaryEdge = true;
                        }
                        else
                        {
                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[i]));



                            limiter = 0;
                            //Find the next non-zero value
                            for (int c = i - 1; true; c--)
                            {
                                limiter++;
                                if (limiter > concavePointStatesSpecific.Count) { break; }

                                if (c < 0)
                                {
                                    c = concavePointStatesSpecific.Count - 1;
                                }
                                Edge edgeToTest = new Edge(points[i], points[i] + ((points[i] - light.Center) * 1000));

                                if (concavePointStatesSpecific[c] != 0)
                                {
                                    Vector2 attachPoint = Vector2.zero;

                                    if (c == concavePointStatesSpecific.Count - 1)
                                    {
                                        if (edgeToTest.EdgeIntersection(new Edge(points[c], points[0])))
                                        {
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, edgeToTest.EdgeIntersectionPoint(new Edge(points[c], points[0]))));
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[c]));
                                            i = c + 1;
                                            addPrimaryEdge = false;
                                            break;
                                        }
                                        else if (edgeToTest.PointIntersection(points[0])) //Check for previous point intersection
                                        {
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[0]));
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[c]));
                                            i = c + 1;
                                            addPrimaryEdge = false;
                                            break;
                                        }
                                        else
                                        {
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[c]));
                                            i = c + 1;
                                            addPrimaryEdge = false;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (edgeToTest.EdgeIntersection(new Edge(points[c], points[c + 1])))
                                        {
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, edgeToTest.EdgeIntersectionPoint(new Edge(points[c], points[c + 1]))));
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[c]));
                                            i = c + 1;
                                            addPrimaryEdge = false;
                                            break;
                                        }
                                        else if (edgeToTest.PointIntersection(points[c + 1])) //Check for previous point intersection
                                        {
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[c + 1]));
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[c]));
                                            i = c + 1;
                                            addPrimaryEdge = false;
                                            break;
                                        }
                                        else
                                        {
                                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[c]));
                                            i = c + 1;
                                            addPrimaryEdge = false;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            List<Vector2> cleanPoints = new List<Vector2>();

            foreach (Edge e in concaveEdges)
            {
                cleanPoints.Add(e.startPoint);
            }

            try
            {
                while (RemoveZeroAreaTriangles(cleanPoints, out cleanPoints)) { }
            }
            catch { }

            List<Edge> cleanEdges = new List<Edge>();
            for (int i = 0; i < cleanPoints.Count; i++)
            {
                if(i == cleanPoints.Count - 1)
                {
                    cleanEdges.Add(new Edge(cleanPoints[i], cleanPoints[0]));
                }
                else
                {
                    cleanEdges.Add(new Edge(cleanPoints[i], cleanPoints[i + 1]));
                }
            }

            //Add all of the found points to the list that is used in light calculations
            compiledShadowEdges = new List<Edge>();
            foreach (Edge e in cleanEdges)
            {
                compiledShadowEdges.Add(new Edge(e.startPoint, e.endPoint));
            }
            compiledShadowPoints = new List<Vector2>();
            foreach (Edge e in cleanEdges)
            {
                compiledShadowPoints.Add(e.startPoint);
            }
        }
        #endregion

        else
        {
            CompileEdgeList();
            //Add all of the found points to the list that is used in light calculations
            compiledShadowEdges = new List<Edge>();
            foreach (Edge e in edges)
            {
                compiledShadowEdges.Add(new Edge(e.startPoint, e.endPoint));
            }
            compiledShadowPoints = new List<Vector2>();
            foreach (Edge e in edges)
            {
                compiledShadowPoints.Add(e.startPoint);
            }
        }

        while (RemoveZeroAreaTriangles(points, out points)) { }
    }

    #region Helper Functions

    public void CalculatePointsList(CustomLight light, out List<int> concavePointStatesSpecific, out bool stable, out bool lineError)
    {

        //Whether a regular shadow can be generated
        stable = true;

        //Check if the light center is within the shape
        if (IsPointInTriangleArray(light.center, points))
        {
            stable = false;
        }

        //Stores states of the points (0 = not seen, 1 = seen, 2 = raycast point)
        List<int> concavePointStates = new List<int>();

        //Stores states of the points (0 = not seen, 1 = seen, 2 = infinite raycast point 3 = non-infinite raycast point)
        concavePointStatesSpecific = new List<int>();

        //We will add every new edge for the convex shadow we find to this list
        List<Edge> concaveEdges = new List<Edge>();

        //Direction to count along the shape
        bool countUp = false;

        #region Assign Basic Point Values
        //Remove extra points
        while (RemoveZeroAreaTriangles(points, out points)) { }
        CompileEdgeList();

        //Loop through each point in the shape and assign it a value of seen or not seen
        for (int i = 0; i < points.Count; i++)
        {
            //Raycast to the point
            Edge centerToPoint = new Edge(light.Center, points[i]);

            //Is there a clear path from the light center to the point?
            bool seen = true;
            foreach (Edge e in edges)
            {
                if (Vector2.Distance(e.startPoint, points[i]) > MARGIN_OF_ERROR && Vector2.Distance(e.endPoint, points[i]) > MARGIN_OF_ERROR)
                {
                    if (centerToPoint.EdgeIntersection(e))
                    {
                        seen = false; //Not a clear path
                        break;
                    }
                }
            }

            if (seen)
            {
                concavePointStates.Add(1);
                concavePointStatesSpecific.Add(1);
            }
            else
            {
                concavePointStates.Add(0);
                concavePointStatesSpecific.Add(0);
            }
        }

        while (AddSameLinePoints(concavePointStatesSpecific, light, out concavePointStatesSpecific)) ;

        //Split and assign split points values
        edges = Edge.SplitEdgeList(edges);

        //Add the points on the edges to the points list
        points = new List<Vector2>();
        foreach (Edge e in edges)
        {
            points.Add(e.startPoint);
        }

        List<int> concavePointsToAdd = new List<int>();
        for (int i = 1; i < points.Count; i += 2)
        {
            Edge centerToPoint = new Edge(light.Center, points[i]);

            //Is there a clear path from the light center to the point?
            bool seen = true;
            foreach (Edge e in edges)
            {
                if (Vector2.Distance(e.startPoint, points[i]) > MARGIN_OF_ERROR && Vector2.Distance(e.endPoint, points[i]) > MARGIN_OF_ERROR)
                {
                    if (centerToPoint.EdgeIntersection(e))
                    {
                        seen = false; //Not a clear path
                        break;
                    }
                }
            }
            if (seen)
            {
                concavePointsToAdd.Add(1);
            }
            else
            {
                concavePointsToAdd.Add(0);
            }
        }

        bool cPointAdd = false;
        List<int> newCPS = new List<int>();
        for (int i = 0; i < (points.Count) / 2; i++)
        {
            if (cPointAdd)
            {
                i -= 1;
                newCPS.Add(concavePointsToAdd[i]);
                cPointAdd = false;
            }
            else
            {
                newCPS.Add(concavePointStatesSpecific[i]);
                cPointAdd = true;
            }
        }
        newCPS.Add(concavePointsToAdd[concavePointsToAdd.Count - 1]);
        concavePointStatesSpecific = new List<int>();

        for (int i = 0; i < newCPS.Count; i++)
        {
            concavePointStatesSpecific.Add(newCPS[i]);
        }

        while (AddSameLinePoints(concavePointStatesSpecific, light, out concavePointStatesSpecific)) ;
        concavePointStates = new List<int>();
        foreach (int i in concavePointStatesSpecific) { concavePointStates.Add(i); }

        //Does this need to be calculated as concave?
        int value2Points = 0;

        //Check for points that are on the end of a 'seen chain' ex. 000011110000 - the two 1 points that border 0s need to become 2s
        for (int i = 0; i < concavePointStates.Count; i++)
        {
            if (i == 0)
            {
                if (concavePointStates[i] == 1)
                {
                    if (concavePointStates[concavePointStates.Count - 1] == 0 || concavePointStates[i + 1] == 0)
                    {
                        value2Points++;
                        concavePointStates[i] = 2;
                        concavePointStatesSpecific[i] = 2;
                    }
                }
            }
            else if (i != concavePointStates.Count - 1)
            {
                if (concavePointStates[i] == 1)
                {
                    if (concavePointStates[i - 1] == 0 || concavePointStates[i + 1] == 0)
                    {
                        value2Points++;
                        concavePointStates[i] = 2;
                        concavePointStatesSpecific[i] = 2;
                    }
                }
            }
            else
            {
                if (concavePointStates[i] == 1)
                {
                    if (concavePointStates[i - 1] == 0 || concavePointStates[0] == 0)
                    {
                        value2Points++;
                        concavePointStates[i] = 2;
                        concavePointStatesSpecific[i] = 2;
                    }
                }
            }
        }
        #endregion

        #region Setup Points List with Specific Values
        //Create the list of points on the shadow

        //Index of where to start the list, this value is found in the loop below
        int startPointIndex = -1;

        value2Points = 0;
        for (int i = 0; i < concavePointStatesSpecific.Count; i++)
        {
            if (concavePointStatesSpecific[i] == 2)
            {
                value2Points++;
            }
        }

        //Find a '2' point that when raycasted will not hit anything
        for (int i = 0; i < concavePointStates.Count; i++)
        {
            if (concavePointStates[i] == 2)
            {
                //Check if there are other points with the same slope from center -> point
                Vector2 slopeA = (points[i] - light.Center).normalized;
                Vector2 slopeB = Vector2.zero;
                bool restartCheck = false;

                for (int c = 0; c < edges.Count; c++)
                {
                    if (edges[c].startPoint == points[i])
                    {
                        if (concavePointStates[points.IndexOf(edges[c].endPoint)] == 0)
                        {
                            slopeB = (edges[c].endPoint - light.Center).normalized;
                            if (Vector2.Distance(slopeA, slopeB) <= MARGIN_OF_ERROR * 10.0f || Vector2.Distance(-slopeA, slopeB) <= MARGIN_OF_ERROR * 10.0f) //2 points share the same slope
                            {
                                concavePointStates[points.IndexOf(edges[c].endPoint)] = 2;
                                concavePointStates[i] = 1;
                                concavePointStatesSpecific[points.IndexOf(edges[c].endPoint)] = 2;
                                concavePointStatesSpecific[i] = 1;
                                i = -1;
                                restartCheck = true;
                                break;
                            }
                        }
                    }
                    else if (edges[c].endPoint == points[i])
                    {
                        if (concavePointStates[points.IndexOf(edges[c].startPoint)] == 0)
                        {
                            slopeB = (edges[c].startPoint - light.Center).normalized;
                            if (Vector2.Distance(slopeA, slopeB) <= MARGIN_OF_ERROR * 10.0f || Vector2.Distance(-slopeA, slopeB) <= MARGIN_OF_ERROR * 10.0f) //2 points share the same slope
                            {
                                concavePointStates[points.IndexOf(edges[c].startPoint)] = 2;
                                concavePointStates[i] = 1;
                                concavePointStatesSpecific[points.IndexOf(edges[c].startPoint)] = 2;
                                concavePointStatesSpecific[i] = 1;
                                i = -1;
                                restartCheck = true;
                                break;
                            }
                        }
                    }
                }
                if (restartCheck)
                {
                    continue;
                }

                //Select the point that is farthest along that slope

                //Check the raycast
                Edge rayToCheck = new Edge(points[i], points[i] + ((points[i] - light.Center) * 1000));

                bool infinite = true;

                foreach (Edge e in edges)
                {
                    if (Vector2.Distance(e.startPoint, rayToCheck.startPoint) > MARGIN_OF_ERROR && Vector2.Distance(e.endPoint, rayToCheck.startPoint) > MARGIN_OF_ERROR)
                    {
                        if (rayToCheck.EdgeIntersection(e))
                        {
                            infinite = false; //Can check here?
                            concavePointStatesSpecific[i] = 3;
                            value2Points -= 1;
                            break;
                        }
                    }
                }
                if (infinite)
                {
                    //Find the direction to count
                    if (i == 0)
                    {
                        if (concavePointStates[concavePointStates.Count - 1] == 0)
                        {
                            startPointIndex = i;
                            countUp = true;
                        }
                    }
                    else if (i == concavePointStates.Count - 1)
                    {
                        if (concavePointStates[i - 1] == 0)
                        {
                            startPointIndex = i;
                            countUp = true;
                        }
                    }
                    else
                    {
                        if (concavePointStates[i - 1] == 0)
                        {
                            startPointIndex = i;
                            countUp = true;
                        }
                    }
                }
            }
        }

        //This will generally check if the light center is inside of the shape
        value2Points = 0;
        int value3Points = 0;
        for (int i = 0; i < concavePointStatesSpecific.Count; i++)
        {
            if (concavePointStatesSpecific[i] == 2)
            {
                value2Points++;
            }
        }
        #endregion

        #region Line Test Error Check 1
        //check for an error where the line test can fail
        lineError = false;
        if (value2Points > 2)
        {
            for (int i = 0; i < concavePointStatesSpecific.Count; i++)
            {
                if (concavePointStatesSpecific[i] == 2)
                {
                    if (i == 0)
                    {
                        if (concavePointStatesSpecific[concavePointStatesSpecific.Count - 1] == 0)
                        {
                            if (concavePointStatesSpecific[i + 1] == 0)
                            {
                                concavePointStatesSpecific[i] = 0;
                                concavePointStates[i] = 0;
                                value2Points--;
                                lineError = true;
                            }
                        }
                    }
                    else if (i == concavePointStatesSpecific.Count - 1)
                    {
                        if (concavePointStatesSpecific[i - 1] == 0)
                        {
                            if (concavePointStatesSpecific[0] == 0)
                            {
                                concavePointStatesSpecific[i] = 0;
                                concavePointStates[i] = 0;
                                value2Points--;
                                lineError = true;
                            }
                        }
                    }
                    else
                    {
                        if (concavePointStatesSpecific[i - 1] == 0)
                        {
                            if (concavePointStatesSpecific[i + 1] == 0)
                            {
                                concavePointStatesSpecific[i] = 0;
                                concavePointStates[i] = 0;
                                value2Points--;
                                lineError = true;
                            }
                        }
                    }
                }
            }
        }
        #endregion 

        #region Line Test Error Check 2
        if (value2Points == 1)
        {
            value3Points = 0;
            foreach (int i in concavePointStatesSpecific)
            {
                if (i == 3)
                {
                    value3Points++;
                }
            }
            if (value3Points != 1)
            {
                //concavePointStatesSpecific[concavePointStatesSpecific.IndexOf(3)] = 2;
                //value2Points++;
                //value3Points--;
            }
            else
            {
                for (int i = 0; i < concavePointStatesSpecific.Count; i++)
                {
                    if (concavePointStatesSpecific[i] == 3)
                    {
                        //Check the raycast
                        Edge rayToCheck = new Edge(points[i], points[i] + ((points[i] - light.Center) * 1000));

                        bool infinite = true;

                        foreach (Edge e in edges)
                        {
                            if (Vector2.Distance(e.startPoint, rayToCheck.startPoint) > MARGIN_OF_ERROR && Vector2.Distance(e.endPoint, rayToCheck.startPoint) > MARGIN_OF_ERROR)
                            {
                                if (rayToCheck.EdgeIntersection(e))
                                {
                                    if (!((rayToCheck.PointIntersection(e.startPoint) && concavePointStatesSpecific[points.IndexOf(e.startPoint)] == 0) || (rayToCheck.PointIntersection(e.endPoint) && concavePointStatesSpecific[points.IndexOf(e.endPoint)] == 0)))
                                    {
                                        infinite = false; //Can check here?
                                        value3Points -= 1;
                                        break;
                                    }
                                }
                            }
                        }
                        if (infinite)
                        {
                            concavePointStatesSpecific[i] = 2;
                            value2Points++;
                        }
                    }
                }
            }
            if (value2Points == 1)
            {
                //Straight line issue is still possible, try one more fix
                //We're going to guess the direction we should go after
                for (int i = 0; i < concavePointStatesSpecific.Count; i++)
                {
                    if (concavePointStatesSpecific[i] == 2)
                    {
                        startPointIndex = i;
                        break;
                    }
                }
                if (startPointIndex != concavePointStatesSpecific.Count - 1)
                {
                    if (concavePointStatesSpecific[startPointIndex + 1] == 0)
                    {
                        countUp = true;
                    }
                    else
                    {
                        countUp = false;
                    }
                }
                else
                {
                    if (concavePointStatesSpecific[0] == 0)
                    {
                        countUp = true;
                    }
                    else
                    {
                        countUp = false;
                    }
                }

                if (countUp)
                {
                    for (int i = startPointIndex + 1; true; i++)
                    {
                        if (i > concavePointStatesSpecific.Count - 1) { i = 0; }
                        if (i == startPointIndex) { break; }
                        if (concavePointStatesSpecific[i] == 1 || concavePointStatesSpecific[i] == 3)
                        {
                            concavePointStatesSpecific[i] = 2;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = startPointIndex - 1; true; i--)
                    {
                        if (i < 0) { i = concavePointStatesSpecific.Count - 1; }
                        if (i == startPointIndex) { break; }
                        if (concavePointStatesSpecific[i] == 1 || concavePointStatesSpecific[i] == 3)
                        {
                            concavePointStatesSpecific[i] = 2;
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        for (int i = 0; i < concavePointStatesSpecific.Count; i++)
        {
            if (concavePointStatesSpecific[i] == 1)
            {
                concavePointStates[i] = 1;
            }
        }
        value2Points = 0;
        for (int i = 0; i < concavePointStatesSpecific.Count; i++)
        {
            if (concavePointStatesSpecific[i] == 2)
            {
                value2Points++;
            }
        }
        if (value2Points == 1)
        {
            for (int i = 0; i < concavePointStatesSpecific.Count; i++)
            {
                if (concavePointStatesSpecific[i] == 3)
                {
                    concavePointStatesSpecific[i] = 2;
                    value2Points++;
                }
            }
        }

        //Check for 2's between 1s
        if (value2Points > 2)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (concavePointStatesSpecific[i] == 2)
                {
                    if (i == 0)
                    {
                        if (concavePointStatesSpecific[concavePointStatesSpecific.Count - 1] != 0 && concavePointStatesSpecific[i + 1] != 0)
                        {
                            concavePointStatesSpecific[i] = 1;
                            value2Points--;
                        }
                    }
                    else if (i == concavePointStatesSpecific.Count - 1)
                    {
                        if (concavePointStatesSpecific[0] != 0 && concavePointStatesSpecific[i - 1] != 0)
                        {
                            concavePointStatesSpecific[i] = 1;
                            value2Points--;
                        }
                    }
                    else
                    {
                        if (concavePointStatesSpecific[i + 1] != 0 && concavePointStatesSpecific[i - 1] != 0)
                        {
                            concavePointStatesSpecific[i] = 1;
                            value2Points--;
                        }
                    }
                }
            }
        }

        if (value2Points != 2)
        {
            if (value2Points == 0)
            {
                value3Points = 0;
                foreach (int i in concavePointStatesSpecific)
                {
                    if (i == 3)
                    {
                        value3Points++;
                    }
                }
                if (value3Points == 2)
                {
                    for (int i = 0; i < concavePointStatesSpecific.Count; i++)
                    {
                        if (concavePointStatesSpecific[i] == 3)
                        {
                            concavePointStatesSpecific[i] = 2;
                            value3Points--;
                            value2Points++;
                        }
                    }
                }
            }

            if (value2Points != 2)
            {
                stable = false;
            }
        }

        #region No Zeros Check
        if (!concavePointStatesSpecific.Contains(0))
        {
            for (int i = 0; i < concavePointStatesSpecific.Count; i++)
            {
                if (concavePointStatesSpecific[i] == 1)
                {
                    if (i == 0)
                    {
                        if (concavePointStatesSpecific[concavePointStatesSpecific.Count - 1] == 2 && concavePointStatesSpecific[i + 1] == 2)
                        {
                            concavePointStatesSpecific[i] = 0;
                            break;
                        }
                    }
                    else if (i == concavePointStatesSpecific.Count - 1)
                    {
                        if (concavePointStatesSpecific[i - 1] == 2 && concavePointStatesSpecific[0] == 2)
                        {
                            concavePointStatesSpecific[i] = 0;
                            break;
                        }
                    }
                    else
                    {
                        if (concavePointStatesSpecific[i - 1] == 2 && concavePointStatesSpecific[i + 1] == 2)
                        {
                            concavePointStatesSpecific[i] = 0;
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        #region Fake 3 Value Check
        foreach (int i in concavePointStatesSpecific)
        {
            if (i == 3)
            {
                value3Points++;
            }
        }
        //Check for fake value 3s
        for (int i = 0; i < concavePointStatesSpecific.Count && value3Points > 1; i++)
        {
            if (concavePointStatesSpecific[i] == 3)
            {
                Vector2 p = points[i] + (points[i] - light.center).normalized * 0.5f;
                if (IsPointInTriangleArray(p, points))
                {
                    concavePointStatesSpecific[i] = 1;
                    value3Points--;
                    continue;
                }
            }
        }
        #endregion

        #region 2 Value Check
        //Find the extra value 2 by
        //find value 3 and then the 2 that is connected by 0's
        if (value3Points > 0 && value2Points > 2)
        {
            int startIndex = 0;
            int loopLimit = 0;
            while (value2Points > 2)
            {
                if (loopLimit > 200) { break; }
                loopLimit++;
                int indexOfThree = concavePointStatesSpecific.FindIndex(startIndex, concavePointStatesSpecific.Count - startIndex - 1, i => EqualsThree(i));
                startIndex = indexOfThree;

                countUp = false;
                int above = indexOfThree == concavePointStatesSpecific.Count - 1 ? 0 : indexOfThree + 1;
                int below = indexOfThree == 0 ? concavePointStatesSpecific.Count - 1 : indexOfThree - 1;

                if (concavePointStatesSpecific[above] == 0) { countUp = true; }

                if (countUp)
                {
                    for (int i = above; true; i++)
                    {
                        if (i >= concavePointStatesSpecific.Count) { i = 0; }
                        if (concavePointStatesSpecific[i] == 2) { concavePointStatesSpecific[i] = 1; value2Points--; break; }
                    }
                }
                else
                {
                    for (int i = below; true; i--)
                    {
                        if (i < 0) { i = concavePointStatesSpecific.Count - 1; }
                        if (concavePointStatesSpecific[i] == 2) { concavePointStatesSpecific[i] = 1; value2Points--; break; }
                    }
                }
            }
            stable = true;
        }
        #endregion
    }

    public override bool ContainsPoint(Vector2 point)
    {
        if(points != null)
        {
            return IsPointInTriangleArray(point, points);
        }
        return false;
    }

    private bool FindFreeStandingZero(List<int> inputList, out List<int> outputList)
    {
        for (int i = 0; i < inputList.Count; i++)
        {
            if (inputList[i] == 0)
            {
                if (i == 0)
                {
                    if((inputList[inputList.Count - 1] == 1 || inputList[inputList.Count - 1] == 2) && (inputList[i + 1] == 1 || inputList[i + 1] == 2))
                    {
                        inputList[i] = 1;
                        outputList = inputList;
                        return true;
                    }
                }
                else if (i == inputList.Count - 1)
                {
                    if ((inputList[0] == 1 || inputList[0] == 2) && (inputList[i - 1] == 1 || inputList[i - 1] == 2))
                    {
                        inputList[i] = 1;
                        outputList = inputList;
                        return true;
                    }
                }
                else
                {
                    if ((inputList[i - 1] == 1 || inputList[i - 1] == 2) && (inputList[i + 1] == 1 || inputList[i + 1] == 2))
                    {
                        inputList[i] = 1;
                        outputList = inputList;
                        return true;
                    }
                }
            }
        }

        outputList = inputList;
        return false;
    }

    private bool RemoveZeroAreaTriangles(List<Vector2> inputPoints, out List<Vector2> outputPoints)
    {
        for (int i = 0; i < inputPoints.Count; i++)
        {
            if (i == inputPoints.Count - 2)
            {
                if (!Shape.TriangleAreaTest(inputPoints[i], inputPoints[i + 1], inputPoints[0]))
                {
                    inputPoints.Remove(inputPoints[i + 1]);
                    outputPoints = inputPoints;
                    return true;
                }
            }
            else if (i == inputPoints.Count - 1)
            {
                if (!Shape.TriangleAreaTest(inputPoints[i], inputPoints[0], inputPoints[1]))
                {
                    inputPoints.Remove(inputPoints[0]);
                    outputPoints = inputPoints;
                    return true;
                }
            }
            else
            {
                if (!Shape.TriangleAreaTest(inputPoints[i], inputPoints[i + 1], inputPoints[i + 2]))
                {
                    inputPoints.Remove(inputPoints[i + 1]);
                    outputPoints = inputPoints;
                    return true;
                }
            }
        }
        outputPoints = inputPoints;
        return false;
    }

    private bool AddSameLinePoints(List<int> inputPoints, CustomLight light, out List<int> outputPoints)
    {
        //Find points with same slope edges as center -> point
        for (int i = 0; i < points.Count; i++)
        {
            if (inputPoints[i] == 0)
            {
                foreach (Edge e in edges)
                {
                    Vector2 slopeA = (e.startPoint - e.endPoint).normalized;
                    Vector2 slopeB = (points[i] - light.center).normalized;
                    float testOne = Vector2.Distance(slopeA, slopeB);
                    float testTwo = Vector2.Distance(-slopeA, slopeB);
                    if (e.endPoint == points[i])
                    {
                        if (testOne < 0.04f || testTwo < 0.04f)
                        {
                            for (int c = 0; c < points.Count; c++)
                            {
                                if (points[c] == e.startPoint)
                                {
                                    if (inputPoints[c] == 1)
                                    {
                                        inputPoints[i] = 1;
                                        outputPoints = inputPoints;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    else if (e.startPoint == points[i])
                    {
                        if (testOne < 0.04f || testTwo < 0.04f)
                        {
                            for (int c = 0; c < points.Count; c++)
                            {
                                if (points[c] == e.endPoint)
                                {
                                    if (inputPoints[c] == 1)
                                    {
                                        inputPoints[i] = 1;
                                        outputPoints = inputPoints;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    if (inputPoints[i] != 0) { break; }
                }
            }
        }
        outputPoints = inputPoints;
        return false;
    }

    public bool EqualsThree(int i)
    {
        return i == 3;
    }
    #endregion

    #endregion
}
