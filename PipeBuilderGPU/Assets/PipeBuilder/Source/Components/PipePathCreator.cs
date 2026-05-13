using System;
using System.Collections.Generic;
using UnityEngine;

namespace PipeBuilder
{
    public class PipePathCreator : MonoBehaviour
    {
        [SerializeField]
        private List<Vector3> _pipePoints;

        [SerializeField]
        private PipeData _pipeData;

#if UNITY_EDITOR
        [SerializeField, Space, Header("Debug Settings")]
        private bool _isDrawGizmo = true;

        [SerializeField]
        private Color32 _pipePathColor = Color.green;

        [SerializeField]
        public bool isDrawHandles = true;

        public int labelTextSize = 20;

        public float labelHeight = 0.2f;

        public List<Vector3> worldPoints;

        private Action<int> _recalculateWorldPointAction;
#endif

        public List<Vector3> PipePoints => _pipePoints;

        public PipeData PipeData => _pipeData;

#if UNITY_EDITOR
        private void Reset()
        {
            _pipePoints?.Clear();

            _pipePoints = new List<Vector3>()
            {
                transform.position,
                transform.position + Vector3.up
            };

            RecalculateWorldPoints();

            _pipeData = new PipeData()
            {
                radius = 0.5f,
                facesCount = 10,
                connectionOffsetCoef = 1.1f,
                connectionBeltsCount = 4
            };
        }

        public void RecalculateWorldPoints()
        {
            if(_pipePoints == null || _pipePoints.Count == 0)
                return;

            if (_pipePoints.Count == worldPoints.Count)
                _recalculateWorldPointAction = ReplaceWorldPoint;
            else
            {
                worldPoints.Clear();
                _recalculateWorldPointAction = AddWorldPoint;
            }

            for (int i = 0; i < _pipePoints.Count; i++)
                _recalculateWorldPointAction(i);
        }

        private void ReplaceWorldPoint(int index) => worldPoints[index] = transform.TransformPoint(_pipePoints[index]);

        private void AddWorldPoint(int index) => worldPoints.Add(transform.TransformPoint(_pipePoints[index]));

        public void ChangePointPosition(int pointNumber, Vector3 worldPosition)
        {
            if(pointNumber < _pipePoints.Count)
                _pipePoints[pointNumber] = transform.InverseTransformPoint(worldPosition);
        }

        private void OnDrawGizmos()
        {
            if(!_isDrawGizmo)
                return;

            if (worldPoints == null || worldPoints.Count == 0)
                return;

            Gizmos.color = _pipePathColor;

            for (int i = 0; i < worldPoints.Count; i++)
            {
                Gizmos.DrawSphere(worldPoints[i], _pipeData.radius);

                if(i != worldPoints.Count - 1)
                    Gizmos.DrawLine(worldPoints[i], worldPoints[i + 1]);

                if (worldPoints.Count > 2 && i > 0 && i < worldPoints.Count - 1)
                    DrawConnectionOffsetPoints(i);
            }
        }

        private void DrawConnectionOffsetPoints(int currentPoint)
        {
            var dir1 = worldPoints[currentPoint - 1] - worldPoints[currentPoint];
            var dir2 = worldPoints[currentPoint + 1] - worldPoints[currentPoint];

            var distance = _pipeData.radius * _pipeData.connectionOffsetCoef;

            Gizmos.DrawSphere(worldPoints[currentPoint] + dir1.normalized * distance, _pipeData.radius * 0.5f);
            Gizmos.DrawSphere(worldPoints[currentPoint] + dir2.normalized * distance, _pipeData.radius * 0.5f);
        }
#endif
    }
}
