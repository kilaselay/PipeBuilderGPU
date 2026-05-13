using UnityEngine;

namespace PipeBuilder
{
    internal class PipeKernelsID
    {
		private ComputeShader _pipeComputeShader;

        internal int CalculateStraightVerticesID { get; private set; }

        internal int SetStraightTrianglesID { get; private set; }

        internal int CalculateConnectionsSegmentsID { get; private set; }

        internal int CalculateCircleSegmentsID { get; private set; }

        internal int CalculateConnectionsVerticesID { get; private set; }

        internal int SetDefaultNearestVerticesID { get; private set; }

        internal int PreventTwistingID { get; private set; }

        internal int SetConnectionsTrianglesID { get; private set; }

        internal int SetEndCapsTrianglesID { get; private set; }

        internal PipeKernelsID(ComputeShader pipeComputeShader) => _pipeComputeShader = pipeComputeShader;

        internal void FindKernels()
		{
            CalculateStraightVerticesID = _pipeComputeShader.FindKernel(GetKernelName(nameof(CalculateStraightVerticesID)));
            SetStraightTrianglesID = _pipeComputeShader.FindKernel(GetKernelName(nameof(SetStraightTrianglesID)));

            CalculateConnectionsSegmentsID = _pipeComputeShader.FindKernel(GetKernelName(nameof(CalculateConnectionsSegmentsID)));
            CalculateCircleSegmentsID = _pipeComputeShader.FindKernel(GetKernelName(nameof(CalculateCircleSegmentsID)));
            CalculateConnectionsVerticesID = _pipeComputeShader.FindKernel(GetKernelName(nameof(CalculateConnectionsVerticesID)));
            SetDefaultNearestVerticesID = _pipeComputeShader.FindKernel(GetKernelName(nameof(SetDefaultNearestVerticesID)));
            PreventTwistingID = _pipeComputeShader.FindKernel(GetKernelName(nameof(PreventTwistingID)));
            SetConnectionsTrianglesID = _pipeComputeShader.FindKernel(GetKernelName(nameof(SetConnectionsTrianglesID)));

            SetEndCapsTrianglesID = _pipeComputeShader.FindKernel(GetKernelName(nameof(SetEndCapsTrianglesID)));

            string GetKernelName(string kernelNameID) => kernelNameID.Remove(kernelNameID.Length - 2, 2);
        }
	}
}
