struct CircleVertices
{
    float3 center;
    float3 direction;
};

struct Axes
{
    float3 xAxis;
    float3 yAxis;
};

struct ConnectionSegmentPoints
{
    float3 start;
    float3 center;
    float3 end;
};

struct TorusSegment
{
    float3 torusCenter;
    float torusRadius;
};

struct ConnectionSegment
{
    Axes axes;
    TorusSegment torusSegment;
    float radianPerSegment;
};

struct CircleSegment
{
    float3 center;
    Axes axes;
};

struct NearestConnectionVertex
{
    int vertexIndex;
    float distance;
};