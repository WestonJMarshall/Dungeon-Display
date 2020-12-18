using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LightTool : MonoBehaviour
{
    public GameObject freeLightAreaPrefab;
    public List<GameObject> freeLightAreas;

    public GameObject formattingPanelA;
    public GameObject formattingPanelB;
    public GameObject formattingPanelC;
    public GameObject gridSnapToggle;
    public GameObject functionalityToggle;
    public Scrollbar scrollBar;

    public GameObject deletePointButtonPrefab;
    public GameObject pointInformationPrefab;
    public GameObject dragPointPrefab;
    public GameObject pointLinePrefab;

    public GameObject currentShape;
    public GameObject hoveredButton;
    public GameObject currentDragPoint;

    public List<GameObject> xPoints;
    public List<GameObject> yPoints;
    public List<GameObject> deleteButtons;

    public List<GameObject> dragPoints;
    public List<GameObject> pointLines;

    public bool pauseUpdates = false;
    private bool deletePrepare = false;
    private bool gridSnap = false;
    private bool canDrag = true;
    private DragState dragState = DragState.None;

    public void Awake()
    {
        freeLightAreas = new List<GameObject>();
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
            if(ShapeClick())
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
                if (Shape.IsPointInTriangleArray(Camera.main.ScreenToWorldPoint(Input.mousePosition), currentShape.GetComponent<FreeLightArea>().points) && !EventSystem.current.IsPointerOverGameObject() && dragState != DragState.Shape)
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
        if (currentShape != null && dragPoints.Count == 0 && currentShape.GetComponent<FreeLightArea>().points.Count > 0)
        {
            DragPoint[] dpl = FindObjectsOfType<DragPoint>();

            foreach (DragPoint dp in dpl)
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

    public void ServerHandleLightChange(int lightIndex, bool delete, List<Vector2> pointValues, bool functionality, bool closeAfterFinish)
    {
        currentShape = freeLightAreas[lightIndex];

        if (delete)
        {
            if (currentShape != null)
            {
                freeLightAreas.Remove(currentShape);
                DeleteCurrentData(true, true);
            }
            currentShape = null;
            deletePrepare = true;
        }
        else
        {
            if (pointValues.Count != 0)
            {
                currentShape.GetComponent<FreeLightArea>().points = new List<Vector2>();
                foreach (Vector2 v in pointValues) { currentShape.GetComponent<FreeLightArea>().points.Add(new Vector2(v.x, v.y)); }
            }
            if(functionality != currentShape.GetComponent<FreeLightArea>().functional)
            {
                ServerToggleFunctionality();
            }
        }
        SetCurrentShapePointsList();
        Scroll();
        if (closeAfterFinish) { Manager.Instance.CloseLightMenu(); }
    }

    public void DragShape()
    {
        if (currentShape != null && !gridSnap)
        {
            bool clickable = true;
            foreach (Vector2 v in currentShape.GetComponent<FreeLightArea>().points)
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
                currentShape.GetComponent<FreeLightArea>().GenerateBoundingSphere();
                List<Vector2> points = new List<Vector2>();
                foreach (Vector2 v in currentShape.GetComponent<FreeLightArea>().points) { points.Add(new Vector2(v.x, v.y)); }
                for (int i = 0; i < points.Count; i++)
                {
                    points[i] += (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - currentShape.GetComponent<FreeLightArea>().boundingSphereCenter;
                }
                string pointValues = "";
                foreach (Vector2 v in points)
                {
                    pointValues += v.x.ToString() + "|" + v.y.ToString() + "|";
                }
                NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"fvc{freeLightAreas.IndexOf(currentShape)}|{0}|{pointValues}{(currentShape.GetComponent<FreeLightArea>().functional ? 1 : 0)}"));
            }
            else
            {
                currentShape.GetComponent<FreeLightArea>().GenerateBoundingSphere();

                for (int i = 0; i < currentShape.GetComponent<FreeLightArea>().points.Count; i++)
                {
                    currentShape.GetComponent<FreeLightArea>().points[i] += (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - currentShape.GetComponent<FreeLightArea>().boundingSphereCenter;
                }

                SetCurrentShapePointsList();
            }
        }

    }

    public bool ShapeClick()
    {
        if(currentShape != null)
        {
            bool clickable = true;
            foreach (Vector2 v in currentShape.GetComponent<FreeLightArea>().points)
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
            foreach (GameObject g in freeLightAreas)
            {
                if (Shape.IsPointInTriangleArray(Camera.main.ScreenToWorldPoint(Input.mousePosition), g.GetComponent<FreeLightArea>().points))
                {
                    if (Manager.Instance.inspector.gameObject.activeSelf)
                    {
                        Manager.Instance.inspector.CloseInspector();
                    }
                    if (currentShape == null)
                    {
                        currentShape = g;
                        SetCurrentShapePointsList();
                        return true;
                    }
                    else
                    {
                        if (currentShape == g)
                        {
                            break;
                        }
                        DeleteCurrentData(true, false);
                        currentShape = g;
                        SetCurrentShapePointsList();
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void CreateShape()
    {
        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
        {
            GameObject lightShape = Instantiate(freeLightAreaPrefab);
            List<Vector2> points = new List<Vector2>();
            foreach (Vector2 v in lightShape.GetComponent<FreeLightArea>().points) { points.Add(new Vector2(v.x, v.y)); }
            string pointValues = "";
            foreach (Vector2 v in points)
            {
                pointValues += v.x.ToString() + "|" + v.y.ToString() + "|";
            }
            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"fcr{pointValues}"));
            DestroyImmediate(lightShape);
        }
        else
        {
            DeleteCurrentData(true, false);

            GameObject lightShape = Instantiate(freeLightAreaPrefab);
            lightShape.name = "Free Light Area";
            currentShape = lightShape;

            freeLightAreas.Add(lightShape);

            currentShape.transform.position = Vector2.zero;

            SetCurrentShapePointsList();
        }
    }

    public void ServerCreateShape(List<Vector2> pointValues, bool closeAfterFinish)
    {
        DeleteCurrentData(true, false);

        GameObject lightShape = Instantiate(freeLightAreaPrefab);
        lightShape.name = "Free Light Area";
        currentShape = lightShape;

        freeLightAreas.Add(lightShape);

        currentShape.transform.position = Vector2.zero;
        currentShape.GetComponent<FreeLightArea>().points = new List<Vector2>();
        foreach (Vector2 v in pointValues) { currentShape.GetComponent<FreeLightArea>().points.Add(new Vector2(v.x, v.y)); }
        SetCurrentShapePointsList();

        if (closeAfterFinish) { Manager.Instance.CloseLightMenu(); }
    }

    public void DeleteShape()
    {
        if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection && currentShape != null)
        {
            NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"fvc{freeLightAreas.IndexOf(currentShape)}|{1}|X|{0}"));
        }
        else
        {
            if (currentShape != null)
            {
                freeLightAreas.Remove(currentShape);
                DeleteCurrentData(true, true);
            }
            currentShape = null;
            deletePrepare = true;
        }
    }

    public void AddPoint()
    {
        if (currentShape != null)
        {
            #region  Find best place to add the point
            Vector2 placement = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width / 2, Screen.height / 2));
            List<Edge> edgesToCheck = new List<Edge>();

            for (int i = 0; i < currentShape.GetComponent<FreeLightArea>().points.Count; i++)
            {
                if(i < currentShape.GetComponent<FreeLightArea>().points.Count - 1)
                {
                    edgesToCheck.Add(new Edge(currentShape.GetComponent<FreeLightArea>().points[i], currentShape.GetComponent<FreeLightArea>().points[i + 1]));
                }
                else
                {
                    edgesToCheck.Add(new Edge(currentShape.GetComponent<FreeLightArea>().points[i], currentShape.GetComponent<FreeLightArea>().points[0]));
                }
            }
            List<Vector2> middlePoints = new List<Vector2>();
            foreach(Edge e in edgesToCheck)
            {
                middlePoints.Add(e.startPoint + ((e.endPoint - e.startPoint) / 2.0f));
            }

            List<Vector2> visiblePoints = new List<Vector2>();
            List<int> visiblePointsIndeces = new List<int>();
            for (int i = 0; i < middlePoints.Count; i++)
            {
                Edge edgeToCheck = new Edge(placement, middlePoints[i]);
                bool visible = true;
                foreach(Edge e in edgesToCheck)
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
                if(visible)
                {
                    visiblePoints.Add(middlePoints[i]);
                    visiblePointsIndeces.Add(i);
                }
            }

            if (visiblePoints.Count > 0)
            {
                Vector2 closestPoint = visiblePoints[0];

                for(int i = 1; i < visiblePoints.Count; i++)
                {
                    if(Vector2.Distance(visiblePoints[i], placement) < Vector2.Distance(closestPoint, placement))
                    {
                        closestPoint = visiblePoints[i];
                    }
                }
                if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
                {
                    currentShape.GetComponent<FreeLightArea>().GenerateBoundingSphere();
                    List<Vector2> points = new List<Vector2>();
                    foreach (Vector2 v in currentShape.GetComponent<FreeLightArea>().points) { points.Add(new Vector2(v.x, v.y)); }
                    points.Insert(middlePoints.IndexOf(closestPoint) + 1, Camera.main.ScreenToWorldPoint(new Vector2(Screen.width / 2, Screen.height / 2)));
                    string pointValues = "";
                    foreach (Vector2 v in points)
                    {
                        pointValues += v.x.ToString() + "|" + v.y.ToString() + "|";
                    }
                    NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"fvc{freeLightAreas.IndexOf(currentShape)}|{0}|{pointValues}{(currentShape.GetComponent<FreeLightArea>().functional ? 1 : 0)}"));
                }
                else
                {
                    currentShape.GetComponent<FreeLightArea>().points.Insert(middlePoints.IndexOf(closestPoint) + 1, Camera.main.ScreenToWorldPoint(new Vector2(Screen.width / 2, Screen.height / 2)));
                    SetCurrentShapePointsList();
                    Scroll();
                }
            }
            else
            {
                if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
                {
                    currentShape.GetComponent<FreeLightArea>().GenerateBoundingSphere();
                    List<Vector2> points = new List<Vector2>();
                    foreach (Vector2 v in currentShape.GetComponent<FreeLightArea>().points) { points.Add(new Vector2(v.x, v.y)); }
                    points.Add(Camera.main.ScreenToWorldPoint(new Vector2(Screen.width / 2, Screen.height / 2)));
                    string pointValues = "";
                    foreach (Vector2 v in points)
                    {
                        pointValues += v.x.ToString() + "|" + v.y.ToString() + "|";
                    }
                    NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"fvc{freeLightAreas.IndexOf(currentShape)}|{0}|{pointValues}{(currentShape.GetComponent<FreeLightArea>().functional ? 1 : 0)}"));
                }
                else
                {
                    currentShape.GetComponent<FreeLightArea>().points.Add(Camera.main.ScreenToWorldPoint(new Vector2(Screen.width / 2, Screen.height / 2)));
                    SetCurrentShapePointsList();
                    Scroll();
                }
            }
            #endregion
        }
    }

    public void PointAltered(float input, int index, bool xValue)
    {
        if (index < currentShape.GetComponent<FreeLightArea>().points.Count)
        {
            if (xValue)
            {
                if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
                {
                    currentShape.GetComponent<FreeLightArea>().GenerateBoundingSphere();
                    List<Vector2> points = new List<Vector2>();
                    foreach (Vector2 v in currentShape.GetComponent<FreeLightArea>().points) { points.Add(new Vector2(v.x, v.y)); }
                    points[index] = new Vector2(input, currentShape.GetComponent<FreeLightArea>().points[index].y);
                    string pointValues = "";
                    foreach (Vector2 v in points)
                    {
                        pointValues += v.x.ToString() + "|" + v.y.ToString() + "|";
                    }
                    NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"fvc{freeLightAreas.IndexOf(currentShape)}|{0}|{pointValues}{(currentShape.GetComponent<FreeLightArea>().functional ? 1 : 0)}"));
                }
                else
                {
                    currentShape.GetComponent<FreeLightArea>().points[index] = new Vector2(input, currentShape.GetComponent<FreeLightArea>().points[index].y);
                    SetCurrentShapePointsList();
                }
            }
            else
            {
                if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
                {
                    currentShape.GetComponent<FreeLightArea>().GenerateBoundingSphere();
                    List<Vector2> points = new List<Vector2>();
                    foreach (Vector2 v in currentShape.GetComponent<FreeLightArea>().points) { points.Add(new Vector2(v.x, v.y)); }
                    points[index] = new Vector2(currentShape.GetComponent<FreeLightArea>().points[index].x, input);
                    string pointValues = "";
                    foreach (Vector2 v in points)
                    {
                        pointValues += v.x.ToString() + "|" + v.y.ToString() + "|";
                    }
                    NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"fvc{freeLightAreas.IndexOf(currentShape)}|{0}|{pointValues}{(currentShape.GetComponent<FreeLightArea>().functional ? 1 : 0)}"));
                }
                else
                {
                    currentShape.GetComponent<FreeLightArea>().points[index] = new Vector2(currentShape.GetComponent<FreeLightArea>().points[index].x, input);
                    SetCurrentShapePointsList();
                }
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

                if(Camera.main.ScreenToWorldPoint(Input.mousePosition).x > 1 && Camera.main.ScreenToWorldPoint(Input.mousePosition).x <= 1.5f)
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
                currentShape.GetComponent<FreeLightArea>().GenerateBoundingSphere();
                List<Vector2> points = new List<Vector2>();
                foreach (Vector2 v in currentShape.GetComponent<FreeLightArea>().points) { points.Add(new Vector2(v.x, v.y)); }
                points[dragPoints.IndexOf(currentDragPoint)] = currentDragPoint.transform.position;
                string pointValues = "";
                foreach (Vector2 v in points)
                {
                    pointValues += v.x.ToString() + "|" + v.y.ToString() + "|";
                }
                NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"fvc{freeLightAreas.IndexOf(currentShape)}|{0}|{pointValues}{(currentShape.GetComponent<FreeLightArea>().functional ? 1 : 0)}"));
            }
            else
            {
                currentShape.GetComponent<FreeLightArea>().points[dragPoints.IndexOf(currentDragPoint)] = currentDragPoint.transform.position;
                SetCurrentShapePointsList();
            }
        }
    }

    public void ToggleGridSnap()
    {
        if (gridSnap)
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
        if (currentShape != null)
        {
            if (!currentShape.GetComponent<FreeLightArea>().functional)
            {
                if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
                {
                    NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"fvc{freeLightAreas.IndexOf(currentShape)}|{0}|X|{1}"));
                }
                else
                {
                    functionalityToggle.GetComponent<Image>().color = new Color(158.0f / 255.0f, 255.0f / 255.0f, 160.0f / 255.0f);
                    currentShape.GetComponent<FreeLightArea>().functional = true;
                    currentShape.GetComponent<FreeLightArea>().DrawLight();
                }
            }
            else
            {
                if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
                {
                    NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"fvc{freeLightAreas.IndexOf(currentShape)}|{0}|X|{0}"));
                }
                else
                {
                    functionalityToggle.GetComponent<Image>().color = new Color(169.0f / 255.0f, 99.0f / 255.0f, 98.0f / 255.0f);
                    currentShape.GetComponent<FreeLightArea>().ClearDrawnLight();
                    currentShape.GetComponent<FreeLightArea>().functional = false;
                }
            }
        }
    }

    public void ServerToggleFunctionality()
    {
        if (currentShape != null)
        {
            if (!currentShape.GetComponent<FreeLightArea>().functional)
            {
                functionalityToggle.GetComponent<Image>().color = new Color(158.0f / 255.0f, 255.0f / 255.0f, 160.0f / 255.0f);
                currentShape.GetComponent<FreeLightArea>().functional = true;
                currentShape.GetComponent<FreeLightArea>().DrawLight();
            }
            else
            {
                functionalityToggle.GetComponent<Image>().color = new Color(169.0f / 255.0f, 99.0f / 255.0f, 98.0f / 255.0f);
                currentShape.GetComponent<FreeLightArea>().ClearDrawnLight();
                currentShape.GetComponent<FreeLightArea>().functional = false;
            }
        }
    }

    public void DeletePoint()
    {
        if (dragPoints.Count > 3)
        {
            if (NetworkingManager.Instance.networkingState != NetworkingState.NoConnection)
            {
                currentShape.GetComponent<FreeLightArea>().GenerateBoundingSphere();
                List<Vector2> points = new List<Vector2>();
                foreach (Vector2 v in currentShape.GetComponent<FreeLightArea>().points) { points.Add(new Vector2(v.x, v.y)); }
                int indexToDelete = deleteButtons.IndexOf(hoveredButton);
                points.RemoveAt(indexToDelete);
                string pointValues = "";
                foreach (Vector2 v in points)
                {
                    pointValues += v.x.ToString() + "|" + v.y.ToString() + "|";
                }
                NetworkingManager.Instance.gameClient.Connection.SendMessage(Encoding.UTF8.GetBytes($"fvc{freeLightAreas.IndexOf(currentShape)}|{0}|{pointValues}{(currentShape.GetComponent<FreeLightArea>().functional ? 1 : 0)}"));
            }
            else
            {
                int indexToDelete = deleteButtons.IndexOf(hoveredButton);
                currentShape.GetComponent<FreeLightArea>().points.RemoveAt(indexToDelete);
                SetCurrentShapePointsList();
                Scroll();
            }
        }
    }
    
    public void Scroll()
    {
        int scrollValue = 0;
        if (xPoints.Count > 8)
        {
            scrollValue = (int)(scrollBar.value * (xPoints.Count - 8) * 37);
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

            foreach (GameObject g in dragPoints)
            {
                Destroy(g);
            }

            foreach (GameObject g in pointLines)
            {
                Destroy(g);
            }

            if (destroyCurrentShape)
            {
                Destroy(currentShape);
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

                for (int i = 0; i < currentShape.GetComponent<FreeLightArea>().points.Count; i++)
                {
                    Vector2 v = currentShape.GetComponent<FreeLightArea>().points[i];

                    xPoints.Add(Instantiate(pointInformationPrefab, formattingPanelA.transform));
                    xPoints[xPoints.Count - 1].GetComponentInChildren<InputField>().text = v.x.ToString();
                    xPoints[xPoints.Count - 1].GetComponent<Image>().color = Color.HSVToRGB((i % 10.0f) / 10.0f, 0.18f, 1);

                    yPoints.Add(Instantiate(pointInformationPrefab, formattingPanelB.transform));
                    yPoints[yPoints.Count - 1].GetComponentInChildren<InputField>().text = v.y.ToString();
                    yPoints[yPoints.Count - 1].GetComponent<Image>().color = Color.HSVToRGB((i % 10.0f) / 10.0f, 0.18f, 1);

                    GameObject deleteButton = Instantiate(deletePointButtonPrefab, formattingPanelC.transform);
                    deleteButton.GetComponent<Button>().onClick.AddListener(DeletePoint);
                    deleteButtons.Add(deleteButton);

                    if (i != currentShape.GetComponent<FreeLightArea>().points.Count - 1)
                    {
                        GameObject pointLine = Instantiate(pointLinePrefab);
                        pointLine.transform.position = v;
                        SetupPointLine(pointLine, v, currentShape.GetComponent<FreeLightArea>().points[i + 1]);
                        pointLines.Add(pointLine);
                    }
                    else
                    {
                        if (i > 0)
                        {
                            GameObject pointLine = Instantiate(pointLinePrefab);
                            pointLine.transform.position = v;
                            SetupPointLine(pointLine, v, currentShape.GetComponent<FreeLightArea>().points[0]);
                            pointLines.Add(pointLine);
                        }
                    }

                    GameObject dragPoint = Instantiate(dragPointPrefab);
                    dragPoint.GetComponent<DragPoint>().linkedTool = gameObject;
                    dragPoint.GetComponent<DragPoint>().typeShadow = false;
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

                for (int i = 0; i < currentShape.GetComponent<FreeLightArea>().points.Count; i++)
                {
                    Vector2 v = currentShape.GetComponent<FreeLightArea>().points[i];

                    xPoints[i].GetComponentInChildren<InputField>().text = v.x.ToString();

                    yPoints[i].GetComponentInChildren<InputField>().text = v.y.ToString();

                    if (i != currentShape.GetComponent<FreeLightArea>().points.Count - 1)
                    {
                        GameObject pointLine = Instantiate(pointLinePrefab);
                        pointLine.transform.position = v;
                        SetupPointLine(pointLine, v, currentShape.GetComponent<FreeLightArea>().points[i + 1]);
                        pointLines.Add(pointLine);
                    }
                    else
                    {
                        if (i > 0)
                        {
                            GameObject pointLine = Instantiate(pointLinePrefab);
                            pointLine.transform.position = v;
                            SetupPointLine(pointLine, v, currentShape.GetComponent<FreeLightArea>().points[0]);
                            pointLines.Add(pointLine);
                        }
                    }
                }
            }

            currentShape.GetComponent<FreeLightArea>().GenerateBoundingSphere();
            if(currentShape.GetComponent<FreeLightArea>().functional)
            {
                functionalityToggle.GetComponent<Image>().color = new Color(158.0f / 255.0f, 255.0f / 255.0f, 160.0f / 255.0f);
            }
            else
            {
                functionalityToggle.GetComponent<Image>().color = new Color(169.0f / 255.0f, 99.0f / 255.0f, 98.0f / 255.0f);
            }

            currentShape.GetComponent<FreeLightArea>().DrawLight();
        }
    }

    public void SetupPointLine(GameObject line, Vector2 start, Vector2 end)
    {
        float deg = Mathf.Rad2Deg * Mathf.Atan2((end - start).y, (end - start).x);
        line.transform.rotation = Quaternion.Euler(0, 0, deg - 90);

        line.transform.position = start + ((end - start) / 2.0f);

        line.transform.localScale = new Vector3(0.055f, Vector2.Distance(start, end), 1);
    }

    public void CloseLightTool()
    {
        DeleteCurrentData(true, false);
    }

    public void OpenLightTool()
    {
        freeLightAreas = new List<GameObject>();
        FreeLightArea[] flal = FindObjectsOfType<FreeLightArea>();

        foreach(FreeLightArea fla in flal)
        {
            freeLightAreas.Add(fla.gameObject);
        }

        if (freeLightAreas.Count > 0)
        {
            currentShape = freeLightAreas[0];
        }

        if (gridSnap)
        {
            gridSnapToggle.GetComponent<Image>().color = new Color(158.0f / 255.0f, 255.0f / 255.0f, 160.0f / 255.0f);
        }
        else
        {
            gridSnapToggle.GetComponent<Image>().color = new Color(169.0f / 255.0f, 99.0f / 255.0f, 98.0f / 255.0f);
        }

        if (currentShape != null)
        {
            if (currentShape.GetComponent<FreeLightArea>().functional)
            {
                gridSnapToggle.GetComponent<Image>().color = new Color(158.0f / 255.0f, 255.0f / 255.0f, 160.0f / 255.0f);
            }
            else
            {
                functionalityToggle.GetComponent<Image>().color = new Color(169.0f / 255.0f, 99.0f / 255.0f, 98.0f / 255.0f);
            }
        }

        SetCurrentShapePointsList();
    }

    public void CloseButtonPressed()
    {
        Manager.Instance.OpenLightMenu();
    }

    public void RightButtonPressed()
    {
        if (freeLightAreas.Count > 1)
        {
            if (freeLightAreas.IndexOf(currentShape) < freeLightAreas.Count - 1)
            {
                currentShape = freeLightAreas[freeLightAreas.IndexOf(currentShape) + 1];
            }
            else
            {
                currentShape = freeLightAreas[0];
            }
            SetCurrentShapePointsList();
        }
    }

    public void LeftButtonPressed()
    {
        if(freeLightAreas.Count > 1)
        {
            if(freeLightAreas.IndexOf(currentShape) > 0)
            {
                currentShape = freeLightAreas[freeLightAreas.IndexOf(currentShape) - 1];
            }
            else
            {
                currentShape = freeLightAreas[freeLightAreas.Count - 1];
            }
            SetCurrentShapePointsList();
        }
    }
}
