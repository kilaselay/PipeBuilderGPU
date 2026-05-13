using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace PipeBuilder
{
	using strides = PipeStructsStrides;

	public class PipeBuilderGPU
	{
		private const float MinConnectionOffsetCoef = 1.1f;
		private const float ColinearThreshold = 0.001f;
		private const int MinConnectionBeltsCount = 3;

		private int _preventTwistingIterationID;

		private PipeKernelsID _kernelsID;

		private List<Vector3> _points;

		private ComputeShader _pipeComputeshader;

		#region Compute Buffers

		private ComputeBuffer _constantsBuffer;
		private ComputeBuffer _straightSegmentsPointsBuffer;
		private ComputeBuffer _connectionSegmentsPointsBuffer;
		private ComputeBuffer _connectionSegmentsBuffer;
		private ComputeBuffer _circleSegmentsBuffer;
		private ComputeBuffer _connectionsNearestVerticesBuffer;
		private ComputeBuffer _verticesBuffer;
		private ComputeBuffer _normalsBuffer;
		private ComputeBuffer _trianglesBuffer;

		#endregion

		public PipeBuilderGPU(ComputeShader pipeComputeShader)
		{
			_points = new List<Vector3>();
			_pipeComputeshader = pipeComputeShader;
			_preventTwistingIterationID = Shader.PropertyToID("preventTwistingIteration");

			_kernelsID = new PipeKernelsID(_pipeComputeshader);
			_kernelsID.FindKernels();
		}

        public Mesh Create(List<Vector3> points, PipeData pipeData)
        {
            Validate(points, ref pipeData);

			_points = points;

			var constants = GetConstants(
				points.Count,
				pipeData,
				out var verticesCount,
				out var trianglesCount);

			SetConstantBuffer(constants);

			CreateOutputBuffers(verticesCount, trianglesCount);

			GeneratePipe(constants);

			var mesh = GetMesh(verticesCount, trianglesCount);

			Release();

			return mesh;
		}

		#region Preparing For Creation Methods

		private ConstantsGPU GetConstants(
			int inputPointsCount,
            PipeData pipeData,
            out int verticesCount,
			out int trianglesCount)
		{
			float connectionOffset = 1f;
			int connectionSegmentsCount = 0;

			int straightSegmentsCount = inputPointsCount - 1;

			verticesCount = pipeData.facesCount * 2 * straightSegmentsCount;
			trianglesCount = straightSegmentsCount * pipeData.facesCount * 6;

			if (inputPointsCount > 2)
			{
				connectionOffset = CalculateConnectionOffset(pipeData.connectionOffsetCoef, pipeData.radius);

				connectionSegmentsCount = straightSegmentsCount - 1;
				int circleSegmentsCount = connectionSegmentsCount * pipeData.connectionBeltsCount - connectionSegmentsCount;
				int connectionVerticesCount = circleSegmentsCount * pipeData.facesCount;

				verticesCount += connectionVerticesCount;

				int connectionsTrianglesCount = connectionSegmentsCount * pipeData.connectionBeltsCount * pipeData.facesCount * 6;
				trianglesCount += connectionsTrianglesCount;
			}

			trianglesCount += (pipeData.facesCount - 2) * 6;

			var constants = new ConstantsGPU()
			{
				rad_conOff_maxFV = new Vector3(pipeData.radius, connectionOffset, float.MaxValue),

				facesCount = pipeData.facesCount,

				straightSegmentsCount = straightSegmentsCount,
				connectionSegmentsCount = connectionSegmentsCount,
				connectionBeltsCount = pipeData.connectionBeltsCount,
			};

			return constants;
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetConstantBuffer(ConstantsGPU constants)
		{
			var constantsGPU = new ConstantsGPU[] { constants };

			_constantsBuffer = new ComputeBuffer(1, strides.ConstantsGPUStride, ComputeBufferType.Constant);
			_constantsBuffer.SetData(constantsGPU);
			_pipeComputeshader.SetConstantBuffer(nameof(_constantsBuffer), _constantsBuffer, 0, _constantsBuffer.count * _constantsBuffer.stride);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateOutputBuffers(int verticesCount, int trianglesCount)
		{
			_verticesBuffer = new ComputeBuffer(verticesCount, UnsafeUtility.SizeOf<Vector3>());
			_normalsBuffer = new ComputeBuffer(verticesCount, UnsafeUtility.SizeOf<Vector3>());
			_trianglesBuffer = new ComputeBuffer(trianglesCount, sizeof(int));
		}

        #region Validation Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Validate(List<Vector3> points, ref PipeData pipeData)
		{
            CheckMinPointsCount(points.Count);
            CheckMinFacesCount(ref pipeData.facesCount);
            CheckMinConnectionBeltsCount(ref pipeData.connectionBeltsCount);

            ExcludeIncorrectPoints(points);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckMinPointsCount(int pointsCount)
		{
			if (!IsMinPointsCount(pointsCount))
				throw new Exception($"The number of points to build is less than 2!\nInput points count: {pointsCount}");
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckMinFacesCount(ref int facesCount)
		{
			if (!IsMinFacesCount(facesCount))
			{
				Debug.LogError($"The number of faces must be at least 3!\nFaces count: {facesCount}");

				facesCount = 3;
			}
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckMinConnectionBeltsCount(ref int connectionBeltsCount)
		{
			if (connectionBeltsCount < MinConnectionBeltsCount)
				connectionBeltsCount = MinConnectionBeltsCount;
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsMinPointsCount(int pointsCount) => pointsCount > 1 ? true : false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsMinFacesCount(int facesCount) => facesCount > 2 ? true : false;

		private void ExcludeIncorrectPoints(List<Vector3> inputPoints)
		{
			List<int> pointsToRemove = new List<int>();

			RemoveDuplicatePoints(inputPoints, pointsToRemove);

			pointsToRemove.Clear();

			CheckMinPointsCount(inputPoints.Count);

			RemoveCollinearPoints(inputPoints, pointsToRemove);

			CheckMinPointsCount(inputPoints.Count);
		}

		private void RemoveDuplicatePoints(List<Vector3> points, List<int> pointsToRemove)
		{
			for (int i = 0; i < points.Count - 1; i++)
			{
				Vector3 point1 = points[i];
				Vector3 point2 = points[i + 1];

				if (point1 == point2)
					pointsToRemove.Add(i + 1);
			}

			RemoveIncorrectPoints(points, pointsToRemove, "Duplicate point removed: ");
		}

		private void RemoveCollinearPoints(List<Vector3> points, List<int> pointsToRemove)
		{
			for (int i = 0; i < points.Count - 2; i++)
			{
				Vector3 point1 = points[i];
				Vector3 point2 = points[i + 1];
				Vector3 point3 = points[i + 2];

				Vector3 direction1 = point2 - point1;
				Vector3 direction2 = point3 - point2;

				if (Vector3.Distance(direction1.normalized, direction2.normalized) < ColinearThreshold)
					pointsToRemove.Add(i + 1);
			}

			pointsToRemove.Reverse();

			RemoveIncorrectPoints(points, pointsToRemove, "Collinear point removed: ");
		}

		private void RemoveIncorrectPoints(List<Vector3> points, List<int> pointsToRemove, string warningMessage)
		{
			if (pointsToRemove.Count > 0)
			{
				Vector3 removedPoint;

				foreach (int index in pointsToRemove)
				{
					removedPoint = points[index];
					points.RemoveAt(index);
					Debug.LogWarning(warningMessage + removedPoint);
				}
			}
		}

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float CalculateConnectionOffset(float connectionOffsetCoef, float radius)
		{
			if (radius <= 0)
				throw new Exception($"The radius must be a positive number!\nThe entered radius: {radius}");

			if (connectionOffsetCoef < MinConnectionOffsetCoef)
				connectionOffsetCoef = MinConnectionOffsetCoef;

			return radius * connectionOffsetCoef;
		}

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GeneratePipe(ConstantsGPU constants)
		{
			CreateStraightSegments(constants);

			if (constants.connectionSegmentsCount > 0)
				CreateConnections(constants);

			CreateEndCaps(constants.facesCount);
		}

		#region Creating Straight Segments Methods

		private void CreateStraightSegments(ConstantsGPU constants)
		{
			var calcStraightVertID = _kernelsID.CalculateStraightVerticesID;
			var setStraightTrianglesID = _kernelsID.SetStraightTrianglesID;

			_straightSegmentsPointsBuffer = new ComputeBuffer(constants.straightSegmentsCount * 2, strides.CircleVerticesStride);

			SetStraightSegmentsBuffers(calcStraightVertID, setStraightTrianglesID);

			CalculateStraightVertices(calcStraightVertID, constants);

			_pipeComputeshader.Dispatch(setStraightTrianglesID, constants.straightSegmentsCount, constants.facesCount, 1);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetStraightSegmentsBuffers(int calcStraightVertID, int setStraightTrianglesID)
		{
			_pipeComputeshader.SetBuffer(calcStraightVertID, nameof(_straightSegmentsPointsBuffer), _straightSegmentsPointsBuffer);

			_pipeComputeshader.SetBuffer(calcStraightVertID, nameof(_verticesBuffer), _verticesBuffer);
			_pipeComputeshader.SetBuffer(calcStraightVertID, nameof(_normalsBuffer), _normalsBuffer);

			_pipeComputeshader.SetBuffer(setStraightTrianglesID, nameof(_trianglesBuffer), _trianglesBuffer);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CalculateStraightVertices(int calcStraightVertID, ConstantsGPU constants)
		{
			var straightCirclesVertices = GetStraightCirclesVertices(constants.straightSegmentsCount, constants.rad_conOff_maxFV.y);

			_straightSegmentsPointsBuffer.SetData(straightCirclesVertices);

			_pipeComputeshader.Dispatch(calcStraightVertID, constants.straightSegmentsCount, constants.facesCount, 1);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private CircleVertices[] GetStraightCirclesVertices(int straightSegmentsCount, float connectionOffset)
		{
			var straightCirclesVertices = new CircleVertices[straightSegmentsCount * 2];

			Vector3 point;
			Vector3 endPoint;
			Vector3 direction;

			for (int i = 0; i < straightSegmentsCount; i++)
			{
				point = _points[i];
				endPoint = _points[i + 1];
				direction = (endPoint - point).normalized;

				if (i > 0)
					point = point + direction * connectionOffset;

				if (i < straightSegmentsCount - 1)
					endPoint = endPoint - direction * connectionOffset;

				straightCirclesVertices[i * 2] = new CircleVertices { center = point, direction = direction };
				straightCirclesVertices[i * 2 + 1] = new CircleVertices { center = endPoint, direction = direction };
			}

			return straightCirclesVertices;
		}

		#endregion

		#region Creating Connection Segments Methods

		private void CreateConnections(ConstantsGPU constants)
		{
			CreateConnectionVertices(constants);
			CreateConnectionTriangles(constants);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateConnectionVertices(ConstantsGPU constants)
		{
			var calcConSegID = _kernelsID.CalculateConnectionsSegmentsID;
			var calcCircleSegID = _kernelsID.CalculateCircleSegmentsID;
			var calcConVertID = _kernelsID.CalculateConnectionsVerticesID;

			var circleSegmentsCount = constants.connectionSegmentsCount * constants.connectionBeltsCount - constants.connectionSegmentsCount;

			CreateConnectionsVerticesBuffers(constants.connectionSegmentsCount, circleSegmentsCount);
			SetConnectionsVerticesBuffers(calcConSegID, calcCircleSegID, calcConVertID);

			CalculateConnectionsSegments(calcConSegID, constants.connectionSegmentsCount);

			_pipeComputeshader.Dispatch(calcCircleSegID, constants.connectionSegmentsCount, constants.connectionBeltsCount, 1);

			_pipeComputeshader.Dispatch(calcConVertID, circleSegmentsCount, constants.facesCount, 1);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateConnectionsVerticesBuffers(int connectionSegmentsCount, int circleSegmentsCount)
		{
			_connectionSegmentsPointsBuffer = new ComputeBuffer(connectionSegmentsCount, strides.ConnectionSegmentPointsStride);
			_connectionSegmentsBuffer = new ComputeBuffer(connectionSegmentsCount, strides.ConnectionSegmentStride);

			_circleSegmentsBuffer = new ComputeBuffer(circleSegmentsCount, strides.CircleSegmentStride);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetConnectionsVerticesBuffers(int calcConSegID, int calcCircleSegID, int calcConVertID)
		{
			_pipeComputeshader.SetBuffer(calcConSegID, nameof(_connectionSegmentsPointsBuffer), _connectionSegmentsPointsBuffer);
			_pipeComputeshader.SetBuffer(calcConSegID, nameof(_connectionSegmentsBuffer), _connectionSegmentsBuffer);

			_pipeComputeshader.SetBuffer(calcCircleSegID, nameof(_connectionSegmentsBuffer), _connectionSegmentsBuffer);
			_pipeComputeshader.SetBuffer(calcCircleSegID, nameof(_circleSegmentsBuffer), _circleSegmentsBuffer);

			_pipeComputeshader.SetBuffer(calcConVertID, nameof(_circleSegmentsBuffer), _circleSegmentsBuffer);
			_pipeComputeshader.SetBuffer(calcConVertID, nameof(_verticesBuffer), _verticesBuffer);
			_pipeComputeshader.SetBuffer(calcConVertID, nameof(_normalsBuffer), _normalsBuffer);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CalculateConnectionsSegments(int calcConSegID, int connectionSegmentsCount)
		{
			var segments = new ConnectionSegmentPoints[connectionSegmentsCount];

			for (int i = 0; i < connectionSegmentsCount; i++)
			{
				var segment = new ConnectionSegmentPoints
				{
					start = _points[i],
					center = _points[i + 1],
					end = _points[i + 2]
				};

				segments[i] = segment;
			}

			_connectionSegmentsPointsBuffer.SetData(segments);

			_pipeComputeshader.Dispatch(calcConSegID, connectionSegmentsCount, 1, 1);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateConnectionTriangles(ConstantsGPU constants)
		{
			var setDefNearVertID = _kernelsID.SetDefaultNearestVerticesID;
			var preventTwistID = _kernelsID.PreventTwistingID;
			var setConTrianglesID = _kernelsID.SetConnectionsTrianglesID;

			int allConnectionVerticesCount = constants.connectionSegmentsCount * constants.connectionBeltsCount * constants.facesCount;

			_connectionsNearestVerticesBuffer = new ComputeBuffer(allConnectionVerticesCount, strides.NearestConnectionVertexStride);

			SetConnectionTrianglesBuffers(setDefNearVertID, preventTwistID, setConTrianglesID);

			_pipeComputeshader.Dispatch(setDefNearVertID, constants.connectionSegmentsCount, constants.connectionBeltsCount, constants.facesCount);

			PreventTwisting(preventTwistID, constants);

			_pipeComputeshader.Dispatch(setConTrianglesID, constants.connectionSegmentsCount, constants.connectionBeltsCount, constants.facesCount);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetConnectionTrianglesBuffers(int setDefNearVertID, int preventTwistID, int setConTrianglesID)
		{
			_pipeComputeshader.SetBuffer(setDefNearVertID, nameof(_connectionsNearestVerticesBuffer), _connectionsNearestVerticesBuffer);

			_pipeComputeshader.SetBuffer(preventTwistID, nameof(_connectionsNearestVerticesBuffer), _connectionsNearestVerticesBuffer);
			_pipeComputeshader.SetBuffer(preventTwistID, nameof(_verticesBuffer), _verticesBuffer);

			_pipeComputeshader.SetBuffer(setConTrianglesID, nameof(_trianglesBuffer), _trianglesBuffer);
			_pipeComputeshader.SetBuffer(setConTrianglesID, nameof(_connectionsNearestVerticesBuffer), _connectionsNearestVerticesBuffer);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PreventTwisting(int preventTwistID, ConstantsGPU constants)
		{
			for (int i = 0; i < constants.facesCount; i++)
			{
				_pipeComputeshader.SetInt(_preventTwistingIterationID, i);

				_pipeComputeshader.Dispatch(preventTwistID, constants.connectionSegmentsCount, constants.connectionBeltsCount, constants.facesCount);
			}
		}

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateEndCaps(int facesCount)
		{
			var setEndCapsTrianglesID = _kernelsID.SetEndCapsTrianglesID;

			_pipeComputeshader.SetBuffer(setEndCapsTrianglesID, nameof(_trianglesBuffer), _trianglesBuffer);

			_pipeComputeshader.Dispatch(setEndCapsTrianglesID, facesCount - 2, 1, 1);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Mesh GetMesh(int verticesCount, int trianglesCount)
		{
			var vertices = new Vector3[verticesCount];
			_verticesBuffer.GetData(vertices);

			var normals = new Vector3[verticesCount];
			_normalsBuffer.GetData(normals);

			var triangles = new int[trianglesCount];
			_trianglesBuffer.GetData(triangles);

			Mesh mesh = new Mesh()
			{
				vertices = vertices,
				normals = normals,
				triangles = triangles
			};

			return mesh;
		}

		private void Release()
		{
			_constantsBuffer?.Release();

			_straightSegmentsPointsBuffer?.Release();

			_connectionSegmentsPointsBuffer?.Release();
			_connectionSegmentsBuffer?.Release();
			_circleSegmentsBuffer?.Release();
			_connectionsNearestVerticesBuffer?.Release();

			_verticesBuffer?.Release();
			_normalsBuffer?.Release();

			_trianglesBuffer?.Release();
		}
	}
}
