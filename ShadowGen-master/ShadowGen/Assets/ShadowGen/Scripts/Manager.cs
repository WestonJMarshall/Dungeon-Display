using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class Manager : MonoBehaviour
{
    #region Singlton
    private static Manager instance;

    public static Manager Instance { get { return instance; } }
    #endregion

    //Properties
    private List<CustomLight> lights;
    private List<Shape> shapes;

    #region Properties
    
    public List<CustomLight> Lights
    {
        get { return lights; }
    }
    
    public List<Shape> Shapes
    {
        get { return shapes; }
    }

    #endregion

    #region Unity Methods
    //Used to make sure the singleton is working
    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
    // Start is called before the first frame update
    void Start()
    {
        FindElementsToManage();
        AssignShapesAmbiguous();
    }

    private void Update()
    {
        #region Optional Controls
        if (shapes.Count > 0)
        {
            if (Input.GetKey(KeyCode.W)) { lights[0].Translate(new Vector2(0.0f, 0.05f)); }
            if (Input.GetKey(KeyCode.A)) { lights[0].Translate(new Vector2(-0.05f, 0.0f)); }
            if (Input.GetKey(KeyCode.S)) { lights[0].Translate(new Vector2(0.0f, -0.05f)); }
            if (Input.GetKey(KeyCode.D)) { lights[0].Translate(new Vector2(0.05f, 0.0f)); }
            if (Input.GetKey(KeyCode.E)) { shapes[0].Rotate(shapes[0].boundingSphereCenter, -0.5f); }
            if (Input.GetKey(KeyCode.Q)) { shapes[0].Rotate(shapes[0].boundingSphereCenter, 0.5f); }
            if (Input.GetKey(KeyCode.X)) { shapes[0].Scale(shapes[0].boundingSphereCenter, 0.99f); }
            if (Input.GetKey(KeyCode.Z)) { shapes[0].Scale(shapes[0].boundingSphereCenter, 1.01f); }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                foreach(CustomLight light in lights)
                {
                    light.drawShadows = !light.drawShadows;
                }
            }
        }
        #endregion

        AssignShapes();
    }
    #endregion

    #region Methods
    private void AssignShapesAmbiguous()
    {
        foreach (CustomLight light in lights)
        {
            foreach (Shape shape in shapes)
            {
                if (Vector2.Distance(shape.boundingSphereCenter, light.boundingSphereCenter) <= shape.boundingSphereRadius + light.boundingSphereRadius)
                {
                    //More expensive check to see if the shape is actually within the light area
                    //Check if any of the shape points are within the light area
                    if (Shape.IsPointInTriangleArray(shape.Points, light.Points))
                    {
                        light.shapes.Add(shape);
                    }
                }
            }
        }
    }

    private void AssignShapes()
    {
        foreach(CustomLight light in lights)
        {
            bool assignNewShapes = false;
            bool lightAreaReset = false;
            if (light.location != light.lastLocation || light.rotation != light.lastRotation || light.scale != light.lastScale) { assignNewShapes = true; light.shapes = new List<Shape>(); lightAreaReset = true; }
            foreach (Shape shape in shapes)
            {
                if (shape.location != shape.lastLocation || shape.rotation != shape.lastRotation || shape.scale != shape.lastScale) { assignNewShapes = true; }
                if (assignNewShapes)
                {
                    if (!lightAreaReset) { light.shapes = new List<Shape>(); lightAreaReset = true; }
                    if (Vector2.Distance(shape.boundingSphereCenter, light.boundingSphereCenter) <= shape.boundingSphereRadius + light.boundingSphereRadius)
                    {
                        //More expensive check to see if the shape is actually within the light area
                        //Check if any of the shape points are within the light area
                        if (Shape.IsPointInTriangleArray(shape.Points, light.Points))
                        {
                            light.shapes.Add(shape);
                        }
                    }
                }
            }
        }
    }

    private void FindElementsToManage()
    {
        CustomLight[] tempL = GameObject.FindObjectsOfType<CustomLight>();
        lights = new List<CustomLight>(tempL);

        Shape[] tempS = GameObject.FindObjectsOfType<Shape>();
        shapes = new List<Shape>(tempS);
    }
    #endregion
}
