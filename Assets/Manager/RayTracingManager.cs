using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Attributes;
using Objects;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static Structs;

namespace Manager
{
    [ExecuteAlways, ImageEffectAllowedInSceneView]
    public class RayTracingManager : MonoBehaviour
    {
        private const float Deg2Rad = Mathf.PI / 180f;

        private static readonly int Frame = Shader.PropertyToID("Frame");
        private static readonly int Rects = Shader.PropertyToID("Rects");
        private static readonly int MainTex = Shader.PropertyToID("MainTex");
        private static readonly int Spheres = Shader.PropertyToID("Spheres");
        private static readonly int NumRects = Shader.PropertyToID("NumRects");
        private static readonly int SunFocus = Shader.PropertyToID("SunFocus");
        private static readonly int NumMeshes = Shader.PropertyToID("NumMeshes");
        private static readonly int Triangles = Shader.PropertyToID("Triangles");
        private static readonly int MainOldTex = Shader.PropertyToID("MainOldTex");
        private static readonly int NumSpheres = Shader.PropertyToID("NumSpheres");
        private static readonly int ViewParams = Shader.PropertyToID("ViewParams");
        private static readonly int AllMeshInfo = Shader.PropertyToID("AllMeshInfo");
        private static readonly int GroundColor = Shader.PropertyToID("GroundColor");
        //private static readonly int SunIntensity = Shader.PropertyToID("SunIntensity");

        private static readonly int DivergeStrength = Shader.PropertyToID("DivergeStrength");
        private static readonly int MaxBounceCount = Shader.PropertyToID("MaxBounceCount");
        private static readonly int SkyColorZenith = Shader.PropertyToID("SkyColorZenith");
        private static readonly int NumRaysPerPixel = Shader.PropertyToID("NumRaysPerPixel");
        private static readonly int SkyColorHorizon = Shader.PropertyToID("SkyColorHorizon");
        private static readonly int NumRenderedFrames = Shader.PropertyToID("NumRenderedFrames");
        //private static readonly int SunLightDirection = Shader.PropertyToID("SunLightDirection");
        private static readonly int EnvironmentEnabled = Shader.PropertyToID("EnvironmentEnabled");
        private static readonly int CamLocalToWorldMatrix = Shader.PropertyToID("CamLocalToWorldMatrix");


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

        [Space, Header("Setup Settings"),SerializeField]
        private bool useShaderInSceneView;

        //[SerializeField] private Light sun;
        [SerializeField] private Material rayTracingMaterial;
        [SerializeField] private Material combiningMaterial;
        [SerializeField] private Changer changer;

        [SerializeField] private bool environmentEnabled;
        [SerializeField] private Color skyColorHorizon;
        [SerializeField] private Color skyColorZenith;
        [SerializeField] private Color groundColor;
        //[SerializeField,Range(0, 10)] private float sunFocus = 1;

        [Space,SerializeField] private List<MeshObject> meshes;
        [SerializeField] private List<SphereObject> spheres;
        [SerializeField] private List<RectObject> rects;

        private GraphicsBuffer _spheres;
        private GraphicsBuffer _triangles;
        private GraphicsBuffer _meshInfos;
        private GraphicsBuffer _rects;

        private RenderTexture _oldRT;
        private RenderTexture _newRT;
        private bool _wasLastFrameRayTraced;
        private bool _startNewRender;

        private int _oldFrameCount;


        private Stopwatch _stopwatch;

        private void OnEnable()
        {
            changer.Initialize();
            totalAmountOfRaysPerPixel = 0;
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            UpdateParams();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            OnEnable();
        }

        private void UpdateParams()
        {
            rayTracingMaterial.SetFloat(DivergeStrength, divergeStrength);
            rayTracingMaterial.SetInteger(NumRaysPerPixel, numRaysPerPixel);
            rayTracingMaterial.SetInteger(MaxBounceCount, maxBounceCount);

            UpdateSunParams();

            if (spheres.Count > 0)
            {
                _spheres?.Dispose();
                _spheres = new GraphicsBuffer(GraphicsBuffer.Target.Structured, spheres.Count,
                    Marshal.SizeOf(typeof(Sphere)));

                var s = new Sphere[spheres.Count];

                for (var i = 0; i < spheres.Count; i++)
                {
                    s[i] = spheres[i].Sphere;
                }

                _spheres.SetData(s);
                rayTracingMaterial.SetBuffer(Spheres, _spheres);
                rayTracingMaterial.SetInteger(NumSpheres, spheres.Count);
            }

            if (rects.Count > 0)
            {
                _rects?.Dispose();
                _rects = new GraphicsBuffer(GraphicsBuffer.Target.Structured, rects.Count,
                    Marshal.SizeOf(typeof(Structs.Rect)));

                var r = new Structs.Rect[rects.Count];

                for (var i = 0; i < rects.Count; i++)
                {
                    r[i] = rects[i].GetRect();
                }

                _rects.SetData(r);
                rayTracingMaterial.SetBuffer(Rects, _rects);
                rayTracingMaterial.SetInteger(NumRects, rects.Count);
            }

            if (meshes.Count <= 0) return;

            // Is this a messy way to do this? yes. Does it work? yes
            var list = meshes.Select(m => m.GetInfoAndList());

            var valueTuples = list as (MeshInfo, List<Triangle>)[] ?? list.ToArray();

            var triangles = valueTuples
                .Select(l => l.Item2)
                .SelectMany(x => x)
                .ToArray();

            _triangles?.Dispose();
            _triangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, triangles.Length,
                Marshal.SizeOf(typeof(Triangle)));

            _triangles.SetData(triangles);
            rayTracingMaterial.SetBuffer(Triangles, _triangles);

            var meshInfos = valueTuples
                .Select(l => l.Item1)
                .Select(l => l)
                .ToArray();

            var infos = new MeshInfo[meshInfos.Length];
            
            for (int i = 0, verts = 0; i < meshInfos.Length; i++)
            {
                var mi = meshInfos[i];

                mi.firstTriangleIndex = verts;
                verts += mi.numTriangles;

                infos[i] = mi;
            }

            _meshInfos?.Dispose();
            _meshInfos = new GraphicsBuffer(GraphicsBuffer.Target.Structured, infos.Length,
                Marshal.SizeOf(typeof(MeshInfo)));

            _meshInfos.SetData(infos);
            rayTracingMaterial.SetBuffer(AllMeshInfo, _meshInfos);

            rayTracingMaterial.SetInteger(NumMeshes, infos.Length);
        }

        private void Update()
        {
            if (saveThisFrame)
            {
                StartCoroutine(SaveScreenShot());
                return;
            }

            if (restartRender)
            {
                restartRender = false;
                _wasLastFrameRayTraced = false;
                stopRender = false;
                totalAmountOfRaysPerPixel = 0;
                _oldFrameCount = frameCount;
                UpdateParams();
                return;
            }

            if (totalAmountOfRaysPerPixel < raysPerPixelPerImage
                || !saveImageSequence
                || !EditorApplication.isPlaying) return;

            StartCoroutine(SaveScreenShot());
            _wasLastFrameRayTraced = false;
            totalAmountOfRaysPerPixel = 0;
            _oldFrameCount = frameCount;

            if (changer.IsDone) stopRender = true;

            amountOfPictures++;
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (Camera.current.name == "SceneCamera" && !useShaderInSceneView)
            {
                _wasLastFrameRayTraced = false;
                Graphics.Blit(src, dest);
                return;
            }

            frameCount = Time.frameCount;

            if (totalAmountOfRaysPerPixel >= raysPerPixelPerImage | stopRender)
            {
                Graphics.Blit(_oldRT, dest);
                return;
            }

            rayTracingMaterial.SetInteger(Frame, frameCount);

            totalAmountOfRaysPerPixel += numRaysPerPixel;
            UpdateCameraParams(Camera.current);

            _oldRT ??= new RenderTexture(src.descriptor);
            _oldRT.Create();
            _newRT ??= new RenderTexture(src.descriptor);
            _newRT.Create();

            if (!_wasLastFrameRayTraced || Camera.current.name == "SceneCamera")
            {
                Graphics.Blit(null, _oldRT, rayTracingMaterial);
                Graphics.Blit(_oldRT, dest);
                _wasLastFrameRayTraced = true;
                return;
            }

            Graphics.Blit(null, _newRT, rayTracingMaterial);

            combiningMaterial.SetTexture(MainOldTex, _oldRT);
            combiningMaterial.SetTexture(MainTex, _newRT);
            combiningMaterial.SetInteger(NumRenderedFrames, frameCount - _oldFrameCount);

            Graphics.Blit(null, _oldRT, combiningMaterial);
            Graphics.Blit(_oldRT, dest);
        }

        private void UpdateCameraParams(Camera cam)
        {
            var planeHeight = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * 0.5f * Deg2Rad) * 2;
            var planeWidth = planeHeight * cam.aspect;

            rayTracingMaterial.SetVector(ViewParams, new Vector4(planeWidth, planeHeight, cam.nearClipPlane, 0));
            rayTracingMaterial.SetMatrix(CamLocalToWorldMatrix, cam.transform.localToWorldMatrix);
        }

        private void UpdateSunParams()
        {
            rayTracingMaterial.SetInteger(EnvironmentEnabled, environmentEnabled ? 1 : 0);
            rayTracingMaterial.SetVector(SkyColorHorizon, skyColorHorizon);
            rayTracingMaterial.SetVector(SkyColorZenith, skyColorZenith);
            //rayTracingMaterial.SetVector(SunLightDirection, sun.transform.rotation.eulerAngles.normalized);
            //rayTracingMaterial.SetFloat(SunFocus, sunFocus);
            //rayTracingMaterial.SetFloat(SunIntensity, sun.intensity);
            rayTracingMaterial.SetVector(GroundColor, groundColor);
        }

        private IEnumerator SaveScreenShot()
        {
            yield return new WaitForEndOfFrame();

            var path = $"images/{folderName}";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

/* ffmpeg -v warning -i "input.mp4" -vf "fps=36,scale=1080:-1:flags=lanczos,palettegen" -y "tmp/palette.png"
 * ffmpeg -v warning -i "input.mp4" -i "tmp/palette.png" -lavfi "fps=36,scale=1080:-1:flags=lanczos [x]; [x][1:v] paletteuse" -y "out.gif"
 */
            if (!saveThisFrame)
            {
                path += changer.FileName();
                changer.Increment();

                UpdateParams();
            }
            else
            {
                saveThisFrame = false;
                var current = Camera.current;
                path +=
                    $"/Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{current.pixelWidth}x{current.pixelHeight}_{totalAmountOfRaysPerPixel}rays_{maxBounceCount}bounces.png";
            }

            if (File.Exists(path))
            {
                print("File already exists");
                yield break;
            }

            ScreenCapture.CaptureScreenshot(path);
            var time = _stopwatch.Elapsed;
            _stopwatch.Restart();

            print($"Saved to {path}\nAnd took {time:g}");
        }

        private void OnDisable()
        {
            _spheres?.Dispose();
            _spheres = null;
            _triangles?.Dispose();
            _triangles = null;
            _meshInfos?.Dispose();
            _meshInfos = null;
            _rects?.Dispose();
            _rects = null;
        }
    }
}