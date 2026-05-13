#ifndef MATH_FOR_PIPE_INCLUDED
#define MATH_FOR_PIPE_INCLUDED

#include "PipeStructs.hlsl"

// NORMALIZED VECTORS:

// Equivalent to Vector3.up in Unity
// Returns a normalized vector directed up along the Y axis
float3 UP()
{
    return float3(0.0, 1.0, 0.0);
}

// Equivalent to Vector3.forward in Unity
// Returns a normalized vector directed forward along the Z axis
float3 FORWARD()
{
    return float3(0.0, 0.0, 1.0);
}

// END NORMALIZED VECTORS
//////////////////////////////////////////////////////

// AUXILIARY MATHEMATICAL FUNCTIONS:

// Returns the vector projection of a onto b
float3 Project(float3 a, float3 b)
{
    float m = length(b);
    return dot(a, b) / (m * m) * b;
}

// Returns the vector rejection of a on b
float3 Reject(float3 a, float3 b)
{
    return a - Project(a, b);
}

// The Gram-Schmidt process for 2 vectors
// Normalize "normal", normalizes "tangent" and makes it orthogonal to "normal"
// Returns an orthogonal and normalized coordinate system of 2 axes
Axes OrthoNormalize(float3 normal, float3 tangent)
{
    normal = normalize(normal);

    tangent = Reject(tangent, normal);
    tangent = normalize(tangent);
	
    Axes axes;
	
    axes.xAxis = normal;
    axes.yAxis = tangent;
	
    return axes;
}

// The Gram-Schmidt process for 3 vectors
// Normalize "normal", normalizes "tangent" and makes it orthogonal to "normal"
// Normalizes "binormal" and makes it orthogonal to both "normal" and "tangent"
// Returns an orthogonal and normalized coordinate system of 2 axes
Axes OrthoNormalize(float3 normal, float3 tangent, float3 binormal)
{
    normal = normalize(normal);
	
    tangent = Reject(tangent, normal);
    tangent = normalize(tangent);
	
    binormal = Reject(binormal, tangent);
    binormal = Reject(binormal, normal);
    binormal = normalize(binormal);
	
    Axes axes;
	
    axes.xAxis = tangent;
    axes.yAxis = binormal;
	
    return axes;
}

// Excludes the presence of a zero scalar in the coordinates of the direction vector
float3 GetNonZeroDirection(float3 direction)
{
    direction.x = any(direction.x) * direction.x + 0.000001;
    direction.y = any(direction.y) * direction.y + 0.000001;
    direction.z = any(direction.z) * direction.z + 0.000001;
	
    return direction;
}

// Returns the normalized local coordinate system for forming a circle of vertices
Axes GetCirclesAxes(float3 direction)
{
    direction = GetNonZeroDirection(direction);
	
    float3 yAxis = float3(-sign(dot(FORWARD(), direction)), 0.0, 0.0);

    return OrthoNormalize(direction, UP(), yAxis);
}

// END AUXILIARY MATHEMATICAL FUNCTIONS
//////////////////////////////////////////////////////

// FUNCTIONS FOR PIPE CONNECTION SEGMENTS

// Calculates center of torus
float3 CalculateTorusCenter(float3 startLinePoint, float3 startDirection, float3 endLinePoint, float3 endDirection)
{
    float startDirectionDot = dot(startDirection, startDirection);
    float middleDirectionDot = dot(startDirection, endDirection);
    float endDirectionDot = dot(endDirection, endDirection);

    float directionDot = startDirectionDot * endDirectionDot - middleDirectionDot * middleDirectionDot;

    float3 middlePoint = startLinePoint - endLinePoint;

    float startDot = dot(startDirection, middlePoint);
    float endDot = dot(endDirection, middlePoint);

    float startOffsetPoint = (middleDirectionDot * endDot - startDot * endDirectionDot) / directionDot;
    float endOffsetPoint = (startDirectionDot * endDot - startDot * middleDirectionDot) / directionDot;

    float3 closestStartPointLine = startLinePoint + startDirection * startOffsetPoint;
    float3 closestEndPointLine = endLinePoint + endDirection * endOffsetPoint;

    return (closestStartPointLine + closestEndPointLine) * 0.5;
}

// Returns the center of the torus for building a connection segment
float3 GetTorusCenter(float3 startPoint, float3 startOffset, float3 endPoint, float3 endOffset)
{
    float3 perpendicularToBoth = cross(startOffset, endOffset);
    float3 startDirection = normalize(cross(perpendicularToBoth, startOffset));
    float3 endDirection = normalize(cross(perpendicularToBoth, endOffset));

    return CalculateTorusCenter(startPoint, startDirection, endPoint, endDirection);
}

// Returns the angle of the torus part in radians
float GetTorusAngle(float3 from, float3 to)
{
    return acos(dot(from, to) / sqrt(dot(from, from) * dot(to, to)));
}

// END FUNCTIONS FOR PIPE CONNECTION SEGMENTS

// Returns the opposite value of 0 or 1
int Inverse(int value)
{
    return 1 - value;
}

#endif