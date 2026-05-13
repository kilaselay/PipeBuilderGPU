using System.Runtime.InteropServices;
using UnityEngine;

namespace PipeBuilder
{
    /// <summary>
    /// Structure for declaring a constant buffer in the compute shader
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
	public struct ConstantsGPU
	{
        /// <summary>
        /// A vector containing 3 float constants:
        ///<br> x - pipe radius </br>
        ///<br> y - pipe connection offset </br>
        ///<br> z - float.MaxValue </br>
        /// </summary>
        public Vector3 rad_conOff_maxFV;

		public int facesCount;
		public int straightSegmentsCount;
		public int connectionSegmentsCount;
		public int connectionBeltsCount;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct CircleVertices
	{
		public Vector3 center;
		public Vector3 direction;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Axes
	{
		public Vector3 xAxis;
		public Vector3 yAxis;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct ConnectionSegmentPoints
	{
		public Vector3 start;
		public Vector3 center;
		public Vector3 end;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct TorusSegment
	{
		public Vector3 torusCenter;
		public float torusRadius;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct ConnectionSegment
	{
		public Axes axes;
		public TorusSegment torusSegment;
		public float radianPerSegment;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct CircleSegment
	{
		public Vector3 center;
		public Axes axes;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct NearestConnectionVertex
	{
		public int vertexIndex;
		public float distance;
	}
}
