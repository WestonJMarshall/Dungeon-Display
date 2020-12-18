using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum DragState
{
    Point,
    Shape,
    None,
}

public class ShadowTool : MonoBehaviour
{
    public GameObject formattingPanelA;
    public GameObject formattingPanelB;
    public GameObject formattingPanelC;
    public GameObject gridSnapToggle;
    public GameObject functionalityToggle;
    public Scrollbar scrollBar;
    public Text warningText;

    public GameObject deletePointButtonPrefab;
    public GameObject pointInformationPrefab;
    public GameObject dragPointPrefab;
    public GameObject pointLinePrefab;
    public GameObject permanentPointLinePrefab;

    public GameObject currentShape;
    public GameObject hoveredButton;
    public GameObject currentDragPoint;

    public List<GameObject> xPoints;
    public List<GameObject> yPoints;
    public List<GameObject> deleteButtons;

    public List<GameObject> dragPoints;
    public List<GameObject> pointLines;

    public List<GameObject> storedPointLines;

    public bool pauseUpdates = false;
    private bool deletePrepare = false;
    private bool gridSnap = false;
    private bool canDrag = true;
    private DragState dragState = DragState.None;

    public void Awake()
    {
        xPoints = new List<GameObject>();
        yPoints = new List<GameObject>();
        deleteButtons = new List<GameObject>();
        dragPoints = new List<GameObject>();
        pointLines = new List<GameObject>();
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(0) && dragState == DragState.None)
        {
            if (ShapeClick())
            {
                canDrag = false;
            }
            else
            {
                canDrag = true;
            }
        }
        if (canDrag && dragState != DragState.Point)
        {
            if (Input.GetMouseButton(0) && currentShape != null)
            {
                if (Shape.IsPointInTriangleArray(Camera.main.ScreenToWorldPoint(Input.mousePosition), currentShape.GetComponent<ComplexShape>().points) && !EventSystem.current.IsPointerOverGameObject() && dragState != DragState.Shape)
                {
                    dragState = DragState.Shape;
                }
                if (dragState == DragState.Shape)
                {
                    DragShape();
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            dragState = DragState.None;
        }

        //Fix an odd load bug
        if (currentShape != null && dragPoints.Count == 0 && currentShape.GetComponent<ComplexShape>().points.Count > 0)
        {
            DragPoint[] dpl = FindObjectsOfType<DragPoint>();

            foreach(DragPoint dp in dpl)
            {
                Destroy(dp.gameObject);
            }

            PointLine[] pll = FindObjectsOfType<PointLine>();

            foreach (PointLine pl in pll)
            {
                Destroy(pl.gameObject);
            }

            SetCurrentShapePointsList();
        }
    }

    public void ServerHandleShadowChange(int shadowIndex, bool delete, List<Vector2> pointValues, bool functionality, bool closeAfterFinish)
    {
        currentShape = Manager.Instance.Shapes[shadowIndex].gameObject;

        if (delete)
        {
            if (currentShape != null)
            {
                Manager.Instance.Shapes.Remove(currentShape.GetComponent<Shape>());
                DeleteCurrentData(true, true);
                Manager.Instance.AssignShapes();
                Manager.Instance.BuildAllLights();

                ResetPermanentLines();
            }
            currentShape = null;
            deletePrepare = true;

            ServerCheckForDestroy();
        }
        else
        {
            if (pointValues.Count != 0)
            {
                currentShape.GetComponent<ComplexShape>().points = new List<Vector2>();
                foreach (Vector2 v in pointValues) { currentShape.GetComponent<ComplexShape>().points.Add(new Vector2(v.x, v.y)); }
            }
            if (functionality != currentShape.GetComponent<ComplexShape>().functional)
            {
                ServerToggleFunctionality();
            }
        }
        SetCurrentShapePointsList();
        Scroll();
        ResetPermanentLines();

        Manager.Instance.FindElementsToManage();

        if (closeAfterFinish) { Manager.Instance.CloseShadowMenu(); }
    }

    public void ServerCheckForDestroy()
    {
        List<GameObject> shadowsToDestroy = new List<GameObject>();
        for(int x = 0; x < Manager.Instance.Shapes.Count; x++)
        {
            for (int i = 0; i < Manager.Instance.Shapes[x].points.Count; i++)
            {
                for (int c = i + 1; c < Manager.Instance.Shapes[x].points.Count; c++)
                {
                    if (Vector2.Distance(Manager.Instance.Shapes[x].points[i], Manager.Instance.Shapes[x].points[c]) < 0.001f)
                    {
                        shadowsToDestroy.Add(Manager.Instance.Shapes[x].gameObject);
                        break;
                    }
                }
            }
            List<Edge> edges = new List<Edge>();
            for (int i = 0; i < Manager.Instance.Shapes[x].points.Count; i++)
            {
                if (i != Manager.Instance.Shapes[x].points.Count - 1)
                {
                    edges.Add(new Edge(Manager.Instance.Shapes[x].points[i], Manager.Instance.Shapes[x].points[i + 1]));
                }
                else
                {
                    edges.Add(new Edge(Manager.Instance.Shapes[x].points[i], Manager.Instance.Shapes[x].points[0]));
                }
            }
            for (int i = 0; i < edges.Count; i++)
            {
                for (int c = i + 1; c < edges.Count; c++)
                {
                    if (edges[i].endPoint != edges[c].startPoint && edges[i].startPoint != edges[c].endPoint)
                    {
                        if (edges[i].EdgeIntersection(edges[c]))
                        {
                            shadowsToDestroy.Add(Manager.Instance.Shapes[x].gameObject);
                            break;
                        }
                    }
                }
            }
            if (Manager.Instance.Shapes[x].points.Count < 3)
            {
                shadowsToDestroy.Add(Manager.Instance.Shapes[x].gameObject);
            }
        }

        for (int x = 0; x < shadowsToDestroy.Count; x++)
        {
            currentShape = shadowsToDestroy[x];
            Manager.Instance.Shapes.Remove(currentShape.GetComponent<Shape>());
            DeleteCurrentData(true, true);
            Manager.Instance.AssignShapes();
            Manager.Instance.BuildAllLights();
            ResetPermanentLines();
            currentShape = null;
            deletePrepare = true;
        }

    }

    public void DragShape()
    {
        if (currentShape != null && !gridSnap)
        {
            bool clickable = true;
            foreach (Vector2 v in currentShape.GetComponent<ComplexShape>().points)
            {
                if (Vector2.Distance(v, Camera.main.ScreenToWorldPoint(Input.mousePosition)) < 0.21f)
                {
                    clickable = false;
                    break;
                }
            }
            if (!clickable) { return; }

            if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
            {
                currentShape.GetComponent<ComplexShape>().GenerateBoundingSphere();
                List<Vector2> points = new List<Vector2>();
                foreach (Vector2 v in currentShape.GetComponent<ComplexShape>().points) { points.Add(new Vector2(v.x, v.y)); }
                for (int i = 0; i < points.Count; i++)
                {
                    points[i] += (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - currentShape.GetComponent<ComplexShape>().boundingSphereCenter;
                }
                string pointValues = "";
                foreach (Vector2 v in points)
                {
                    pointValues += v.x.ToString() + "|" + v.y.ToString() + "|";
                }
                NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"svc{Manager.Instance.Shapes.IndexOf(currentShape.GetComponent<ComplexShape>())}|{0}|{pointValues}{(currentShape.GetComponent<ComplexShape>().functional ? 1 : 0)}"));
            }
            else
            {
                currentShape.GetComponent<ComplexShape>().GenerateBoundingSphere();

                for (int i = 0; i < currentShape.GetComponent<ComplexShape>().points.Count; i++)
                {
                    currentShape.GetComponent<ComplexShape>().points[i] += (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - currentShape.GetComponent<ComplexShape>().boundingSphereCenter;
                }
                if (currentShape.GetComponent<ComplexShape>().functional) { ToggleFunctionality(); }
                SetCurrentShapePointsList();
                ResetPermanentLines();
            }
        }
    }

    public bool ShapeClick()
    {
        if (currentShape != null)
        {
            bool clickable = true;
            foreach (Vector2 v in currentShape.GetComponent<ComplexShape>().points)
            {
                if (Vector2.Distance(v, Camera.main.ScreenToWorldPoint(Input.mousePosition)) < 0.21f)
                {
                    clickable = false;
                    break;
                }
            }
            if (!clickable) { return false; }
        }
        if (currentShape != null && deletePrepare)
        {
            deletePrepare = false;
            SetCurrentShapePointsList();
        }
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            bool errorFound = false;
            GameObject shapeSwap = null;
            foreach (Shape s in Manager.Instance.Shapes)
            {
                if (Shape.IsPointInTriangleArray(Camera.main.ScreenToWorldPoint(Input.mousePosition), s.points))
                {
                    if (Manager.Instance.inspector.gameObject.activeSelf)
                    {
                        Manager.Instance.inspector.CloseInspector();
                    }
                    if (currentShape == null)
                    {
                        currentShape = s.gameObject;
                        break;
                    }
                    else
                    {
                        if (currentShape == s.gameObject)
                        {
                            break;
                        }
                        for (int i = 0; i < dragPoints.Count; i++)
                        {
                            for (int c = i + 1; c < dragPoints.Count; c++)
                            {
                                if (Vector2.Distance(dragPoints[i].transform.position, dragPoints[c].transform.position) < 0.001f)
                                {
                                    errorFound = true;
                                    warningText.text = "Deleted Previous Shape: \n Two or more points overlapping";
                                    WarningTextChanged();
                                    break;
                                }
                            }
                        }
                        List<Edge> edges = new List<Edge>();
                        for (int i = 0; i < dragPoints.Count; i++)
                        {
                            if (i != dragPoints.Count - 1)
                            {
                                edges.Add(new Edge(dragPoints[i].transform.position, dragPoints[i + 1].transform.position));
                            }
                            else
                            {
                                edges.Add(new Edge(dragPoints[i].transform.position, dragPoints[0].transform.position));
                            }
                        }
                        for (int i = 0; i < edges.Count; i++)
                        {
                            for (int c = i + 1; c < edges.Count; c++)
                            {
                                if (edges[i].endPoint != edges[c].startPoint && edges[i].startPoint != edges[c].endPoint)
                                {
                                    if (edges[i].EdgeIntersection(edges[c]))
                                    {
                                        errorFound = true;
                                        warningText.text = "Deleted Previous Shape: \n Two or more lines overlapping";
                                        WarningTextChanged();
                                        break;
                                    }
                                }
                            }
                        }
                        if (currentShape.GetComponent<ComplexShape>().points.Count < 3)
                        {
                            errorFound = true;
                            warningText.text = "Deleted Previous Shape: \n Less than three points";
                            WarningTextChanged();
                        }
                        if (!Manager.Instance.Shapes.Contains(currentShape.GetComponent<ComplexShape>()))
                        {
                            errorFound = true;
                            warningText.text = "Deleted Previous Shape: \n Not enough points";
                            WarningTextChanged();
                        }
                        shapeSwap = s.gameObject;
                    }
                }
            }
            if (shapeSwap != null)
            {
                if (errorFound)
                {
                    DeleteShape();
                    currentShape = shapeSwap;
                    SetCurrentShapePointsList();
                }
                else
                {
                    StorePermanentLines();
                    DeleteCurrentData(true, false);
                    currentShape = shapeSwap;
                    SetCurrentShapePointsList();
                }
                return true;
            }
        }
        return false;
    }

    public void CreateShape()
    {
        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            List<Vector2> pointValues = new List<Vector2>();
            pointValues.Add(Camera.main.ScreenToWorldPoint(new Vector2(Screen.width / 2, Screen.height / 2)));
            pointValues.Add(new Vector2(pointValues[0].x + 2, pointValues[0].y));
            pointValues.Add(new Vector2(pointValues[0].x + 2, pointValues[0].y + 2));
            pointValues.Add(new Vector2(pointValues[0].x, pointValues[0].y + 2));
            string sPointValues = "";
            foreach(Vector2 v in pointValues) { sPointValues += v.x.ToString() + "|" + v.y.ToString() + "|"; }
            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"scr{sPointValues}"));
        }
        else
        {
            bool errorFound = false;
            if (currentShape != null)
            {
                for (int i = 0; i < dragPoints.Count; i++)
                {
                    for (int c = i + 1; c < dragPoints.Count; c++)
                    {
                        if (Vector2.Distance(dragPoints[i].transform.position, dragPoints[c].transform.position) < 0.001f)
                        {
                            errorFound = true;
                            warningText.text = "Deleted Previous Shape: \n Two or more points overlapping";
                            WarningTextChanged();
                            break;
                        }
                    }
                }
                List<Edge> edges = new List<Edge>();
                for (int i = 0; i < dragPoints.Count; i++)
                {
                    if (i != dragPoints.Count - 1)
                    {
                        edges.Add(new Edge(dragPoints[i].transform.position, dragPoints[i + 1].transform.position));
                    }
                    else
                    {
                        edges.Add(new Edge(dragPoints[i].transform.position, dragPoints[0].transform.position));
                    }
                }
                for (int i = 0; i < edges.Count; i++)
                {
                    for (int c = i + 1; c < edges.Count; c++)
                    {
                        if (edges[i].endPoint != edges[c].startPoint && edges[i].startPoint != edges[c].endPoint)
                        {
                            if (edges[i].EdgeIntersection(edges[c]))
                            {
                                errorFound = true;
                                warningText.text = "Deleted Previous Shape: \n Two or more lines overlapping";
                                WarningTextChanged();
                                break;
                            }
                        }
                    }
                }
                if (currentShape.GetComponent<ComplexShape>().points.Count < 3)
                {
                    errorFound = true;
                    warningText.text = "Deleted Previous Shape: \n Less than three points";
                    WarningTextChanged();
                }
                if (!Manager.Instance.Shapes.Contains(currentShape.GetComponent<ComplexShape>()))
                {
                    errorFound = true;
                    warningText.text = "Deleted Previous Shape: \n Not enough points";
                    WarningTextChanged();
                }
            }
            else
            {
                warningText.text = "Warnings";
                WarningTextChanged();
            }

            if (errorFound)
            {
                DeleteCurrentData(true, true);
            }
            else
            {
                StorePermanentLines();
                DeleteCurrentData(true, false);
            }

            GameObject shadowShape = new GameObject("Shadow");
            shadowShape.AddComponent<ComplexShape>();
            shadowShape.GetComponent<ComplexShape>().points.Add(Camera.main.ScreenToWorldPoint(new Vector2(Screen.width / 2, Screen.height / 2)));
            shadowShape.GetComponent<ComplexShape>().points.Add(new Vector2(shadowShape.GetComponent<ComplexShape>().points[0].x + 2, shadowShape.GetComponent<ComplexShape>().points[0].y));
            shadowShape.GetComponent<ComplexShape>().points.Add(new Vector2(shadowShape.GetComponent<ComplexShape>().points[0].x + 2, shadowShape.GetComponent<ComplexShape>().points[0].y + 2));
            shadowShape.GetComponent<ComplexShape>().points.Add(new Vector2(shadowShape.GetComponent<ComplexShape>().points[0].x, shadowShape.GetComponent<ComplexShape>().points[0].y + 2));

            currentShape = shadowShape;

            currentShape.GetComponent<ComplexShape>().functional = false;

            SetCurrentShapePointsList();

            currentShape.GetComponent<ComplexShape>().OnCompileEdgeList += CurrentShapeUpdateHandler;

            Manager.Instance.AssignShapes();

            ResetPermanentLines();
        }
    }

    public void ServerCreateShape(List<Vector2> pointValues, bool closeAfterFinish)
    {
        GameObject shadowShape = new GameObject("Shadow");
        shadowShape.AddComponent<ComplexShape>();

        currentShape = shadowShape;
        currentShape.GetComponent<ComplexShape>().points = new List<Vector2>();
        foreach (Vector2 v in pointValues) { currentShape.GetComponent<ComplexShape>().points.Add(new Vector2(v.x, v.y)); }

        currentShape.GetComponent<ComplexShape>().functional = false;

        SetCurrentShapePointsList();

        currentShape.GetComponent<ComplexShape>().OnCompileEdgeList += CurrentShapeUpdateHandler;

        ResetPermanentLines();

        ServerCheckForDestroy();

        Manager.Instance.FindElementsToManage();

        if (closeAfterFinish) { Manager.Instance.CloseShadowMenu(); }
    }

    public void DeleteShape()
    {
        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection && currentShape != null)
        {
            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"svc{Manager.Instance.Shapes.IndexOf(currentShape.GetComponent<ComplexShape>())}|{1}|X|{0}"));
        }
        else
        {
            if (currentShape != null)
            {
                Manager.Instance.Shapes.Remove(currentShape.GetComponent<Shape>());
                DeleteCurrentData(true, true);
                Manager.Instance.AssignShapes();
                Manager.Instance.BuildAllLights();

                ResetPermanentLines();
            }
            currentShape = null;
            deletePrepare = true;
        }
    }

    public void AddPoint()
    {
        if (currentShape != null)
        {
            currentShape.GetComponent<ComplexShape>().functional = false;

            #region  Find best place to add the point
            Vector2 placement = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width / 2, Screen.height / 2));
            List<Edge> edgesToCheck = new List<Edge>();

            for (int i = 0; i < currentShape.GetComponent<ComplexShape>().points.Count; i++)
            {
                if (i < currentShape.GetComponent<ComplexShape>().points.Count - 1)
                {
                    edgesToCheck.Add(new Edge(currentShape.GetComponent<ComplexShape>().points[i], currentShape.GetComponent<ComplexShape>().points[i + 1]));
                }
                else
                {
                    edgesToCheck.Add(new Edge(currentShape.GetComponent<ComplexShape>().points[i], currentShape.GetComponent<ComplexShape>().points[0]));
                }
            }
            List<Vector2> middlePoints = new List<Vector2>();
            foreach (Edge e in edgesToCheck)
            {
                middlePoints.Add(e.startPoint + ((e.endPoint - e.startPoint) / 2.0f));
            }

            List<Vector2> visiblePoints = new List<Vector2>();
            List<int> visiblePointsIndeces = new List<int>();
            for (int i = 0; i < middlePoints.Count; i++)
            {
                Edge edgeToCheck = new Edge(placement, middlePoints[i]);
                bool visible = true;
                foreach (Edge e in edgesToCheck)
                {
                    if (!e.PointIntersection(middlePoints[i]))
                    {
                        if (e.EdgeIntersection(edgeToCheck))
                        {
                            visible = false;
                            break;
                        }
                    }
                }
                if (visible)
                {
                    visiblePoints.Add(middlePoints[i]);
                    visiblePointsIndeces.Add(i);
                }
            }

            if (visiblePoints.Count > 0)
            {
                Vector2 closestPoint = visiblePoints[0];

                for (int i = 1; i < visiblePoints.Count; i++)
                {
                    if (Vector2.Distance(visiblePoints[i], placement) < Vector2.Distance(closestPoint, placement))
                    {
                        closestPoint = visiblePoints[i];
                    }
                }
                if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
                {
                    currentShape.GetComponent<ComplexShape>().GenerateBoundingSphere();
                    List<Vector2> points = new List<Vector2>();
                    foreach (Vector2 v in currentShape.GetComponent<ComplexShape>().points) { points.Add(new Vector2(v.x, v.y)); }
                    points.Insert(middlePoints.IndexOf(closestPoint) + 1, Camera.main.ScreenToWorldPoint(new Vector2(Screen.width / 2, Screen.height / 2)));
                    string pointValues = "";
                    foreach (Vector2 v in points)
                    {
                        pointValues += v.x.ToString() + "|" + v.y.ToString() + "|";
                    }
                    NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"svc{Manager.Instance.Shapes.IndexOf(currentShape.GetComponent<ComplexShape>())}|{0}|{pointValues}{(currentShape.GetComponent<ComplexShape>().functional ? 1 : 0)}"));
                }
                else
                {
                    currentShape.GetComponent<ComplexShape>().points.Insert(middlePoints.IndexOf(closestPoint) + 1, Camera.main.ScreenToWorldPoint(new Vector2(Screen.width / 2, Screen.height / 2)));
                    SetCurrentShapePointsList();
                    Scroll();
                    warningText.text = "Point Added";
                    WarningTextChanged();
                    ResetPermanentLines();
                }
            }
            else
            {
                if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
                {
                    currentShape.GetComponent<ComplexShape>().GenerateBoundingSphere();
                    List<Vector2> points = new List<Vector2>();
                    foreach (Vector2 v in currentShape.GetComponent<ComplexShape>().points) { points.Add(new Vector2(v.x, v.y)); }
                    points.Add(Camera.main.ScreenToWorldPoint(new Vector2(Screen.width / 2, Screen.height / 2)));
                    string pointValues = "";
                    foreach (Vector2 v in points)
                    {
                        pointValues += v.x.ToString() + "|" + v.y.ToString() + "|";
                    }
                    NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"svc{Manager.Instance.Shapes.IndexOf(currentShape.GetComponent<ComplexShape>())}|{0}|{pointValues}{(currentShape.GetComponent<ComplexShape>().functional ? 1 : 0)}"));
                }
                else
                {
                    currentShape.GetComponent<ComplexShape>().points.Add(Camera.main.ScreenToWorldPoint(new Vector2(Screen.width / 2, Screen.height / 2)));
                    SetCurrentShapePointsList();
                    Scroll();
                    warningText.text = "Point Added";
                    WarningTextChanged();
                    ResetPermanentLines();
                }
            }
            #endregion
        }
    }

    public void PointAltered(float input, int index, bool xValue)
    {
        currentShape.GetComponent<ComplexShape>().functional = false;
        if (xValue)
        {
            if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
            {
                currentShape.GetComponent<ComplexShape>().GenerateBoundingSphere();
                List<Vector2> points = new List<Vector2>();
                foreach (Vector2 v in currentShape.GetComponent<ComplexShape>().points) { points.Add(new Vector2(v.x, v.y)); }
                points[index] = new Vector2(input, currentShape.GetComponent<ComplexShape>().points[index].y);
                string pointValues = "";
                foreach (Vector2 v in points)
                {
                    pointValues += v.x.ToString() + "|" + v.y.ToString() + "|";
                }
                NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"svc{Manager.Instance.Shapes.IndexOf(currentShape.GetComponent<ComplexShape>())}|{0}|{pointValues}{(currentShape.GetComponent<ComplexShape>().functional ? 1 : 0)}"));
            }
            else
            {
                currentShape.GetComponent<ComplexShape>().points[index] = new Vector2(input, currentShape.GetComponent<ComplexShape>().points[index].y);
                SetCurrentShapePointsList();
                warningText.text = "Point Changed";
                WarningTextChanged();
                ResetPermanentLines();
            }
        }
        else
        {
            if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
            {
                currentShape.GetComponent<ComplexShape>().GenerateBoundingSphere();
                List<Vector2> points = new List<Vector2>();
                foreach (Vector2 v in currentShape.GetComponent<ComplexShape>().points) { points.Add(new Vector2(v.x, v.y)); }
                points[index] = new Vector2(currentShape.GetComponent<ComplexShape>().points[index].x, input);
                string pointValues = "";
                foreach (Vector2 v in points)
                {
                    pointValues += v.x.ToString() + "|" + v.y.ToString() + "|";
                }
                NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"svc{Manager.Instance.Shapes.IndexOf(currentShape.GetComponent<ComplexShape>())}|{0}|{pointValues}{(currentShape.GetComponent<ComplexShape>().functional ? 1 : 0)}"));
            }
            else
            {
                currentShape.GetComponent<ComplexShape>().points[index] = new Vector2(currentShape.GetComponent<ComplexShape>().points[index].x, input);
                SetCurrentShapePointsList();
                warningText.text = "Point Changed";
                WarningTextChanged();
                ResetPermanentLines();
            }
        }
    }

    public void DragPoint()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            dragState = DragState.Point;
            if (gridSnap)
            {
                bool origErrorX = false;
                if (Camera.main.ScreenToWorldPoint(Input.mousePosition).x >= -0.0f && Camera.main.ScreenToWorldPoint(Input.mousePosition).x <= 1.0f) { origErrorX = true; }
                bool origErrorY = false;
                if (Camera.main.ScreenToWorldPoint(Input.mousePosition).y >= 0.0f && Camera.main.ScreenToWorldPoint(Input.mousePosition).y <= 1.0f) { origErrorY = true; }
                currentDragPoint.transform.position = new Vector3Int((int)Camera.main.ScreenToWorldPoint(Input.mousePosition).x, (int)(Camera.main.ScreenToWorldPoint(Input.mousePosition).y), -8);

                if (!origErrorX)
                {
                    if (currentDragPoint.transform.position.x <= 0)
                    {
                        if (Camera.main.ScreenToWorldPoint(Input.mousePosition).x.ToString().Split(new char[1] { '.' }).Length > 1)
                        {
                            if (int.Parse(Camera.main.ScreenToWorldPoint(Input.mousePosition).x.ToString().Split(new char[1] { '.' })[1].Substring(0, 1)) <= 5)
                            {
                                currentDragPoint.transform.position += new Vector3(1, 0);
                            }
                        }
                    }
                    else
                    {
                        if (Camera.main.ScreenToWorldPoint(Input.mousePosition).x.ToString().Split(new char[1] { '.' }).Length > 1)
                        {
                            if (int.Parse(Camera.main.ScreenToWorldPoint(Input.mousePosition).x.ToString().Split(new char[1] { '.' })[1].Substring(0, 1)) >= 5)
                            {
                                currentDragPoint.transform.position += new Vector3(1, 0);
                            }
                        }
                    }
                }
                if (!origErrorY)
                {
                    if (currentDragPoint.transform.position.y <= 0)
                    {
                        if (Camera.main.ScreenToWorldPoint(Input.mousePosition).y.ToString().Split(new char[1] { '.' }).Length > 1)
                        {
                            if (int.Parse(Camera.main.ScreenToWorldPoint(Input.mousePosition).y.ToString().Split(new char[1] { '.' })[1].Substring(0, 1)) <= 5)
                            {
                                currentDragPoint.transform.position += new Vector3(0, 1);
                            }
                        }
                    }
                    else
                    {
                        if (Camera.main.ScreenToWorldPoint(Input.mousePosition).y.ToString().Split(new char[1] { '.' }).Length > 1)
                        {
                            if (int.Parse(Camera.main.ScreenToWorldPoint(Input.mousePosition).y.ToString().Split(new char[1] { '.' })[1].Substring(0, 1)) >= 5)
                            {
                                currentDragPoint.transform.position += new Vector3(0, 1);
                            }
                        }
                    }
                }

                currentDragPoint.transform.position -= new Vector3(1, 1);

                if (currentDragPoint.transform.position.x > 0) { currentDragPoint.transform.position += new Vector3(1.0f, 0.0f, 0.0f); }
                if (currentDragPoint.transform.position.y > 0) { currentDragPoint.transform.position += new Vector3(0.0f, 1.0f, 0.0f); }

                if (origErrorX)
                {
                    currentDragPoint.transform.position += new Vector3(1.0f, 0.0f, 0.0f);
                    if (currentDragPoint.transform.position.x <= 1)
                    {
                        if (Camera.main.ScreenToWorldPoint(Input.mousePosition).x.ToString().Split(new char[1] { '.' }).Length > 1)
                        {
                            if (int.Parse(Camera.main.ScreenToWorldPoint(Input.mousePosition).x.ToString().Split(new char[1] { '.' })[1].Substring(0, 1)) >= 5)
                            {
                                currentDragPoint.transform.position += new Vector3(1, 0);
                            }
                        }
                    }
                }
                if (origErrorY)
                {
                    currentDragPoint.transform.position += new Vector3(0.0f, 1.0f, 0.0f);
                    if (currentDragPoint.transform.position.y <= 1)
                    {
                        if (Camera.main.ScreenToWorldPoint(Input.mousePosition).y.ToString().Split(new char[1] { '.' }).Length > 1)
                        {
                            if (int.Parse(Camera.main.ScreenToWorldPoint(Input.mousePosition).y.ToString().Split(new char[1] { '.' })[1].Substring(0, 1)) >= 5)
                            {
                                currentDragPoint.transform.position += new Vector3(0, 1);
                            }
                        }
                    }
                }

                if (Camera.main.ScreenToWorldPoint(Input.mousePosition).x > 1 && Camera.main.ScreenToWorldPoint(Input.mousePosition).x <= 1.5f)
                {
                    currentDragPoint.transform.position += new Vector3(1, 0);
                }
                if (Camera.main.ScreenToWorldPoint(Input.mousePosition).y > 1 && Camera.main.ScreenToWorldPoint(Input.mousePosition).y <= 1.5f)
                {
                    currentDragPoint.transform.position += new Vector3(0, 1);
                }
            }
            else
            {
                currentDragPoint.transform.position = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, -8);
            }

            if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
            {
                currentShape.GetComponent<ComplexShape>().GenerateBoundingSphere();
                List<Vector2> points = new List<Vector2>();
                foreach (Vector2 v in currentShape.GetComponent<ComplexShape>().points) { points.Add(new Vector2(v.x, v.y)); }
                points[dragPoints.IndexOf(currentDragPoint)] = currentDragPoint.transform.position;
                string pointValues = "";
                foreach (Vector2 v in points)
                {
                    pointValues += v.x.ToString() + "|" + v.y.ToString() + "|";
                }
                NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"svc{Manager.Instance.Shapes.IndexOf(currentShape.GetComponent<ComplexShape>())}|{0}|{pointValues}{(currentShape.GetComponent<ComplexShape>().functional ? 1 : 0)}"));
            }
            else
            {
                currentShape.GetComponent<ComplexShape>().points[dragPoints.IndexOf(currentDragPoint)] = currentDragPoint.transform.position;
                if (currentShape.GetComponent<ComplexShape>().functional)
                {
                    ToggleFunctionality();
                }
                SetCurrentShapePointsList();
                ResetPermanentLines();
            }
        }
    }

    public void ToggleGridSnap()
    {
        if(gridSnap)
        {
            gridSnapToggle.GetComponent<Image>().color = new Color(169.0f / 255.0f, 99.0f / 255.0f, 98.0f / 255.0f);
        }
        else
        {
            gridSnapToggle.GetComponent<Image>().color = new Color(158.0f / 255.0f, 255.0f / 255.0f, 160.0f / 255.0f);
        }
        gridSnap = !gridSnap;
    }

    public void ToggleFunctionality()
    {
        if(currentShape != null)
        {
            if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
            {
                NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"svc{Manager.Instance.Shapes.IndexOf(currentShape.GetComponent<ComplexShape>())}|{0}|X|{(currentShape.GetComponent<ComplexShape>().functional ? 0 : 1)}"));
            }
            else
            {
                if (!currentShape.GetComponent<ComplexShape>().functional)
                {
                    functionalityToggle.GetComponent<Image>().color = new Color(158.0f / 255.0f, 255.0f / 255.0f, 160.0f / 255.0f);
                }
                else
                {
                    functionalityToggle.GetComponent<Image>().color = new Color(169.0f / 255.0f, 99.0f / 255.0f, 98.0f / 255.0f);
                }
                currentShape.GetComponent<ComplexShape>().functional = !currentShape.GetComponent<ComplexShape>().functional;
                Manager.Instance.BuildAllLights();
            }
        }
    }

    public void ServerToggleFunctionality()
    {
        if (currentShape != null)
        {
            if (!currentShape.GetComponent<ComplexShape>().functional)
            {
                functionalityToggle.GetComponent<Image>().color = new Color(158.0f / 255.0f, 255.0f / 255.0f, 160.0f / 255.0f);
            }
            else
            {
                functionalityToggle.GetComponent<Image>().color = new Color(169.0f / 255.0f, 99.0f / 255.0f, 98.0f / 255.0f);
            }
            currentShape.GetComponent<ComplexShape>().functional = !currentShape.GetComponent<ComplexShape>().functional;
            Manager.Instance.BuildAllLights();
        }
    }

    public void DeletePoint()
    {
        if (dragPoints.Count > 1)
        {
            if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
            {
                currentShape.GetComponent<ComplexShape>().GenerateBoundingSphere();
                List<Vector2> points = new List<Vector2>();
                foreach (Vector2 v in currentShape.GetComponent<ComplexShape>().points) { points.Add(new Vector2(v.x, v.y)); }
                int indexToDelete = deleteButtons.IndexOf(hoveredButton);
                points.RemoveAt(indexToDelete);
                string pointValues = "";
                foreach (Vector2 v in points)
                {
                    pointValues += v.x.ToString() + "|" + v.y.ToString() + "|";
                }
                NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"svc{Manager.Instance.Shapes.IndexOf(currentShape.GetComponent<ComplexShape>())}|{0}|{pointValues}{(currentShape.GetComponent<ComplexShape>().functional ? 1 : 0)}"));
            }
            else
            {
                currentShape.GetComponent<ComplexShape>().functional = false;

                int indexToDelete = deleteButtons.IndexOf(hoveredButton);

                currentShape.GetComponent<ComplexShape>().points.RemoveAt(indexToDelete);

                SetCurrentShapePointsList();

                Scroll();

                warningText.text = "Point Deleted";
                WarningTextChanged();

                ResetPermanentLines();
            }
        }
    }

    public void RightButtonPressed()
    {
        if(currentShape == null && Manager.Instance.Shapes.Count != 0)
        {
            currentShape = Manager.Instance.Shapes[0].gameObject;
        }
        else if (Manager.Instance.Shapes.Count > 1 && currentShape != null)
        {
            //Check current shape
            bool errorFound = false;
            for (int i = 0; i < dragPoints.Count; i++)
            {
                for (int c = i + 1; c < dragPoints.Count; c++)
                {
                    if (Vector2.Distance(dragPoints[i].transform.position, dragPoints[c].transform.position) < 0.001f)
                    {
                        errorFound = true;
                        warningText.text = "Deleted Previous Shape: \n Two or more points overlapping";
                        WarningTextChanged();
                        break;
                    }
                }
            }
            List<Edge> edges = new List<Edge>();
            for (int i = 0; i < dragPoints.Count; i++)
            {
                if (i != dragPoints.Count - 1)
                {
                    edges.Add(new Edge(dragPoints[i].transform.position, dragPoints[i + 1].transform.position));
                }
                else
                {
                    edges.Add(new Edge(dragPoints[i].transform.position, dragPoints[0].transform.position));
                }
            }
            for (int i = 0; i < edges.Count; i++)
            {
                for (int c = i + 1; c < edges.Count; c++)
                {
                    if (edges[i].endPoint != edges[c].startPoint && edges[i].startPoint != edges[c].endPoint)
                    {
                        if (edges[i].EdgeIntersection(edges[c]))
                        {
                            errorFound = true;
                            warningText.text = "Deleted Previous Shape: \n Two or more lines overlapping";
                            WarningTextChanged();
                            break;
                        }
                    }
                }
            }
            if (currentShape.GetComponent<ComplexShape>().points.Count < 3)
            {
                errorFound = true;
                warningText.text = "Deleted Previous Shape: \n Less than three points";
                WarningTextChanged();
            }
            if (!Manager.Instance.Shapes.Contains(currentShape.GetComponent<ComplexShape>()))
            {
                errorFound = true;
                warningText.text = "Deleted Previous Shape: \n Not enough points";
                WarningTextChanged();
            }
            if (errorFound)
            {
                DeleteCurrentData(true, true);
            }

            if (Manager.Instance.Shapes.IndexOf(currentShape.GetComponent<ComplexShape>()) < Manager.Instance.Shapes.Count - 1)
            {
                currentShape = Manager.Instance.Shapes[Manager.Instance.Shapes.IndexOf(currentShape.GetComponent<ComplexShape>()) + 1].gameObject;
            }
            else
            {
                currentShape = Manager.Instance.Shapes[0].gameObject;
            }
            SetCurrentShapePointsList();
        }
        ResetPermanentLines();
    }

    public void LeftButtonPressed()
    {
        if (currentShape == null && Manager.Instance.Shapes.Count != 0)
        {
            currentShape = Manager.Instance.Shapes[0].gameObject;
        }
        else if (Manager.Instance.Shapes.Count > 1 && currentShape != null)
        {
            //Check current shape
            bool errorFound = false;
            for (int i = 0; i < dragPoints.Count; i++)
            {
                for (int c = i + 1; c < dragPoints.Count; c++)
                {
                    if (Vector2.Distance(dragPoints[i].transform.position, dragPoints[c].transform.position) < 0.001f)
                    {
                        errorFound = true;
                        warningText.text = "Deleted Previous Shape: \n Two or more points overlapping";
                        WarningTextChanged();
                        break;
                    }
                }
            }
            List<Edge> edges = new List<Edge>();
            for (int i = 0; i < dragPoints.Count; i++)
            {
                if (i != dragPoints.Count - 1)
                {
                    edges.Add(new Edge(dragPoints[i].transform.position, dragPoints[i + 1].transform.position));
                }
                else
                {
                    edges.Add(new Edge(dragPoints[i].transform.position, dragPoints[0].transform.position));
                }
            }
            for (int i = 0; i < edges.Count; i++)
            {
                for (int c = i + 1; c < edges.Count; c++)
                {
                    if (edges[i].endPoint != edges[c].startPoint && edges[i].startPoint != edges[c].endPoint)
                    {
                        if (edges[i].EdgeIntersection(edges[c]))
                        {
                            errorFound = true;
                            warningText.text = "Deleted Previous Shape: \n Two or more lines overlapping";
                            WarningTextChanged();
                            break;
                        }
                    }
                }
            }
            if (currentShape.GetComponent<ComplexShape>().points.Count < 3)
            {
                errorFound = true;
                warningText.text = "Deleted Previous Shape: \n Less than three points";
                WarningTextChanged();
            }
            if (!Manager.Instance.Shapes.Contains(currentShape.GetComponent<ComplexShape>()))
            {
                errorFound = true;
                warningText.text = "Deleted Previous Shape: \n Not enough points";
                WarningTextChanged();
            }
            if(errorFound)
            {
                DeleteCurrentData(true, true);
            }

            if (Manager.Instance.Shapes.IndexOf(currentShape.GetComponent<ComplexShape>()) > 0)
            {
                currentShape = Manager.Instance.Shapes[Manager.Instance.Shapes.IndexOf(currentShape.GetComponent<ComplexShape>()) - 1].gameObject;
            }
            else
            {
                currentShape = Manager.Instance.Shapes[Manager.Instance.Shapes.Count - 1].gameObject;
            }
            SetCurrentShapePointsList();
        }
        ResetPermanentLines();
    }

    public void Scroll()
    {
        int scrollValue = 0;
        if (xPoints.Count > 6)
        {
            scrollValue = (int)((scrollBar.value * (xPoints.Count - 6)) * 37);
        }

        formattingPanelA.GetComponent<VerticalLayoutGroup>().padding.top = -scrollValue;
        formattingPanelA.GetComponent<VerticalLayoutGroup>().enabled = false;
        formattingPanelA.GetComponent<VerticalLayoutGroup>().enabled = true;
        formattingPanelB.GetComponent<VerticalLayoutGroup>().padding.top = -scrollValue;
        formattingPanelB.GetComponent<VerticalLayoutGroup>().enabled = false;
        formattingPanelB.GetComponent<VerticalLayoutGroup>().enabled = true;
        formattingPanelC.GetComponent<VerticalLayoutGroup>().padding.top = -scrollValue;
        formattingPanelC.GetComponent<VerticalLayoutGroup>().enabled = false;
        formattingPanelC.GetComponent<VerticalLayoutGroup>().enabled = true;
    }

    public void DeleteCurrentData(bool deleteCurrentReference, bool destroyCurrentShape)
    {
        if (currentShape != null)
        {
            List<InputField> lif = new List<InputField>();
            lif.AddRange(formattingPanelA.GetComponentsInChildren<InputField>());
            foreach (InputField f in lif) { Destroy(f.gameObject); }

            lif = new List<InputField>();
            lif.AddRange(formattingPanelB.GetComponentsInChildren<InputField>());
            foreach (InputField f in lif) { Destroy(f.gameObject); }

            List<Button> lb = new List<Button>();
            lb.AddRange(formattingPanelC.GetComponentsInChildren<Button>());
            foreach (Button b in lb) { Destroy(b.gameObject); }

            foreach(GameObject g in dragPoints)
            {
                Destroy(g);
            }

            foreach (GameObject g in pointLines)
            {
                Destroy(g);
            }

            if(destroyCurrentShape)
            {
                DestroyImmediate(currentShape);
            }

            if (deleteCurrentReference)
            {
                currentShape = null;
            }

            xPoints = new List<GameObject>();
            yPoints = new List<GameObject>();
            deleteButtons = new List<GameObject>();
            dragPoints = new List<GameObject>();
            pointLines = new List<GameObject>();
        }
    }

    public void SetCurrentShapePointsList()
    {
        if (currentShape != null)
        {
            if (!pauseUpdates)
            {
                DeleteCurrentData(false, false);

                for (int i = 0; i < currentShape.GetComponent<ComplexShape>().points.Count; i++)
                {
                    Vector2 v = currentShape.GetComponent<ComplexShape>().points[i];

                    xPoints.Add(Instantiate(pointInformationPrefab, formattingPanelA.transform));
                    xPoints[xPoints.Count - 1].GetComponentInChildren<InputField>().text = v.x.ToString();
                    xPoints[xPoints.Count - 1].GetComponent<Image>().color = Color.HSVToRGB((i % 10.0f) / 10.0f, 0.18f, 1);

                    yPoints.Add(Instantiate(pointInformationPrefab, formattingPanelB.transform));
                    yPoints[yPoints.Count - 1].GetComponentInChildren<InputField>().text = v.y.ToString();
                    yPoints[yPoints.Count - 1].GetComponent<Image>().color = Color.HSVToRGB((i % 10.0f) / 10.0f, 0.18f, 1);

                    GameObject deleteButton = Instantiate(deletePointButtonPrefab, formattingPanelC.transform);
                    deleteButton.GetComponent<Button>().onClick.AddListener(DeletePoint);
                    deleteButtons.Add(deleteButton);

                    if (i != currentShape.GetComponent<ComplexShape>().points.Count - 1)
                    {
                        GameObject pointLine = Instantiate(pointLinePrefab);
                        pointLine.transform.position = v;
                        SetupPointLine(pointLine, v, currentShape.GetComponent<ComplexShape>().points[i + 1]);
                        pointLines.Add(pointLine);
                    }
                    else
                    {
                        if (i > 0)
                        {
                            GameObject pointLine = Instantiate(pointLinePrefab);
                            pointLine.transform.position = v;
                            SetupPointLine(pointLine, v, currentShape.GetComponent<ComplexShape>().points[0]);
                            pointLines.Add(pointLine);
                        }
                    }

                    GameObject dragPoint = Instantiate(dragPointPrefab);
                    dragPoint.GetComponent<DragPoint>().linkedTool = gameObject;
                    dragPoint.GetComponent<DragPoint>().typeShadow = true;
                    dragPoint.transform.position = new Vector3(v.x, v.y, -8);
                    dragPoint.GetComponent<SpriteRenderer>().color = Color.HSVToRGB((i % 10.0f) / 10.0f, 0.45f, 1);
                    dragPoints.Add(dragPoint);
                }
            }
            else
            {
                foreach (GameObject g in pointLines)
                {
                    Destroy(g);
                }
                pointLines = new List<GameObject>();

                for (int i = 0; i < currentShape.GetComponent<ComplexShape>().points.Count; i++)
                {
                    Vector2 v = currentShape.GetComponent<ComplexShape>().points[i];

                    xPoints[i].GetComponentInChildren<InputField>().text = v.x.ToString();

                    yPoints[i].GetComponentInChildren<InputField>().text = v.y.ToString();

                    if (i != currentShape.GetComponent<ComplexShape>().points.Count - 1)
                    {
                        GameObject pointLine = Instantiate(pointLinePrefab);
                        pointLine.transform.position = v;
                        SetupPointLine(pointLine, v, currentShape.GetComponent<ComplexShape>().points[i + 1]);
                        pointLines.Add(pointLine);
                    }
                    else
                    {
                        if (i > 0)
                        {
                            GameObject pointLine = Instantiate(pointLinePrefab);
                            pointLine.transform.position = v;
                            SetupPointLine(pointLine, v, currentShape.GetComponent<ComplexShape>().points[0]);
                            pointLines.Add(pointLine);
                        }
                    }
                }
            }
            currentShape.GetComponent<ComplexShape>().GenerateBoundingSphere();
            if (currentShape.GetComponent<ComplexShape>().functional)
            {
                functionalityToggle.GetComponent<Image>().color = new Color(158.0f / 255.0f, 255.0f / 255.0f, 160.0f / 255.0f);
            }
            else
            {
                functionalityToggle.GetComponent<Image>().color = new Color(169.0f / 255.0f, 99.0f / 255.0f, 98.0f / 255.0f);
            }
        }
    }

    public void CurrentShapeUpdateHandler(object shape, EventArgs args)
    {
        SetCurrentShapePointsList();
    }

    public void SetupPointLine(GameObject line, Vector2 start, Vector2 end)
    {
        float deg = Mathf.Rad2Deg * Mathf.Atan2((end - start).y, (end - start).x);
        line.transform.rotation = Quaternion.Euler(0, 0, deg - 90);

        line.transform.position = start + ((end - start) / 2.0f);

        line.transform.localScale = new Vector3(0.055f, Vector2.Distance(start, end), 1);
    }

    public void SetupPermanentPointLine(GameObject line, Vector2 start, Vector2 end)
    {
        float deg = Mathf.Rad2Deg * Mathf.Atan2((end - start).y, (end - start).x);
        line.transform.rotation = Quaternion.Euler(0, 0, deg - 90);

        line.transform.position = start + ((end - start) / 2.0f);

        line.transform.localScale = new Vector3(0.023f, Vector2.Distance(start, end), 1);
    }

    public void CloseShadowTool()
    {
        foreach (Shape s in Manager.Instance.Shapes)
        {
            currentShape = s.gameObject;
            currentShape.GetComponent<ComplexShape>().OnCompileEdgeList -= CurrentShapeUpdateHandler;
        }
        List<GameObject> shapesToRemove = new List<GameObject>();
        foreach(Shape s in Manager.Instance.Shapes)
        {
            if(s.points.Count < 3) { shapesToRemove.Add(s.gameObject); }
        }
        foreach(GameObject s in shapesToRemove)
        {
            Manager.Instance.Shapes.Remove(s.GetComponent<Shape>());
            Destroy(s.gameObject);
        }
        foreach (GameObject g in storedPointLines)
        {
            Destroy(g);
        }
        storedPointLines = new List<GameObject>();
        DeleteCurrentData(true, false);
    }

    public void OpenShadowTool()
    {
        foreach (Shape s in Manager.Instance.Shapes)
        {
            currentShape = s.gameObject;
        }

        gridSnap = false;

        gridSnapToggle.GetComponent<Image>().color = new Color(169.0f / 255.0f, 99.0f / 255.0f, 98.0f / 255.0f);

        if (currentShape != null)
        {
            if (currentShape.GetComponent<ComplexShape>().functional)
            {
                functionalityToggle.GetComponent<Image>().color = new Color(158.0f / 255.0f, 255.0f / 255.0f, 160.0f / 255.0f);
            }
            else
            {
                functionalityToggle.GetComponent<Image>().color = new Color(169.0f / 255.0f, 99.0f / 255.0f, 98.0f / 255.0f);
            }
        }

        ResetPermanentLines();


    }

    public void CloseButtonPressed()
    {
        Manager.Instance.OpenShadowMenu();
    }

    public void StorePermanentLines()
    {
        if(currentShape != null)
        {
            for (int i = 0; i < currentShape.GetComponent<ComplexShape>().points.Count; i++)
            {
                Vector2 v = currentShape.GetComponent<ComplexShape>().points[i];

                if (i != currentShape.GetComponent<ComplexShape>().points.Count - 1)
                {
                    GameObject pointLine = Instantiate(permanentPointLinePrefab);
                    pointLine.transform.position = v;
                    SetupPermanentPointLine(pointLine, v, currentShape.GetComponent<ComplexShape>().points[i + 1]);
                    storedPointLines.Add(pointLine);
                }
                else
                {
                    if (i > 0)
                    {
                        GameObject pointLine = Instantiate(permanentPointLinePrefab);
                        pointLine.transform.position = v;
                        SetupPermanentPointLine(pointLine, v, currentShape.GetComponent<ComplexShape>().points[0]);
                        storedPointLines.Add(pointLine);
                    }
                }
            }
        }
    }

    public IEnumerator ResetPermanentLinesAtFrameEnd()
    {
        yield return new WaitForEndOfFrame();

        foreach(GameObject g in storedPointLines)
        {
            Destroy(g);
        }
        storedPointLines = new List<GameObject>();
        List<Shape> toDelete = new List<Shape>();
        foreach(Shape s in Manager.Instance.Shapes)
        {
            if (s == null)
            {
                toDelete.Add(s);
            }
            else
            {
                currentShape = s.gameObject;
                StorePermanentLines();
            }
        }
        foreach(Shape s in toDelete)
        {
            Manager.Instance.Shapes.Remove(s);
        }

        Manager.Instance.FindElementsToManage();

        currentShape = null;
    }

    public void ResetPermanentLines()
    {
        GameObject sCurrentShape = null;
        if(currentShape != null)
        {
            sCurrentShape = currentShape;
        }
        foreach (GameObject g in storedPointLines)
        {
            Destroy(g);
        }
        storedPointLines = new List<GameObject>();
        List<Shape> toDelete = new List<Shape>();
        foreach (Shape s in Manager.Instance.Shapes)
        {
            if (s == null)
            {
                toDelete.Add(s);
            }
            else
            {
                currentShape = s.gameObject;
                StorePermanentLines();
            }
        }
        foreach (Shape s in toDelete)
        {
            Manager.Instance.Shapes.Remove(s);
        }

        if (sCurrentShape != null)
        {
            currentShape = sCurrentShape;
            SetCurrentShapePointsList();
        }
    }

    public void WarningTextChanged()
    {
        warningText.GetComponentInParent<Image>().color = new Color(0.89f, 0.76f, 0.89f);
        StartCoroutine("WarningTextColorFade");
    }

    public IEnumerator WarningTextColorFade()
    {
        yield return new WaitForSecondsRealtime(0.085f);

        if(warningText.GetComponentInParent<Image>().color.r < 1)
        {
            warningText.GetComponentInParent<Image>().color = new Color(warningText.GetComponentInParent<Image>().color.r + 0.02f, warningText.GetComponentInParent<Image>().color.g, warningText.GetComponentInParent<Image>().color.b);
        }

        if (warningText.GetComponentInParent<Image>().color.b < 1)
        {
            warningText.GetComponentInParent<Image>().color = new Color(warningText.GetComponentInParent<Image>().color.r, warningText.GetComponentInParent<Image>().color.g, warningText.GetComponentInParent<Image>().color.b + 0.02f);
        }

        if (warningText.GetComponentInParent<Image>().color.g < 1)
        {
            warningText.GetComponentInParent<Image>().color = new Color(warningText.GetComponentInParent<Image>().color.r, warningText.GetComponentInParent<Image>().color.g + 0.02f, warningText.GetComponentInParent<Image>().color.b);
            StartCoroutine("WarningTextColorFade");
        }
    }
}
