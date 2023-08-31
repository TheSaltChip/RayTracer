using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Attributes;
using DataTypes;
using Helpers;
using RayTracingObjects;
using UnityEditor;
using UnityEngine;
using Util.Bvh;
using Rect = DataTypes.Rect;

namespace Managers
{
    [ExecuteAlways, ImageEffectAllowedInSceneView]
    public class RayTracingManager : MonoBehaviour
    {
        // ReSharper disable once InconsistentNaming
        private enum BVHStatus
        {
            Disabled,
            AllInOne,
            Improved
        }

        #region SHADER_PROPERTIES

        // @formatter:off
        private static readonly int Frame = Shader.PropertyToID("Frame");
        private static readonly int Rects = Shader.PropertyToID("Rects");
        private static readonly int Spheres = Shader.PropertyToID("Spheres");
        private static readonly int BoxInfos = Shader.PropertyToID("BoxInfos");
        private static readonly int BoxSides = Shader.PropertyToID("BoxSides");
        private static readonly int BvhNodes = Shader.PropertyToID("BvhNodes");
        private static readonly int FogBoxes = Shader.PropertyToID("FogBoxes");
        private static readonly int NumRects = Shader.PropertyToID("NumRects");
        private static readonly int SunFocus = Shader.PropertyToID("SunFocus");
        private static readonly int NumMeshes = Shader.PropertyToID("NumMeshes");
        private static readonly int TlasNodes = Shader.PropertyToID("TlasNodes");
        private static readonly int Triangles = Shader.PropertyToID("Triangles");
        private static readonly int FogSpheres = Shader.PropertyToID("FogSpheres");
        private static readonly int MainOldTex = Shader.PropertyToID("MainOldTex");
        private static readonly int NumSpheres = Shader.PropertyToID("NumSpheres");
        private static readonly int ViewParams = Shader.PropertyToID("ViewParams");
        private static readonly int AllMeshInfo = Shader.PropertyToID("AllMeshInfo");
        private static readonly int GroundColor = Shader.PropertyToID("GroundColor");
        private static readonly int NumBoxInfos = Shader.PropertyToID("NumBoxInfos");
        private static readonly int NumBoxSides = Shader.PropertyToID("NumBoxSides");
        private static readonly int NumFogBoxes = Shader.PropertyToID("NumFogBoxes");
        private static readonly int SunIntensity = Shader.PropertyToID("SunIntensity");
        private static readonly int BoundingBoxes = Shader.PropertyToID("BoundingBoxes");
        private static readonly int NumFogSpheres = Shader.PropertyToID("NumFogSpheres");
        private static readonly int MaxBounceCount = Shader.PropertyToID("MaxBounceCount");
        private static readonly int SkyColorZenith = Shader.PropertyToID("SkyColorZenith");
        private static readonly int DivergeStrength = Shader.PropertyToID("DivergeStrength");
        private static readonly int NumRaysPerPixel = Shader.PropertyToID("NumRaysPerPixel");
        private static readonly int SkyColorHorizon = Shader.PropertyToID("SkyColorHorizon");
        private static readonly int TriangleIndices = Shader.PropertyToID("TriangleIndices");
        private static readonly int NumBoundingBoxes = Shader.PropertyToID("NumBoundingBoxes");
        private static readonly int NumRenderedFrames = Shader.PropertyToID("NumRenderedFrames");
        private static readonly int BoundingBoxIndices = Shader.PropertyToID("BoundingBoxIndices");
        private static readonly int EnvironmentEnabled = Shader.PropertyToID("EnvironmentEnabled");
        private static readonly int CamLocalToWorldMatrix = Shader.PropertyToID("CamLocalToWorldMatrix");
        // @formatter:on

        #endregion

        #region Serialized fields

        [Header("Info"), SerializeField, ReadOnly]
        private int frameCount;

        [SerializeField, ReadOnly] private int totalAmountOfRaysPerPixel;
        [SerializeField, ReadOnly] private int amountOfPictures;
        [SerializeField] private string folderName;
        [SerializeField, Min(0)] private int raysPerPixelPerImage;
        [SerializeField] private bool saveImageSequence;
        [SerializeField] private bool saveThisFrame;
        [SerializeField] private bool stopRender;
        [SerializeField] private bool restartRender;

        [Space, Header("Ray Tracing Settings"), SerializeField, Range(0, 100)]
        private int maxBounceCount;

        [SerializeField, Range(1, 200)] private int numRaysPerPixel;
        [SerializeField, Range(0, 5)] private float divergeStrength;

        [Space, Header("Setup Settings"), SerializeField]
        private bool useShaderInSceneView;

        [SerializeField] private bool drawBvh;
        [SerializeField] private bool createBvh;
        [SerializeField] private BVHStatus bvhStatus;

        [SerializeField] private Shader rayTracingShader;
        [SerializeField] private Shader improvedRayTracingShader;
        [SerializeField] private Shader combineShader;
        [SerializeField] private ChangerManager changer;

        [SerializeField] private bool environmentEnabled;
        [SerializeField] private Color skyColorHorizon;
        [SerializeField] private Color skyColorZenith;

        [SerializeField] private Color groundColor;
        [SerializeField, Range(1, 50)] private float sunFocus = 1;
        [SerializeField, Range(0, 10)] private float sunIntensity = 1;

        #endregion

        #region Private fields

        private Material _rayTracingMaterial;
        private Material _combiningMaterial;

        private GraphicsBuffer _sphereBuffer;
        private GraphicsBuffer _fogSphereBuffer;
        private GraphicsBuffer _rectBuffer;
        private GraphicsBuffer _fogBoxBuffer;
        private GraphicsBuffer _boxSideBuffer;
        private GraphicsBuffer _boxInfoBuffer;
        private GraphicsBuffer _triangleBuffer;
        private GraphicsBuffer _triangleIndexBuffer;
        private GraphicsBuffer _bvhBuffer;
        private GraphicsBuffer _tlasBuffer;
        private GraphicsBuffer _meshInfoBuffer;
        private GraphicsBuffer _boundingBoxes;
        private GraphicsBuffer _boundingBoxIndices;

        private RenderTexture _resultTexture;
        private bool _wasLastFrameRayTraced;
        private bool _startNewRender;

        private List<Triangle> _allTriangles;
        private List<MeshInfo> _allMeshInfo;
        private List<BoxInfo> _boxInfos;
        private List<BoxSide> _sides;
        private List<BaseObject> _baseObjects;

        private Stopwatch _stopwatch;
        private List<TimeSpan> _renderTimes;

        private BoundingVolumeHierarchy _bvh;
        private MeshObject[] _meshObjects;
        private FogBoxObject[] _fogBoxObjects;
        private BoxObject[] _boxObjects;
        private RectObject[] _rectObjects;
        private FogSphereObject[] _fogSphereObjects;
        private SphereObject[] _sphereObjects;
        private List<int> _indices;
        private List<BoundingBox> _boundingBoxArray;

        private ImprovedBVH _improvedBvh;

        #endregion

        #region Unity event functions

        private void Start()
        {
            frameCount = 0;
            _bvh = new BoundingVolumeHierarchy();
            _baseObjects?.Clear();
        }

        private void OnEnable()
        {
            totalAmountOfRaysPerPixel = 0;
            changer.Initialize();

            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            _renderTimes = new List<TimeSpan>();
        }

        private void Update()
        {
            if (drawBvh)
            {
                //print(new StringBuilder().AppendJoin(",", _indices).ToString());
                //print(new StringBuilder().AppendJoin(",\n", _boundingBoxArray).ToString());
                switch (bvhStatus)
                {
                    case BVHStatus.Disabled:
                    case BVHStatus.AllInOne:
                        _bvh?.DrawArray(Color.green, Color.blue, Color.red);
                        break;
                    case BVHStatus.Improved:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            switch (bvhStatus)
            {
                case BVHStatus.Disabled:
                    Shader.DisableKeyword("USE_IMPROVED_BVH");
                    Shader.DisableKeyword("USE_AI1_BVH_COLLISION_CALCULATION");
                    break;
                case BVHStatus.AllInOne:
                    Shader.DisableKeyword("USE_IMPROVED_BVH");
                    Shader.EnableKeyword("USE_AI1_BVH_COLLISION_CALCULATION");
                    break;
                case BVHStatus.Improved:
                    Shader.DisableKeyword("USE_AI1_BVH_COLLISION_CALCULATION");
                    Shader.EnableKeyword("USE_IMPROVED_BVH");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (saveThisFrame)
            {
                StartCoroutine(SaveThisFrame());
                return;
            }

            if (restartRender)
            {
                restartRender = false;
                stopRender = false;
                _startNewRender = true;
                totalAmountOfRaysPerPixel = 0;
                _stopwatch.Restart();
                return;
            }

            if (totalAmountOfRaysPerPixel < raysPerPixelPerImage
                || !saveImageSequence
                || !EditorApplication.isPlaying) return;

            StartCoroutine(SaveScreenShot());

            if (!changer.IsDone) return;

            if (!stopRender)
            {
                var accumulated = _renderTimes.Aggregate((acc, timeSpan) => acc.Add(timeSpan));
                print($"Total time: {accumulated:g}" +
                      $"\nAverage time per frame: {accumulated.Divide(_renderTimes.Count):g}");
            }

            stopRender = true;
        }

        private void VisualizeBoundingBoxes(int index, int count)
        {
            if (index == -1) return;

            VisualizeBVH.DrawArray(
                _boundingBoxArray.GetRange(index, count).ToArray(),
                Color.green, Color.blue, Color.red);
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (Camera.current.name == "SceneCamera" && !useShaderInSceneView)
            {
                _startNewRender = true;
                Graphics.Blit(src, dest);
                return;
            }

            if (totalAmountOfRaysPerPixel >= raysPerPixelPerImage || stopRender)
            {
                Graphics.Blit(_resultTexture, dest);
                return;
            }

            InitFrame();

            if (_startNewRender)
            {
                Graphics.Blit(null, _resultTexture, _rayTracingMaterial);
                Graphics.Blit(_resultTexture, dest);
                _startNewRender = false;
                totalAmountOfRaysPerPixel = 0;
                frameCount = Application.isPlaying ? 1 : 0;
                return;
            }

            var prevFrameCopy = RenderTexture.GetTemporary(src.width, src.height, 0, ShaderHelper.RGBA_SFloat);
            Graphics.Blit(_resultTexture, prevFrameCopy);

            _rayTracingMaterial.SetInteger(Frame, frameCount);

            var currentFrame = RenderTexture.GetTemporary(src.width, src.height, 0, ShaderHelper.RGBA_SFloat);
            Graphics.Blit(null, currentFrame, _rayTracingMaterial);

            totalAmountOfRaysPerPixel += numRaysPerPixel;

            _combiningMaterial.SetInteger(NumRenderedFrames, frameCount);
            _combiningMaterial.SetTexture(MainOldTex, prevFrameCopy);

            Graphics.Blit(currentFrame, _resultTexture, _combiningMaterial);

            Graphics.Blit(_resultTexture, dest);

            RenderTexture.ReleaseTemporary(currentFrame);
            RenderTexture.ReleaseTemporary(prevFrameCopy);

            frameCount += Application.isPlaying ? 1 : 0;
        }

        private void OnDisable()
        {
            ShaderHelper.Release(_sphereBuffer, _triangleBuffer, _meshInfoBuffer, _rectBuffer, _boxSideBuffer,
                _boxInfoBuffer, _fogBoxBuffer, _fogSphereBuffer, _boundingBoxes, _boundingBoxIndices);
            ShaderHelper.Release(_resultTexture);
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            OnEnable();
        }

        #endregion

        private void InitFrame()
        {
            ShaderHelper.InitMaterial(
                BVHStatus.Improved.Equals(bvhStatus) ? improvedRayTracingShader : rayTracingShader,
                ref _rayTracingMaterial);
            ShaderHelper.InitMaterial(combineShader, ref _combiningMaterial);

            ShaderHelper.CreateRenderTexture(ref _resultTexture, Screen.width, Screen.height, FilterMode.Bilinear,
                ShaderHelper.RGBA_SFloat, "Result");

            UpdateCameraParams(Camera.current);

            _baseObjects ??= new List<BaseObject>();
            _baseObjects.Clear();

            CreateSpheres();
            CreateFogSpheres();
            CreateRects();
            CreateBoxes();
            CreateFogBoxes();
            CreateMeshes();
            CreateBVH();
            SetShaderVariables();
        }

        private void UpdateCameraParams(Camera cam)
        {
            var planeHeight = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * 0.5f * (Mathf.PI / 180f)) * 2;
            var planeWidth = planeHeight * cam.aspect;

            _rayTracingMaterial.SetVector(ViewParams, new Vector4(planeWidth, planeHeight, cam.nearClipPlane, 0));
            _rayTracingMaterial.SetMatrix(CamLocalToWorldMatrix, cam.transform.localToWorldMatrix);
        }

        private void CreateSpheres()
        {
            _sphereObjects = FindObjectsOfType<SphereObject>();

            var spheres = new Sphere[_sphereObjects.Length];

            for (var i = 0; i < _sphereObjects.Length; i++)
            {
                var sphereObject = _sphereObjects[i];
                sphereObject.Index(i);
                spheres[i] = sphereObject.GetSphere();
            }

            ShaderHelper.CreateStructuredBuffer(ref _sphereBuffer, spheres);
            _rayTracingMaterial.SetBuffer(Spheres, _sphereBuffer);
            _rayTracingMaterial.SetInteger(NumSpheres, _sphereObjects.Length);

            AddToBaseObjects(_sphereObjects);
        }

        private void CreateFogSpheres()
        {
            _fogSphereObjects = FindObjectsOfType<FogSphereObject>();

            var fogSpheres = new FogSphere[_fogSphereObjects.Length];

            for (var i = 0; i < _fogSphereObjects.Length; i++)
            {
                var fogSphereObject = _fogSphereObjects[i];
                fogSphereObject.Index(i);
                fogSpheres[i] = fogSphereObject.GetFogSphere();
            }

            ShaderHelper.CreateStructuredBuffer(ref _fogSphereBuffer, fogSpheres);
            _rayTracingMaterial.SetBuffer(FogSpheres, _fogSphereBuffer);
            _rayTracingMaterial.SetInteger(NumFogSpheres, _fogSphereObjects.Length);

            AddToBaseObjects(_fogSphereObjects);
        }

        private void CreateRects()
        {
            _rectObjects = FindObjectsOfType<RectObject>();

            var rects = new Rect[_rectObjects.Length];

            for (var i = 0; i < _rectObjects.Length; i++)
            {
                var rectObject = _rectObjects[i];
                rectObject.Index(i);
                rects[i] = rectObject.GetRect();
            }

            ShaderHelper.CreateStructuredBuffer(ref _rectBuffer, rects);
            _rayTracingMaterial.SetBuffer(Rects, _rectBuffer);
            _rayTracingMaterial.SetInteger(NumRects, _rectObjects.Length);

            AddToBaseObjects(_rectObjects);
        }

        private void CreateBoxes()
        {
            _boxObjects = FindObjectsOfType<BoxObject>();

            var boxObjectsLength = _boxObjects.Length;

            _boxInfos ??= new List<BoxInfo>(boxObjectsLength);
            _sides ??= new List<BoxSide>(boxObjectsLength * 6);

            _boxInfos.Clear();
            _sides.Clear();

            for (var i = 0; i < boxObjectsLength; i++)
            {
                var boxObject = _boxObjects[i];
                boxObject.Index(i);

                var info = boxObject.GetBoxInfo();
                info.firstSideIndex = _sides.Count;
                _boxInfos.Add(info);

                var tempSides = boxObject.GetSides();

                _sides.AddRange(tempSides);
            }

            ShaderHelper.CreateStructuredBuffer(ref _boxInfoBuffer, _boxInfos);
            _rayTracingMaterial.SetBuffer(BoxInfos, _boxInfoBuffer);
            _rayTracingMaterial.SetInteger(NumBoxInfos, boxObjectsLength);

            ShaderHelper.CreateStructuredBuffer(ref _boxSideBuffer, _sides);
            _rayTracingMaterial.SetBuffer(BoxSides, _boxSideBuffer);
            _rayTracingMaterial.SetInteger(NumBoxSides, _sides.Count);

            AddToBaseObjects(_boxObjects);
        }

        private void CreateFogBoxes()
        {
            _fogBoxObjects = FindObjectsOfType<FogBoxObject>();

            var fogBoxes = new FogBox[_fogBoxObjects.Length];

            for (var i = 0; i < _fogBoxObjects.Length; i++)
            {
                var fogBoxObject = _fogBoxObjects[i];
                fogBoxObject.Index(i);
                fogBoxes[i] = fogBoxObject.GetFogBox();
            }

            ShaderHelper.CreateStructuredBuffer(ref _fogBoxBuffer, fogBoxes);
            _rayTracingMaterial.SetBuffer(FogBoxes, _fogBoxBuffer);
            _rayTracingMaterial.SetInteger(NumFogBoxes, _fogBoxObjects.Length);

            AddToBaseObjects(_fogBoxObjects);
        }

        private void CreateMeshes()
        {
            _meshObjects = FindObjectsOfType<MeshObject>();

            _allTriangles ??= new List<Triangle>();
            _allMeshInfo ??= new List<MeshInfo>();

            _allTriangles.Clear();
            _allMeshInfo.Clear();

            for (var i = 0; i < _meshObjects.Length; i++)
            {
                var t = _meshObjects[i];
                t.Index(i);

                var (meshInfo, triangles, _) = t.GetInfoAndList();
                meshInfo.firstTriangleIndex = _allTriangles.Count;
                _allTriangles.AddRange(triangles);
                _allMeshInfo.Add(meshInfo);
            }

            ShaderHelper.CreateStructuredBuffer(ref _triangleBuffer, _allTriangles);
            ShaderHelper.CreateStructuredBuffer(ref _meshInfoBuffer, _allMeshInfo);

            _rayTracingMaterial.SetBuffer(Triangles, _triangleBuffer);
            _rayTracingMaterial.SetBuffer(AllMeshInfo, _meshInfoBuffer);
            _rayTracingMaterial.SetInteger(NumMeshes, _allMeshInfo.Count);

            AddToBaseObjects(_meshObjects);
        }

        private void AddToBaseObjects(IEnumerable<BaseObject> baseObjects)
        {
            _baseObjects.AddRange(baseObjects);
        }

        // ReSharper disable once InconsistentNaming
        private void CreateBVH()
        {
            if (!createBvh) return;

            createBvh = false;

            if (_bvh == null) return;

            switch
                (bvhStatus)
            {
                case BVHStatus.Disabled:
                    break;
                case BVHStatus.AllInOne:
                    CreateAllInOneBVH();
                    break;
                case BVHStatus.Improved:
                    CreateImprovedBVH();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        private void CreateAllInOneBVH()
        {
            _bvh.CreateBVH(_baseObjects);
            _boundingBoxArray = _bvh.Boxes.ToList();

            ShaderHelper.CreateStructuredBuffer(ref _boundingBoxes, _boundingBoxArray);
            _rayTracingMaterial.SetBuffer(BoundingBoxes, _boundingBoxes);
            _rayTracingMaterial.SetInteger(NumBoundingBoxes, _boundingBoxArray.Count);
        }

        private void CreateImprovedBVH()
        {
            _meshObjects = FindObjectsOfType<MeshObject>();

            var bvhs = new ImprovedBVH[_meshObjects.Length];

            if (_improvedBvh == null || Application.isEditor)
            {
                for (var index = 0; index < _meshObjects.Length; index++)
                {
                    var meshObject = _meshObjects[index];
                    var (meshInfo, triangles, t) = meshObject.GetInfoAndList();
                    meshInfo.firstTriangleIndex = _allTriangles.Count;

                    _improvedBvh = new ImprovedBVH(triangles.ToArray(), meshInfo.material);

                    _improvedBvh.Build();

                    _improvedBvh.SetTransform(t);

                    bvhs[index] = _improvedBvh;
                }
            }

            var tlas = new TLAS(bvhs);

            tlas.Build();

            if (_improvedBvh == null) return;
            
            VisualizeBVH.DrawArray(_improvedBvh.bvhNodes, Color.green, Color.red, Color.blue);

            ShaderHelper.CreateStructuredBuffer(ref _bvhBuffer, _improvedBvh.bvhNodes);
            ShaderHelper.CreateStructuredBuffer(ref _tlasBuffer, tlas._tlasNodes);
            ShaderHelper.CreateStructuredBuffer(ref _triangleBuffer, _improvedBvh.triangles);
            ShaderHelper.CreateStructuredBuffer(ref _triangleIndexBuffer, _improvedBvh._triIndex);

            _rayTracingMaterial.SetBuffer(BvhNodes, _bvhBuffer);
            _rayTracingMaterial.SetBuffer(TlasNodes, _tlasBuffer);
            _rayTracingMaterial.SetBuffer(Triangles, _triangleBuffer);
            _rayTracingMaterial.SetBuffer(TriangleIndices, _triangleIndexBuffer);
        }

        private void SetShaderVariables()
        {
            SetSkyParams();

            _rayTracingMaterial.SetFloat(DivergeStrength, divergeStrength);
            _rayTracingMaterial.SetInteger(NumRaysPerPixel, numRaysPerPixel);
            _rayTracingMaterial.SetInteger(MaxBounceCount, maxBounceCount);
        }

        private void SetSkyParams()
        {
            _rayTracingMaterial.SetInteger(EnvironmentEnabled, environmentEnabled ? 1 : 0);
            _rayTracingMaterial.SetVector(SkyColorHorizon, skyColorHorizon);
            _rayTracingMaterial.SetVector(SkyColorZenith, skyColorZenith);
            _rayTracingMaterial.SetFloat(SunFocus, sunFocus);
            _rayTracingMaterial.SetFloat(SunIntensity, sunIntensity);
            _rayTracingMaterial.SetVector(GroundColor, groundColor);
        }

        private IEnumerator SaveScreenShot()
        {
            yield return new WaitForEndOfFrame();

            var path = $"images/{folderName}/";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

/* ffmpeg -v warning -i "input.mp4" -vf "fps=36,scale=1080:-1:flags=lanczos,palettegen" -y "tmp/palette.png"
 * ffmpeg -v warning -i "input.mp4" -i "tmp/palette.png" -lavfi "fps=36,scale=1080:-1:flags=lanczos [x]; [x][1:v] paletteuse" -y "out.gif"
 */
            path += changer.FileName();
            changer.Increment();
            amountOfPictures = changer.NumberOfImages;

            _startNewRender = true;
            totalAmountOfRaysPerPixel = 0;

            var time = _stopwatch.Elapsed;
            _stopwatch.Restart();
            _renderTimes.Add(time);

            if (File.Exists(path))
            {
                print("File already exists");
                yield break;
            }

            ScreenCapture.CaptureScreenshot(path);

            //print($"Saved to {path}\nAnd took {time:g}");
        }

        private IEnumerator SaveThisFrame()
        {
            yield return new WaitForEndOfFrame();

            var path = $"images/{folderName}/";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            saveThisFrame = false;
            var current = Camera.current;
            path +=
                $"/Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}" +
                $"_{current.pixelWidth}x{current.pixelHeight}" +
                $"_{totalAmountOfRaysPerPixel}rays" +
                $"_{maxBounceCount}bounces.png";

            if (File.Exists(path))
            {
                print("File already exists");
                yield break;
            }

            ScreenCapture.CaptureScreenshot(path);

            print($"Saved to {path}\n");
        }
    }
}