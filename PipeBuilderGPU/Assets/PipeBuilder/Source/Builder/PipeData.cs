using UnityEngine;
using System;

namespace PipeBuilder
{
    [Serializable]
    public struct PipeData
    {
        [Min(0.001f)]
        public float radius;

        [Min(3)]
        public int facesCount;

        [Min(1.1f)]
        public float connectionOffsetCoef;

        [Min(3)]
        public int connectionBeltsCount;
    }
}
