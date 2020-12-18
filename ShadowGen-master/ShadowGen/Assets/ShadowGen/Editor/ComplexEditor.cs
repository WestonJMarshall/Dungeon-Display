using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ComplexShape))]
public class ComplexEditor : Editor
{
    ComplexShape shape;
    Vector3 toolPositionPrevious;
    bool editing = false;
    public override void OnInspectorGUI()
    {
        shape = (ComplexShape)target;

        //Move the shape when its main handle is moved
        if(Tools.handlePosition != toolPositionPrevious)
        {
            shape.FocusPointsAboutCenter();
            EditorUtility.SetDirty(shape);
        }
        toolPositionPrevious = Tools.handlePosition;

        if (editing)
        {
            #region Draw Points
            GUI.color = Color.white;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            GUILayout.TextField("#");

            for (int i = 0; i < shape.points.Count; i++)
            {
                float h = 0.0f, s = 0.0f, v = 0.0f;
                Color.RGBToHSV(GUI.color, out h, out s, out v);
                GUI.color = Color.HSVToRGB(h + 0.2f, 0.15f, 1);
                GUILayout.TextField(i.ToString());
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            GUI.color = Color.white;

            GUILayout.TextField("X");

            foreach (Vector2 v in shape.points)
            {
                float h = 0.0f, s = 0.0f, v2 = 0.0f;
                Color.RGBToHSV(GUI.color, out h, out s, out v2);
                GUI.color = Color.HSVToRGB(h + 0.2f, 0.15f, 1);
                GUILayout.TextField(v.x.ToString());
            }

            GUILayout.EndVertical();

            GUI.color = Color.white;

            GUILayout.BeginVertical();

            GUILayout.TextField("Y");

            foreach (Vector2 v in shape.points)
            {
                float h = 0.0f, s = 0.0f, v2 = 0.0f;
                Color.RGBToHSV(GUI.color, out h, out s, out v2);
                GUI.color = Color.HSVToRGB(h + 0.2f, 0.15f, 1);
                GUILayout.TextField(v.y.ToString());
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            #endregion

            GUI.color = new Color(1.0f, 0.5f, 0.5f);
            GUI.contentColor = Color.white;
            if (GUILayout.Button("Stop Editing Shape"))
            {
                editing = false;
                shape.FocusPointsAboutCenter();
                EditorUtility.SetDirty(shape);
            }
        }
        else
        {
            GUI.contentColor = Color.black;
            GUI.color = Color.white; ;

            GUIStyle gsText = new GUIStyle(GUI.skin.textArea);
            gsText.fontSize = 20;
            gsText.alignment = TextAnchor.MiddleCenter;
            GUILayout.TextField("Points: " + shape.points.Count, gsText);

            GUIStyle gsButton = new GUIStyle(GUI.skin.button);
            gsButton.fontSize = 40;
            gsButton.alignment = TextAnchor.MiddleCenter;
            gsButton.fixedWidth = 50.0f;
            gsButton.fixedHeight = 50.0f;

            #region Draw Points
            GUI.contentColor = Color.white;
            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.95f, 0.5f, 0.5f);
            if (GUILayout.Button("-", gsButton))
            {
                if (shape.points.Count > 0)
                {
                    shape.points.RemoveAt(shape.points.Count - 1);
                }

                shape.FocusPointsAboutCenter();
                shape.CompileEdgeList();
                EditorUtility.SetDirty(shape);
            }

            GUI.color = Color.white;

            GUILayout.BeginVertical();

            GUILayout.TextField("#");

            for(int i = 0; i < shape.points.Count; i++)
            {
                float h = 0.0f, s = 0.0f, v = 0.0f;
                Color.RGBToHSV(GUI.color, out h, out s, out v);
                GUI.color = Color.HSVToRGB(h + 0.2f, 0.15f, 1);
                GUILayout.TextField(i.ToString());
            }

            GUILayout.EndVertical();

            GUI.color = Color.white;

            GUILayout.BeginVertical();

            GUILayout.TextField("X");

            foreach(Vector2 v in shape.points)
            {
                float h = 0.0f, s = 0.0f, v2 = 0.0f;
                Color.RGBToHSV(GUI.color, out h, out s, out v2);
                GUI.color = Color.HSVToRGB(h + 0.2f, 0.15f, 1);
                GUILayout.TextField(v.x.ToString());
            }

            GUILayout.EndVertical();

            GUI.color = Color.white;

            GUILayout.BeginVertical();

            GUILayout.TextField("Y");

            foreach (Vector2 v in shape.points)
            {
                float h = 0.0f, s = 0.0f, v2 = 0.0f;
                Color.RGBToHSV(GUI.color, out h, out s, out v2);
                GUI.color = Color.HSVToRGB(h + 0.2f, 0.15f, 1);
                GUILayout.TextField(v.y.ToString());
            }

            GUILayout.EndVertical();


            GUI.color = new Color(0.5f, 0.95f, 0.5f);
            if (GUILayout.Button("+", gsButton))
            {
                if(shape.points.Count > 1)
                {
                    shape.points.Add(shape.points[shape.points.Count - 1] + ((shape.points[0] - shape.points[shape.points.Count - 1]) / 2.0f));
                }
                else
                {
                    shape.points.Add(new Vector2(shape.transform.position.x, shape.transform.position.y) + new Vector2(0.5f, 0.5f));
                }

                shape.FocusPointsAboutCenter();
                shape.CompileEdgeList();
                EditorUtility.SetDirty(shape);
            }
            GUILayout.EndHorizontal();

            #endregion

            GUILayout.Space(25);

            GUI.color = new Color(0.5f, 0.95f, 0.95f);
            if (GUILayout.Button("Edit Shape"))
            {
                editing = true;
                EditorUtility.SetDirty(shape);
            }
            if (GUILayout.Button("Generate Bounding Sphere"))
            {
                shape.GenerateBoundingSphere();
                EditorUtility.SetDirty(shape);
            }
            if (GUILayout.Button("Spherize Shape"))
            {
                shape.Spherize();
                EditorUtility.SetDirty(shape);
            }
        }
    }

    private void OnSceneGUI()
    {
        if (editing)
        {
            for (int i = 0; i < shape.Points.Count; i++)
            {
                EditorGUI.BeginChangeCheck();
                Vector2 newTargetPosition = Handles.PositionHandle(shape.Points[i], Quaternion.identity);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(shape, "Changed position of " + i);
                    shape.Points[i] = newTargetPosition;
                }
            }
        }
    }
}
