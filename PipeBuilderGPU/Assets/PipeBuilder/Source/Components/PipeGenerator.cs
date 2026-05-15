using UnityEditor;
using UnityEngine;
using System;

namespace PipeBuilder
{
    [RequireComponent(typeof(PipePathCreator))]
    public class PipeGenerator : MonoBehaviour
    {
        [SerializeField]
        private ComputeShader _pipesShader;

        [SerializeField]
        private Material _pipeMaterial;

        [SerializeField]
        private string _pipeName = "Pipe";

        [SerializeField]
        private bool _isRecalculateBounds = true;

        [SerializeField]
        private bool _isRecalculateTangents = false;

        [Space, SerializeField]
        private bool _isGenerateInPlayMode = true;

        private PipeBuilderGPU _pipeBuilderGPU;

        private void Awake()
        {
            if(_isGenerateInPlayMode)
                Initialize();
        }

        private void Start()
        {
            if (_isGenerateInPlayMode)
                Generate();
        }

        private void Initialize()
        {
            _pipesShader = Instantiate(_pipesShader);

            _pipeBuilderGPU = new PipeBuilderGPU(_pipesShader);
        }

        private void Generate()
        {
            var pipeMesh = CreatePipeMesh();
            CreatePipeObject(pipeMesh);
        }

        private Mesh CreatePipeMesh()
        {
            var pipeCreator = GetComponent<PipePathCreator>();

            if (pipeCreator == null)
                throw new Exception($"{nameof(PipePathCreator)} is null");

            var mesh = _pipeBuilderGPU.Create(pipeCreator.PipePoints, pipeCreator.PipeData);

            if (_isRecalculateBounds)
                mesh.RecalculateBounds();

            if (_isRecalculateTangents)
                mesh.RecalculateTangents();

            mesh.name = _pipeName;

            return mesh;
        }

        private void CreatePipeObject(Mesh pipeMesh)
        {
            var pipeOjbect = new GameObject(_pipeName);

            pipeOjbect.transform.position = transform.position;
            pipeOjbect.transform.rotation = transform.rotation;
            pipeOjbect.transform.localScale = transform.localScale;

            var meshFilter = pipeOjbect.AddComponent<MeshFilter>();
            meshFilter.mesh = pipeMesh;

            var meshRenderer = pipeOjbect.AddComponent<MeshRenderer>();
            meshRenderer.material = _pipeMaterial;
        }

#if UNITY_EDITOR

        [ContextMenu("Generate pipe")]
        private void GeneratePipe()
        {
            Initialize();
            Generate();
        }

        [ContextMenu("Generate pipe and save mesh")]
        private void GeneratePipeAndSave()
        {
            Initialize();
            GenerateAndSave();
        }

        private void GenerateAndSave()
        {
            var pipeMesh = CreatePipeMesh();

            var path = $"Assets/{_pipeName}.asset";

            AssetDatabase.CreateAsset(pipeMesh, path);

            Debug.Log($"<color=lime>[PipeGenerator]</color> Mesh saved in \"{path}\"");

            CreatePipeObject(pipeMesh);
        }
#endif

    }
}
