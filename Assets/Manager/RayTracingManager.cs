using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Attributes;
using DataTypes;
using Helpers;
using Objects;
using UnityEditor;
using UnityEngine;
using Rect = DataTypes.Rect;

namespace Manager
{
    [ExecuteAlways, ImageEffectAllowedInSceneView]
    public class RayTracingManager : MonoBehaviour
    {
        private const float Deg2Rad = Mathf.PI / 180f;

        // @formatter:off
        private static readonly int Frame = Shader.PropertyToID("Frame");
        private static readonly int Rects = Shader.PropertyToID("Rects");
        private static readonly int Spheres = Shader.PropertyToID("Spheres");
        private static readonly int BoxInfos = Shader.PropertyToID("BoxInfos");
        private static readonly int BoxSides = Shader.PropertyToID("BoxSides");
        private static readonly int NumRects = Shader.PropertyToID("NumRects");
        //private static readonly int SunFocus = Shader.PropertyToID("SunFocus");
        private static readonly int NumMeshes = Shader.PropertyToID("NumMeshes");
        private static readonly int Triangles = Shader.PropertyToID("Triangles");
        private static readonly int MainOldTex = Shader.PropertyToID("MainOldTex");
        private static readonly int NumSpheres = Shader.PropertyToID("NumSpheres");
        private static readonly int ViewParams = Shader.PropertyToID("ViewParams");
        //private static readonly int SunIntensity = Shader.PropertyToID("SunIntensity");
        private static readonly int AllMeshInfo = Shader.PropertyToID("AllMeshInfo");
        private static readonly int GroundColor = Shader.PropertyToID("GroundColor");
        private static readonly int NumBoxInfos = Shader.PropertyToID("NumBoxInfos");
        private static readonly int NumBoxSides = Shader.PropertyToID("NumBoxSides");
        private static readonly int MaxBounceCount = Shader.PropertyToID("MaxBounceCount");
        private static readonly int SkyColorZenith = Shader.PropertyToID("SkyColorZenith");
        private static readonly int DivergeStrength = Shader.PropertyToID("DivergeStrength");
        private static readonly int NumRaysPerPixel = Shader.PropertyToID("NumRaysPerPixel");
        private static readonly int SkyColorHorizon = Shader.PropertyToID("SkyColorHorizon");
        //private static readonly int SunLightDirection = Shader.PropertyToID("SunLightDirection");
        private static readonly int NumRenderedFrames = Shader.PropertyToID("NumRenderedFrames");
        private static readonly int EnvironmentEnabled = Shader.PropertyToID("EnvironmentEnabled");
        private static readonly int CamLocalToWorldMatrix = Shader.PropertyToID("CamLocalToWorldMatrix");
        // @formatter:on

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

        //[SerializeField] private Light sun;
        [SerializeField] private Shader rayTracingShader;
        [SerializeField] private Shader combineShader;
        [SerializeField] private ChangerManager changer;

        [SerializeField] private bool environmentEnabled;
        [SerializeField] private Color skyColorHorizon;
        [SerializeField] private Color skyColorZenith;

        [SerializeField] private Color groundColor;
        //[SerializeField,Range(1, 10)] private float sunFocus = 1;
        //[SerializeField,Range(0, 10)] private float sunIntensity = 1;

        private Material _rayTracingMaterial;
        private Material _combiningMaterial;

        private GraphicsBuffer _sphereBuffer;
        private GraphicsBuffer _rectBuffer;
        private GraphicsBuffer _boxSideBuffer;
        private GraphicsBuffer _boxInfoBuffer;
        private GraphicsBuffer _triangleBuffer;
        private GraphicsBuffer _meshInfoBuffer;

        private RenderTexture _resultTexture;
        private bool _wasLastFrameRayTraced;
        private bool _startNewRender;

        private List<Triangle> _allTriangles;
        private List<MeshInfo> _allMeshInfo;
        private List<BoxInfo> _boxInfos;
        private List<BoxSide> _sides;
        
        private Stopwatch _stopwatch;
        private List<TimeSpan> _renderTimes;

        private void Start()
        {
            frameCount = 0;
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
                var accumulated = _renderTimes.Aggregate(new TimeSpan(), (acc, timeSpan) => acc.Add(timeSpan));
                print($"Total time: {accumulated:g}" +
                      $"\nAverage time per frame: {accumulated.Divide(_renderTimes.Count):g}");
            }

            stopRender = true;
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

        private void InitFrame()
        {
            ShaderHelper.InitMaterial(rayTracingShader, ref _rayTracingMaterial);
            ShaderHelper.InitMaterial(combineShader, ref _combiningMaterial);

            ShaderHelper.CreateRenderTexture(ref _resultTexture, Screen.width, Screen.height, FilterMode.Bilinear,
                ShaderHelper.RGBA_SFloat, "Result");

            UpdateCameraParams(Camera.current);
            CreateSpheres();
            CreateRects();
            CreateBoxes();
            CreateMeshes();
            SetShaderVariables();
        }


        private void UpdateCameraParams(Camera cam)
        {
            var planeHeight = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * 0.5f * Deg2Rad) * 2;
            var planeWidth = planeHeight * cam.aspect;

            _rayTracingMaterial.SetVector(ViewParams, new Vector4(planeWidth, planeHeight, cam.nearClipPlane, 0));
            _rayTracingMaterial.SetMatrix(CamLocalToWorldMatrix, cam.transform.localToWorldMatrix);
        }

        private void CreateSpheres()
        {
            var sphereObjects = FindObjectsOfType<SphereObject>();

            var spheres = new Sphere[sphereObjects.Length];

            for (var i = 0; i < sphereObjects.Length; i++)
            {
                spheres[i] = new Sphere
                {
                    center = sphereObjects[i].transform.position,
                    radius = sphereObjects[i].transform.lossyScale.x * 0.5f,
                    rayTracingMaterial = sphereObjects[i].GetMaterial(),
                };
            }

            ShaderHelper.CreateStructuredBuffer(ref _sphereBuffer, spheres);
            _rayTracingMaterial.SetBuffer(Spheres, _sphereBuffer);
            _rayTracingMaterial.SetInteger(NumSpheres, sphereObjects.Length);
        }

        private void CreateRects()
        {
            var rectObjects = FindObjectsOfType<RectObject>();

            var rects = new Rect[rectObjects.Length];

            for (var i = 0; i < rectObjects.Length; i++)
            {
                rects[i] = rectObjects[i].GetRect();
            }

            ShaderHelper.CreateStructuredBuffer(ref _rectBuffer, rects);
            _rayTracingMaterial.SetBuffer(Rects, _rectBuffer);
            _rayTracingMaterial.SetInteger(NumRects, rectObjects.Length);
        }

        private void CreateBoxes()
        {
            var boxObjects = FindObjectsOfType<BoxObject>();

            var boxObjectsLength = boxObjects.Length;

            _boxInfos ??= new List<BoxInfo>(boxObjectsLength);
            _sides ??= new List<BoxSide>(boxObjectsLength * 6);
            
            _boxInfos.Clear();
            _sides.Clear();
            
            for (var i = 0; i < boxObjectsLength; i++)
            {
                var info = boxObjects[i].GetBoxInfo();
                info.firstSideIndex = _sides.Count;
                _boxInfos.Add(info);
                
                var tempSides = boxObjects[i].GetSides();

                _sides.AddRange(tempSides);
            }
            
            ShaderHelper.CreateStructuredBuffer(ref _boxInfoBuffer, _boxInfos);
            _rayTracingMaterial.SetBuffer(BoxInfos, _boxInfoBuffer);
            _rayTracingMaterial.SetInteger(NumBoxInfos, boxObjectsLength);

            ShaderHelper.CreateStructuredBuffer(ref _boxSideBuffer, _sides);
            _rayTracingMaterial.SetBuffer(BoxSides, _boxSideBuffer);
            _rayTracingMaterial.SetInteger(NumBoxSides, _sides.Count);
        }

        private void CreateMeshes()
        {
            var meshObjects = FindObjectsOfType<MeshObject>();

            _allTriangles ??= new List<Triangle>();
            _allMeshInfo ??= new List<MeshInfo>();

            _allTriangles.Clear();
            _allMeshInfo.Clear();

            foreach (var t in meshObjects)
            {
                var (meshInfo, triangles) = t.GetInfoAndList();

                meshInfo.firstTriangleIndex = _allTriangles.Count;
                _allTriangles.AddRange(triangles);
                _allMeshInfo.Add(meshInfo);
            }

            ShaderHelper.CreateStructuredBuffer(ref _triangleBuffer, _allTriangles);
            ShaderHelper.CreateStructuredBuffer(ref _meshInfoBuffer, _allMeshInfo);

            _rayTracingMaterial.SetBuffer(Triangles, _triangleBuffer);
            _rayTracingMaterial.SetBuffer(AllMeshInfo, _meshInfoBuffer);
            _rayTracingMaterial.SetInteger(NumMeshes, _allMeshInfo.Count);
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
            //rayTracingMaterial.SetVector(SunLightDirection, sun.transform.rotation.eulerAngles.normalized);
            //rayTracingMaterial.SetFloat(SunFocus, sunFocus);
            //rayTracingMaterial.SetFloat(SunIntensity, sun.intensity);
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
            /*
             * TODO Whenever a change has been done by the ChangerManager, it should mark the changed object
             * TODO as needed to be recalculated, otherwise it should fetch cached values
             * Should save processing power, if done correctly
             */
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

        private void OnDisable()
        {
            ShaderHelper.Release(_sphereBuffer, _triangleBuffer, _meshInfoBuffer, _rectBuffer, _boxSideBuffer,
                _boxInfoBuffer);
            ShaderHelper.Release(_resultTexture);
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            OnEnable();
        }
    }
}