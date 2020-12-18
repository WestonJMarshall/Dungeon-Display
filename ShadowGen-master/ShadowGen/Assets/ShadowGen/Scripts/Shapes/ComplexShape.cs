using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComplexShape : Shape
{
    private bool split = false;

    const float MARGIN_OF_ERROR = 0.0001f;

    #region Methods
    public override void BuildShadow(CustomLight light)
    {
        CompileEdgeList();

        if (!split)
        {
            edges = Edge.SplitEdgeList(edges);

            //Add the points on the edges to the points list
            points = new List<Vector2>();
            foreach (Edge e in edges)
            {
                points.Add(e.startPoint);
            }

            split = true;
        }

        CompileEdgeList();

        //Whether a regular shadow can be generated
        bool stable = true;

        //Check if the light center is within the shape
        if (IsPointInTriangleArray(light.center,points))
        {
            stable = false;
        }

        //Stores states of the points (0 = not seen, 1 = seen, 2 = raycast point)
        List<int> concavePointStates = new List<int>();

        //Stores states of the points (0 = not seen, 1 = seen, 2 = infinite raycast point 3 = non-infinite raycast point)
        List<int> concavePointStatesSpecific = new List<int>();

        //We will add every new edge for the convex shadow we find to this list
        List<Edge> concaveEdges = new List<Edge>();

        //Direction to count along the shape
        bool countUp = false;

        #region Assign Basic Point Values
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

            //Run two more checks to make sure it actually is a seen point
            if(seen)
            {
                Edge centerToPointExtraCheckA = new Edge(light.Center, points[i] + (centerToPoint.normal.normalized * boundingSphereRadius * 0.01f));
                Edge centerToPointExtraCheckB = new Edge(light.Center, points[i] + (-centerToPoint.normal.normalized * boundingSphereRadius * 0.01f));
                foreach (Edge e in edges)
                {
                    if (Vector2.Distance(e.startPoint, points[i]) > MARGIN_OF_ERROR && Vector2.Distance(e.endPoint, points[i]) > MARGIN_OF_ERROR)
                    {
                        if (centerToPointExtraCheckA.EdgeIntersection(e))
                        {
                            seen = false; //Not a clear path
                            break;
                        }
                        if (centerToPointExtraCheckB.EdgeIntersection(e))
                        {
                            seen = false; //Not a clear path
                            break;
                        }
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
        //We can now decide whether this shape's shadow can be derived using the simpler convex function
        bool addPrimaryEdge = true;
        //Create the list of points on the shadow

        //Index of where to start the list, this value is found in the loop below
        int startPointIndex = -1;
        
        //Find a '2' point that when raycasted will not hit anything
        for (int i = 0; i < concavePointStates.Count; i++)
        {
            if (concavePointStates[i] == 2)
            {
                //Check if there are other points with the same slope from center -> point
                Vector2 slopeA = (points[i] - light.Center).normalized;
                Vector2 slopeB = Vector2.zero;
                bool restartCheck = false;
                for (int c = 0; c < points.Count; c++)
                {
                    if(i != c)
                    {
                        slopeB = (points[c] - light.Center).normalized;
                        if (Vector2.Distance(slopeA, slopeB) <= MARGIN_OF_ERROR * 2.0f) //2 points share the same slope
                        {
                            //Slect the point that is farthest away to be 2
                            if(Vector2.Distance(light.Center,points[i]) > Vector2.Distance(light.Center, points[c]))
                            {
                                concavePointStates[c] = 1;
                            }
                            else
                            {
                                concavePointStates[c] = 2;
                                concavePointStates[i] = 1;
                                i = 0;
                                restartCheck = true;
                                break;
                            }
                        }
                    }
                }
                if(restartCheck)
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

                            //Check two closer points
                            Vector2 offset = points[i] + (rayToCheck.normal.normalized * 0.1f);
                            Edge rayExtraCheckA = new Edge(points[i], rayToCheck.endPoint + offset);
                            Edge rayExtraCheckB = new Edge(points[i], rayToCheck.endPoint - offset);
                            foreach (Edge e2 in edges)
                            {
                                if (!rayExtraCheckA.EdgeIntersection(e2))
                                {
                                    concavePointStatesSpecific[i] = 2;
                                    infinite = true; //clear path
                                    break;
                                }
                                if (!rayExtraCheckA.EdgeIntersection(e2))
                                {
                                    concavePointStatesSpecific[i] = 2;
                                    infinite = true; //clear path
                                    break;
                                }
                            }
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
                //If this is not an infinite point, we must check if it should be a value of 1 or 3
                else
                {
                    Vector2 testPoint = points[i] + ((points[i] - light.Center) * MARGIN_OF_ERROR);
                    //Check if testPoint is within the triangle
                    if (IsPointInTriangleArray(testPoint, points))
                    {
                        concavePointStatesSpecific[i] = 1;
                        countUp = true;
                    }
                }
            }
        }
        
        //This will generally check if the light center is inside of the shape
        value2Points = 0;
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
        bool lineError = false;
        if(value2Points > 2)
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
            //Straight line issue is still possible, try one more fix
            //We're going to guess the direction we should go after
            for(int i = 0; i < concavePointStatesSpecific.Count; i++)
            {
                if(concavePointStatesSpecific[i] == 2)
                {
                    if (i == 0)
                    {
                        if (concavePointStatesSpecific[concavePointStatesSpecific.Count - 1] == 0)
                        {
                            if (concavePointStatesSpecific[i + 1] == 1)
                            {
                                for(int c = 1; true; c++)
                                {
                                    if(c > concavePointStatesSpecific.Count - 2)
                                    {
                                        break;
                                    }
                                    if(concavePointStatesSpecific[c] == 0)
                                    {
                                        concavePointStatesSpecific[c - 1] = 2;
                                        value2Points++;
                                        break;
                                    }
                                }
                            }
                            else { break; }
                        }
                        else
                        {
                            if (concavePointStatesSpecific[i - 1] == 1)
                            {
                                for (int c = concavePointStatesSpecific.Count - 1; true; c--)
                                {
                                    if (c < 1)
                                    {
                                        break;
                                    }
                                    if (concavePointStatesSpecific[c] == 0)
                                    {
                                        concavePointStatesSpecific[c + 1] = 2;
                                        value2Points++;
                                        break;
                                    }
                                }
                            }
                            else { break; }
                        }
                    }
                    else if (i == concavePointStatesSpecific.Count - 1)
                    {
                        if (concavePointStatesSpecific[concavePointStatesSpecific.Count - 2] == 0)
                        {
                            if (concavePointStatesSpecific[0] == 1)
                            {
                                for (int c = 0; true; c++)
                                {
                                    if (c == i)
                                    {
                                        break;
                                    }
                                    if (concavePointStatesSpecific[c] == 0)
                                    {
                                        concavePointStatesSpecific[c - 1] = 2;
                                        value2Points++;
                                        break;
                                    }
                                }
                            }
                            else { break; }
                        }
                        else
                        {
                            if (concavePointStatesSpecific[i - 1] == 1)
                            {
                                for (int c = i - 1; true; c--)
                                {
                                    if (c < 0)
                                    {
                                        break;
                                    }
                                    if (concavePointStatesSpecific[c] == 0)
                                    {
                                        concavePointStatesSpecific[c + 1] = 2;
                                        value2Points++;
                                        break;
                                    }
                                }
                            }
                            else { break; }
                        }
                    }
                    else
                    {
                        if (concavePointStatesSpecific[i - 1] == 0)
                        {
                            if (concavePointStatesSpecific[i + 1] == 1)
                            {
                                for (int c = i + 1; true; c++)
                                {
                                    if(c > concavePointStatesSpecific.Count - 1) { c = 0; }
                                    if (c == i)
                                    {
                                        break;
                                    }
                                    if (concavePointStatesSpecific[c] == 0)
                                    {
                                        concavePointStatesSpecific[c - 1] = 2;
                                        value2Points++;
                                        break;
                                    }
                                }
                            }
                            else { break; }
                        }
                        else
                        {
                            if (concavePointStatesSpecific[i - 1] == 1)
                            {
                                for (int c = i - 1; true; c--)
                                {
                                    if (c < 0) { c = concavePointStatesSpecific.Count - 1; }
                                    if (c == i)
                                    {
                                        break;
                                    }
                                    if (concavePointStatesSpecific[c] == 0)
                                    {
                                        concavePointStatesSpecific[c + 1] = 2;
                                        value2Points++;
                                        break;
                                    }
                                }
                            }
                            else { break; }
                        }
                    }
                }
            }
        }
        #endregion

        if (value2Points != 2)
        {
            stable = false;
        }

        #region Stable Shadow Creation
        if (stable)
        {
            //Is there a chain between the two 2 values?
            //Find a 2 value
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
            for (int i = startPointIndex + 1; true; i++)
            {
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


            //Add the first two edges using the points we found
            concaveEdges.Add(new Edge(extendedInfinitePoint, infinitePoint));
            concaveEdges.Add(new Edge(infinitePoint, points[startPointIndex]));

            int limiter = 0;

            //Loop until you reach the other infinite point (there can only ever be two)
            if (countUp)
            {
                for (int i = startPointIndex + 1; true; i++)
                {
                    //If the limit is reached, there was probably an error
                    limiter++;
                    if (limiter > 50) { break; }

                    if (i > concavePointStates.Count - 1) { i = 0; }

                    if (concavePointStatesSpecific[i] == 0)
                    {
                        //Do the math for that one infinite point -> 0 point adjacent point
                        for (int c = i + 1; true; c++)
                        {
                            if (c > concavePointStatesSpecific.Count - 1) { c = 0; }
                            if (concavePointStatesSpecific[c] == 3) //Reached a non-infinite point
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
                                Edge closestEdge = hitEdges[0];
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
                                Debug.Log("Error");
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
                        if (addPrimaryEdge)
                        {
                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[i]));
                        }
                        else
                        {
                            addPrimaryEdge = true;
                        }

                        //Raycast
                        List<Edge> hitEdges = new List<Edge>();
                        Edge edgeToTest = new Edge(points[i], points[i] + ((points[i] - light.Center) * 1000));

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
                        //This is not an infinite point, keep the loop going

                        //Test which edge is closest
                        Edge closestEdge = hitEdges[0];
                        if (hitEdges.Count > 1)
                        {
                            foreach (Edge e in hitEdges)
                            {
                                if (Vector2.Distance(edgeToTest.EdgeIntersectionPoint(e), points[i]) < Vector2.Distance(edgeToTest.EdgeIntersectionPoint(closestEdge), points[i]))
                                {
                                    closestEdge = e;
                                }
                            }
                        }

                        //Add the raycast line and then the  rest of the edge line
                        concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, edgeToTest.EdgeIntersectionPoint(closestEdge)));

                        //Select the point on the line that has a value of 2 or 1
                        //concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, closestEdge.endPoint));

                        //Set i to be the index of the closest edges endpoint
                        for (int c = i + 1; true; c++)
                        {
                            if (c >= concavePointStates.Count - 1)
                            {
                                c = 0;
                            }
                            if (Mathf.Abs(Vector2.Distance(points[c], closestEdge.endPoint)) <= MARGIN_OF_ERROR)
                            {
                                if (concavePointStates[c] != 0)
                                {
                                    concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, closestEdge.endPoint));
                                    i = c - 1;
                                    addPrimaryEdge = false;
                                    break;
                                }
                            }
                            if (Mathf.Abs(Vector2.Distance(points[c], closestEdge.startPoint)) <= MARGIN_OF_ERROR)
                            {
                                if (concavePointStates[c] != 0)
                                {
                                    concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, closestEdge.startPoint));
                                    i = c - 1;
                                    addPrimaryEdge = false;
                                    break;
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
                    if (limiter > 50) { break; }

                    if (i < 0) { i = concavePointStates.Count - 1; }

                    if (concavePointStatesSpecific[i] == 0)
                    {
                        //Do the math for that one infinite point -> 0 point adjacent point
                        for (int c = i - 1; true; c--)
                        {
                            if (c < 0) { c = concavePointStatesSpecific.Count - 1; }
                            if (concavePointStatesSpecific[c] == 3) //Reached a non-infinite point
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
                                Edge closestEdge = hitEdges[0];
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
                                Debug.Log("Error");
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
                        if (addPrimaryEdge)
                        {
                            concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, points[i]));
                        }
                        else
                        {
                            addPrimaryEdge = true;
                        }

                        //Raycast
                        List<Edge> hitEdges = new List<Edge>();
                        Edge edgeToTest = new Edge(points[i], points[i] + ((points[i] - light.Center) * 1000)); //Testing the wrong line

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
                        Edge closestEdge = hitEdges[0];
                        if (hitEdges.Count > 1)
                        {
                            foreach (Edge e in hitEdges)
                            {
                                if (Vector2.Distance(edgeToTest.EdgeIntersectionPoint(e), points[i]) < Vector2.Distance(edgeToTest.EdgeIntersectionPoint(closestEdge), points[i]))
                                {
                                    closestEdge = e;
                                }
                            }
                        }

                        //Add the raycast line and then the  rest of the edge line
                        concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, edgeToTest.EdgeIntersectionPoint(closestEdge)));

                        //Select the point on the line that has a value of 2 or 1
                        //Set i to be the index of the closest edges endpoint
                        for (int c = i - 1; true; c--)
                        {
                            if (c <= 0)
                            {
                                c = concavePointStates.Count - 1;
                            }
                            if (Mathf.Abs(Vector2.Distance(points[c], closestEdge.endPoint)) <= MARGIN_OF_ERROR)
                            {
                                if (concavePointStates[c] != 0)
                                {
                                    concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, closestEdge.endPoint));
                                    i = c + 1;
                                    addPrimaryEdge = false;
                                    break;
                                }
                            }
                            if (Mathf.Abs(Vector2.Distance(points[c], closestEdge.startPoint)) <= MARGIN_OF_ERROR)
                            {
                                if (concavePointStates[c] != 0)
                                {
                                    concaveEdges.Add(new Edge(concaveEdges[concaveEdges.Count - 1].endPoint, closestEdge.startPoint));
                                    i = c + 1;
                                    addPrimaryEdge = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            //Make sure the generated shadow is clean
            List<Edge> cleanConcaveEdges = new List<Edge>();
            //If an edge has a very short length, delete it
            for (int i = 0; i < concaveEdges.Count; i++)
            {
                if (Vector2.Distance(concaveEdges[i].startPoint, concaveEdges[i].endPoint) < 0.1f)
                {
                    if (cleanConcaveEdges.Count != 0)
                    {
                        cleanConcaveEdges[cleanConcaveEdges.Count - 1].endPoint = concaveEdges[i].endPoint;
                    }
                }
                else
                {
                    cleanConcaveEdges.Add(concaveEdges[i]);
                }
            }

            //Add all of the found points to the list that is used in light calculations
            compiledShadowEdges = new List<Edge>();
            foreach (Edge e in cleanConcaveEdges)
            {
                compiledShadowEdges.Add(new Edge(e.startPoint, e.endPoint));
            }
            compiledShadowPoints = new List<Vector2>();
            foreach (Edge e in cleanConcaveEdges)
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
    }

    public override bool ContainsPoint(Vector2 point)
    {
        if(points != null)
        {
            return IsPointInTriangleArray(point, points);
        }
        return false;
    }
    #endregion
}
