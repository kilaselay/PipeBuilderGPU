Additional pages:

[ReadMe.ru.md](README.ru.md)

---

# PipeBuilderGPU

![PipeBuilder-generated pipe models](Images/Preview.png)

---

**PipeBuilder** allows you to procedurally create a pipe geometry using both the editor tools (*PipePathCreator*, *PipeGenerator*) so it is during execution (*PipeBuilderGPU*), performing all mathematical calculations in parallel on the GPU using compute shaders. This approach significantly reduces the time it takes to create a Mesh at runtime.

*PipeBuilderGPU* accepts the *PipeCompute* computational shader file into the constructor and contains a single *Create* method that accepts a list of points (the path of the pipe) and a structure with pipe parameters *PipeData* (radius, number of faces, indentation coefficient of the connecting segment, number of belts of the connecting segment), and returns a Mesh containing vertices, normals and triangles.

```c#
public Mesh Create(List<Vector3> points, PipeData pipeData)
```
```c#
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
```
During creation, the received data will be automatically validated to meet the minimum requirements of the input parameters. Duplicate and collinear points will also be automatically excluded. If you need to build only a straight pipe segment and only 2 points are used, the algorithm will work faster, since it will not use more complex calculations to build connecting segments.

During construction, unnecessary points and hidden polygons are not created on the borders of straight and connecting pipe segments. The plugs at the ends of the pipes are formed without creating additional central vertices, so they contain fewer triangles. All buffers allocated for calculations are automatically released upon completion of the *Create* method and it can be called again with other input data.

## PipePathCreator

To create a pipe geometry in the editor, there are only 2 components, one of which is used to build a path:

![Path сreation tool inspector](Images/PipePathCreatorInspector.png)

The tool contains a list of pipe points, in local coordinates, which are recalculated relative to the GameObject itself, and there is also a *PipeData* structure with pipe parameters and editor debugging settings below.

![A tool for creating a path on the stage](Images/Tool.png)

The positions of the points can be changed both using the list in the inspector and on the scene by clicking on the white square with the point number. The selected point activate the built-in Unity position change tool. Each change can be undone by using the Undo command in the editor, and Unity will notify you of unsaved changes when you exit the stage.

The size of the points corresponds to the configured radius. The smaller dots indicate the boundaries where the edges of the connecting segments will be located in accordance with the connection offset coefficients.

In the debugging settings, you can enable/disable the display of Gizmo and the operation of the tool on the scene (including active points and vertex numbers above them), adjust the color of Gismo, as well as adjust the size of the displayed point numbers on the stage and their height above the position.

## PipeGenerator

The second component is used to create an object with a Mesh pipe. When located on the object, it requires the *PipePathCreator* component, so it will create it automatically if it is not present.

![Pipe generator inspector window](Images/PipeGeneratorInspector.png)

Here you need to specify the resource of the compute shader "PipeCompute", the material that will be applied to the model, and the name of the object being created. Also, when creating it, you can specify whether the model bounds and tangents need to be recalculated. The last parameter is responsible for whether the object needs to be generated during execution. You can disable this option if the component is required to create a model in the editor.

To create an object with a model in the editor, right-click to open the context menu of the component and select the desired option in the list below:

![Context menu of the pipe generator](Images/GeneratePipeMenu.png)

You can simply create a pipe object on the stage, or create and save a Mesh. The model will be saved as *.asset* in the root of the assets folder and will have the name specified in the component, which will be sent a message to the Unity console.

## Pipe creation parameters

Let's consider the influence of the generation parameters on the final model.

### FacesCount

The parameter allows you to set the number of pipe faces, which is its degree of smoothing:

![The difference is the number of faces](Images/FacesCount.png)

The minimum number of faces is 3. The number depends on the desired shape/smoothness of the model.

### ConnectionOffsetCoef

This parameter affects how far away from the bending point the belts of pipe connection polygons can be formed:

![The difference in the offset coefficients of the pipe connections segments](Images/ConnectionOffsetCoef.png)

The greater the connection offset coefficient of the pipe, the smoother its bending is obtained. The minimum value is 1.1.

### ConnectionBeltsCount

The parameter affects the number of polygon belts in the connection segment of the pipe, the degree of smoothness of the bend:

![The difference is the number of connection belts](Images/ConnectionBeltsCount.png)

The more polygon belts there are in the connection, the smoother the bend looks. The minimum quantity is 3. In each belt, the number of polygons is equal to twice the number of faces.

---

The solution works on any discrete GPU, as well as with DirectX 12.
