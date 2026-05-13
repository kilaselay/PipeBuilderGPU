using Unity.Collections.LowLevel.Unsafe;

namespace PipeBuilder
{
	internal readonly struct PipeStructsStrides
	{
        internal static readonly int ConstantsGPUStride = UnsafeUtility.SizeOf<ConstantsGPU>();

        internal static readonly int CircleVerticesStride = UnsafeUtility.SizeOf<CircleVertices>();

        internal static readonly int AxesStride = UnsafeUtility.SizeOf<Axes>();

        internal static readonly int ConnectionSegmentPointsStride = UnsafeUtility.SizeOf<ConnectionSegmentPoints>();

        internal static readonly int TorusSegmentStride = UnsafeUtility.SizeOf<TorusSegment>();

        internal static readonly int ConnectionSegmentStride = UnsafeUtility.SizeOf<ConnectionSegment>();

        internal static readonly int CircleSegmentStride = UnsafeUtility.SizeOf<CircleSegment>();

        internal static readonly int NearestConnectionVertexStride = UnsafeUtility.SizeOf<NearestConnectionVertex>();
	}
}
