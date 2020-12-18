using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Circle))]
public class CircleEditor : Editor
{
    bool editing = false;
    Circle circle;
    public override void OnInspectorGUI()
    {
        circle = (Circle)target;

        circle.Radius = EditorGUILayout.FloatField("Radius", circle.Radius);
        circle.Resolution = EditorGUILayout.IntField("Resolution", circle.Resolution);

        if (EditorUtility.IsDirty(circle))
        {
            circle.Setup();
        }

        if (GUILayout.Button("Update Shape"))
        {
            circle.Setup();
            EditorUtility.SetDirty(circle);
        }

        if (editing)
        {
            if (GUILayout.Button("Stop Editing Shape"))
            {
                editing = false;
            }
        }
        else
        {
            if (GUILayout.Button("Edit Shape"))
            {
                editing = true;
                EditorUtility.SetDirty(circle);
            }
        }
    }

    private void OnSceneGUI()
    {
        if (editing)
        {

            for (int i = 0; i < circle.Points.Count; i = i+(circle.Resolution/4))
            {
                EditorGUI.BeginChangeCheck();
                Vector2 newTargetPosition = Handles.PositionHandle(circle.Points[i] + new Vector2(circle.transform.position.x, circle.transform.position.y), Quaternion.identity);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(circle, "Changed position of corner");
                    circle.Radius = (newTargetPosition - new Vector2(circle.transform.position.x, circle.transform.position.y)).magnitude;
                }
            }
        }

        if (EditorUtility.IsDirty(circle))
        {
            circle.FocusPointsAboutCenter();
            EditorUtility.SetDirty(circle);
        }
    }
}
