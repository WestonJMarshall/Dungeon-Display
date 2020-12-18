using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CustomLight))]
public class LightEditor : Editor
{
    CustomLight light;
    //bool editing = false;
    Vector3 toolPositionPrevious;

    public override void OnInspectorGUI()
    {
        //light = (CustomLight)target;

        DrawDefaultInspector();

        //if (Tools.handlePosition != toolPositionPrevious)
        //{
        //    light.FocusPointsAboutCenter();
        //    EditorUtility.SetDirty(light);
        //}
        //toolPositionPrevious = Tools.handlePosition;
        //
        //if (editing)
        //{
        //    GUI.color = Color.white;
        //
        //    GUILayout.BeginHorizontal();
        //    GUILayout.BeginVertical();
        //
        //    GUILayout.TextField("#");
        //
        //    for (int i = 0; i < light.points.Count; i++)
        //    {
        //        float h = 0.0f, s = 0.0f, v = 0.0f;
        //        Color.RGBToHSV(GUI.color, out h, out s, out v);
        //        GUI.color = Color.HSVToRGB(h + 0.2f, 0.15f, 1);
        //        GUILayout.TextField(i.ToString());
        //    }
        //
        //    GUILayout.EndVertical();
        //
        //    GUILayout.BeginVertical();
        //
        //    GUI.color = Color.white;
        //
        //    GUILayout.TextField("X");
        //
        //    foreach (Vector2 v in light.points)
        //    {
        //        float h = 0.0f, s = 0.0f, v2 = 0.0f;
        //        Color.RGBToHSV(GUI.color, out h, out s, out v2);
        //        GUI.color = Color.HSVToRGB(h + 0.2f, 0.15f, 1);
        //        GUILayout.TextField(v.x.ToString());
        //    }
        //
        //    GUILayout.EndVertical();
        //
        //    GUI.color = Color.white;
        //
        //    GUILayout.BeginVertical();
        //
        //    GUILayout.TextField("Y");
        //
        //    foreach (Vector2 v in light.points)
        //    {
        //        float h = 0.0f, s = 0.0f, v2 = 0.0f;
        //        Color.RGBToHSV(GUI.color, out h, out s, out v2);
        //        GUI.color = Color.HSVToRGB(h + 0.2f, 0.15f, 1);
        //        GUILayout.TextField(v.y.ToString());
        //    }
        //
        //    GUILayout.EndVertical();
        //    GUILayout.EndHorizontal();
        //
        //    GUI.color = new Color(1.0f, 0.5f, 0.5f);
        //    GUI.contentColor = Color.white;
        //    if (GUILayout.Button("Stop Editing light"))
        //    {
        //        editing = false;
        //        light.FocusPointsAboutCenter();
        //        EditorUtility.SetDirty(light);
        //    }
        //}
        //else
        //{
        //    GUI.contentColor = Color.black;
        //    GUI.color = Color.white; ;
        //
        //    GUIStyle gsText = new GUIStyle(GUI.skin.textArea);
        //    gsText.fontSize = 20;
        //    gsText.alignment = TextAnchor.MiddleCenter;
        //    GUILayout.TextField("Points: " + light.points.Count, gsText);
        //
        //    GUIStyle gsButton = new GUIStyle(GUI.skin.button);
        //    gsButton.fontSize = 40;
        //    gsButton.alignment = TextAnchor.MiddleCenter;
        //    gsButton.fixedWidth = 50.0f;
        //    gsButton.fixedHeight = 50.0f;
        //
        //
        //    GUI.contentColor = Color.white;
        //    GUILayout.BeginHorizontal();
        //    GUI.color = new Color(0.95f, 0.5f, 0.5f);
        //    if (GUILayout.Button("-", gsButton))
        //    {
        //        if (light.points.Count > 0)
        //        {
        //            light.points.RemoveAt(light.points.Count - 1);
        //        }
        //
        //        light.FocusPointsAboutCenter();
        //        light.CompileEdgeList();
        //        EditorUtility.SetDirty(light);
        //    }
        //
        //    GUI.color = Color.white;
        //
        //    GUILayout.BeginVertical();
        //
        //    GUILayout.TextField("#");
        //
        //    for (int i = 0; i < light.points.Count; i++)
        //    {
        //        float h = 0.0f, s = 0.0f, v = 0.0f;
        //        Color.RGBToHSV(GUI.color, out h, out s, out v);
        //        GUI.color = Color.HSVToRGB(h + 0.2f, 0.15f, 1);
        //        GUILayout.TextField(i.ToString());
        //    }
        //
        //    GUILayout.EndVertical();
        //
        //    GUI.color = Color.white;
        //
        //    GUILayout.BeginVertical();
        //
        //    GUILayout.TextField("X");
        //
        //    foreach (Vector2 v in light.points)
        //    {
        //        float h = 0.0f, s = 0.0f, v2 = 0.0f;
        //        Color.RGBToHSV(GUI.color, out h, out s, out v2);
        //        GUI.color = Color.HSVToRGB(h + 0.2f, 0.15f, 1);
        //        GUILayout.TextField(v.x.ToString());
        //    }
        //
        //    GUILayout.EndVertical();
        //
        //    GUI.color = Color.white;
        //
        //    GUILayout.BeginVertical();
        //
        //    GUILayout.TextField("Y");
        //
        //    foreach (Vector2 v in light.points)
        //    {
        //        float h = 0.0f, s = 0.0f, v2 = 0.0f;
        //        Color.RGBToHSV(GUI.color, out h, out s, out v2);
        //        GUI.color = Color.HSVToRGB(h + 0.2f, 0.15f, 1);
        //        GUILayout.TextField(v.y.ToString());
        //    }
        //
        //    GUILayout.EndVertical();
        //
        //
        //    GUI.color = new Color(0.5f, 0.95f, 0.5f);
        //    if (GUILayout.Button("+", gsButton))
        //    {
        //        if (light.points.Count > 1)
        //        {
        //            light.points.Add(light.points[light.points.Count - 1] + ((light.points[0] - light.points[light.points.Count - 1]) / 2.0f));
        //        }
        //        else
        //        {
        //            light.points.Add(new Vector2(light.transform.position.x, light.transform.position.y) + new Vector2(0.5f, 0.5f));
        //        }
        //
        //        light.FocusPointsAboutCenter();
        //        light.CompileEdgeList();
        //        EditorUtility.SetDirty(light);
        //    }
        //    GUILayout.EndHorizontal();
        //
        //    GUILayout.Space(25);
        //
        //    GUI.color = new Color(0.5f, 0.95f, 0.95f);
        //    if (GUILayout.Button("Edit light"))
        //    {
        //        editing = true;
        //        EditorUtility.SetDirty(light);
        //    }
        //    if (GUILayout.Button("Generate Bounding Sphere"))
        //    {
        //        light.GenerateBoundingSphere();
        //        EditorUtility.SetDirty(light);
        //    }
        //    if (GUILayout.Button("Spherize light"))
        //    {
        //        light.Spherize();
        //        EditorUtility.SetDirty(light);
        //    }
        //
        //    GUILayout.Space(25);
        //
        //    GUI.color = new Color(0.95f, 0.95f, 0.95f);
        //    gsText.fontSize = 14;
        //    GUILayout.TextField("What Happens When This GameObject's Transform is Changed?", gsText);
        //
        //    if(GUILayout.Toggle(light.moveAreaOnTransform,new GUIContent("Move Light Area")))
        //    {
        //        light.moveAreaOnTransform = true;
        //        EditorUtility.SetDirty(light);
        //    }
        //    else
        //    {
        //        light.moveAreaOnTransform = false;
        //        EditorUtility.SetDirty(light);
        //    }
        //
        //    if (GUILayout.Toggle(light.moveCenterOnTransform, new GUIContent("Move Center Point")))
        //    {
        //        light.moveCenterOnTransform = true;
        //        EditorUtility.SetDirty(light);
        //    }
        //    else
        //    {
        //        light.moveCenterOnTransform = false;
        //        EditorUtility.SetDirty(light);
        //    }
        //
        //}
    }
    private void OnSceneGUI()
    {
        //if (editing)
        //{
        //    for (int i = 0; i < light.Points.Count; i++)
        //    {
        //        EditorGUI.BeginChangeCheck();
        //        Vector2 newTargetPosition = Handles.PositionHandle(light.Points[i], Quaternion.identity);
        //
        //        if (EditorGUI.EndChangeCheck())
        //        {
        //            Undo.RecordObject(light, "Changed position of " + i);
        //            light.Points[i] = newTargetPosition;
        //        }
        //    }
        //}
        //
        //if (EditorUtility.IsDirty(light))
        //{
        //    EditorUtility.SetDirty(light);
        //}
    }
}
