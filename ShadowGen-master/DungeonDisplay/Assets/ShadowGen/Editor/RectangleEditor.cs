using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Rectangle))]
public class RectangleEditor : Editor
{
    Rectangle rect;
    bool editing = false;
    public override void OnInspectorGUI()
    {
        rect = (Rectangle)target;

        rect.Extents = EditorGUILayout.Vector2Field("Extents", rect.Extents);

        if (EditorUtility.IsDirty(rect))
        {
            rect.Setup();
        }

        if (GUILayout.Button("Update Shape"))
        {
            rect.Setup();
            EditorUtility.SetDirty(rect);
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
                EditorUtility.SetDirty(rect);
            }
        }
    }

    private void OnSceneGUI()
    {
        if (editing)
        {
            for (int i = 0; i < rect.Points.Count; i++)
            {
                EditorGUI.BeginChangeCheck();
                //Get the point and it's opposite
                Vector2 newTargetPosition = Handles.PositionHandle(rect.Points[i] + new Vector2(rect.transform.position.x, rect.transform.position.y), Quaternion.identity);
                Vector2 oppositePoint = rect.Points[(i + 2) % 4] + new Vector2(rect.transform.position.x, rect.transform.position.y);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(rect, "Changed position of corner");
                    //Get the distance between the pointsin x and y
                    float xDist = newTargetPosition.x - oppositePoint.x;
                    float yDist = newTargetPosition.y - oppositePoint.y;

                    //Set the center and extents
                    //rect.transform.position = new Vector3(oppositePoint.x + (xDist / 2), oppositePoint.y + (yDist / 2), rect.transform.position.z);
                    rect.Extents = new Vector2(Mathf.Abs(xDist / 2.0f), Mathf.Abs(yDist / 2.0f));

                }
            }
        }

        if (EditorUtility.IsDirty(rect))
        {
            rect.FocusPointsAboutCenter();
            EditorUtility.SetDirty(rect);
        }
    }
}
