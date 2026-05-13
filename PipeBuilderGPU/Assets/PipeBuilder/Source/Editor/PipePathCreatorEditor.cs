using UnityEngine;
using UnityEditor;

namespace PipeBuilder.Editor
{
    [CustomEditor(typeof(PipePathCreator))]
    public class PipePathCreatorEditor : UnityEditor.Editor
    {
        private const float _handleSize = 0.05f;
        private const float _pickSize = 0.07f;

        private int _selectedPoint = -1;

        private GUIStyle _labelStyle;

        private void Reset()
        {
            _labelStyle = new GUIStyle();

            _labelStyle.alignment = TextAnchor.MiddleCenter;
            _labelStyle.fontStyle |= FontStyle.Bold;
        }

        private void OnSceneGUI()
        {
            var creator = target as PipePathCreator;

            if (!creator.isDrawHandles)
                return;

            creator.RecalculateWorldPoints();

            var worldPoints = creator.worldPoints;

            if(worldPoints == null || worldPoints.Count == 0)
                return;

            _labelStyle.fontSize = creator.labelTextSize;

            for (int i = 0; i < worldPoints.Count; i++)
            {
                DrawPoint(i, worldPoints[i]);

                Handles.Label(worldPoints[i] + Vector3.up * creator.labelHeight, i.ToString(), _labelStyle);
            }

            EditorGUI.BeginChangeCheck();

            if(_selectedPoint > worldPoints.Count -1)
            {
                _selectedPoint = -1;
                return;
            }

            if(_selectedPoint == -1)
                return;

            var worldPosition = Handles.DoPositionHandle(worldPoints[_selectedPoint], Quaternion.identity);

            if (Application.isPlaying)
                return;

            if (EditorGUI.EndChangeCheck())
            {
                if(worldPosition != worldPoints[_selectedPoint])
                {
                    Undo.RecordObject(creator, "PipePoint position changed");

                    creator.ChangePointPosition(_selectedPoint, worldPosition);

                    EditorUtility.SetDirty(creator);
                }
            }
        }

        private void DrawPoint(int pointNumber, Vector3 pointPosition)
        {
            float size = HandleUtility.GetHandleSize(pointPosition);

            if(Handles.Button(pointPosition, Quaternion.identity, size * _handleSize, size * _pickSize, Handles.DotHandleCap))
                _selectedPoint = pointNumber;
        }
    }
}
