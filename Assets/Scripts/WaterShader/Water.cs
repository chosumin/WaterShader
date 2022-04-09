using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace WaterShader
{
    [ExecuteAlways]
    public class Water : MonoBehaviour
    {
        private static readonly int FoamMap = Shader.PropertyToID("_FoamMap");
        private static readonly int SurfaceMap = Shader.PropertyToID("_SurfaceMap");
        private static readonly int WaveHeight = Shader.PropertyToID("_WaveHeight");
        private static readonly int MaxWaveHeight = Shader.PropertyToID("_MaxWaveHeight");
        private static readonly int MaxDepth = Shader.PropertyToID("_MaxDepth");
        private static readonly int WaveCount = Shader.PropertyToID("_WaveCount");
        private static readonly int WaveData = Shader.PropertyToID("waveData");
        
        [SerializeField] private WaterResources _resources;
        [SerializeField] private WaterSurfaceData _surfaceData;

        private PlanarReflection _planarReflection;
        private Transform _cachedTransform;
        private Wave[] _waves;
        private float _maxWaveHeight;
        private float _waveHeight;
        
        private void Awake()
        {
            _cachedTransform = transform;
        }

        private void OnEnable()
        {
            Init();
            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
        }

        private void Init()
        {
            SetWaves();
            //todo : GenerateColorRamp();

            //todo : Depth Map
            // if(bakedDepthTex)
            //     Shader.SetGlobalTexture(WaterDepthMap, bakedDepthTex);

            if (!gameObject.TryGetComponent(out _planarReflection))
                _planarReflection = gameObject.AddComponent<PlanarReflection>();
            
            //todo : CaptureDepthMap();
        }

        private void SetWaves()
        {
            SetUpWaves();
            
            Shader.SetGlobalTexture(FoamMap, _resources.DefaultFoamMap);
            Shader.SetGlobalTexture(SurfaceMap, _resources.DefaultSurfaceMap);
            
            _maxWaveHeight = 0f;
            foreach (var w in _waves)
            {
                _maxWaveHeight += w.Amplitude;
            }
            _maxWaveHeight /= _waves.Length;

            _waveHeight = transform.position.y;
            
            Shader.SetGlobalFloat(WaveHeight, _waveHeight);
            Shader.SetGlobalFloat(MaxWaveHeight, _maxWaveHeight);
            Shader.SetGlobalFloat(MaxDepth, _surfaceData.WaterMaxVisibility);

            Shader.EnableKeyword("_REFLECTION_PLANARREFLECTION");
            
            Shader.SetGlobalInt(WaveCount, _waves.Length);
            Shader.SetGlobalVectorArray(WaveData, GetWaveData());
        }
        
        private Vector4[] GetWaveData()
        {
            var waveData = new Vector4[20];
            for (var i = 0; i < _waves.Length; i++)
            {
                waveData[i] = new Vector4(_waves[i].Amplitude, _waves[i].Direction,
                    _waves[i].WaveLength, _waves[i].OnmiDir);

                waveData[i + 10] = new Vector4(_waves[i].Origin.x, _waves[i].Origin.y, 0, 0);
            }

            return waveData;
        }
        
        private void SetUpWaves()
        {
            if(_surfaceData.CustomWaves == false)
            {
                //create basic waves based off basic wave settings
                var backupSeed = Random.state;
                Random.InitState(_surfaceData.RandomSeed);
                var basicWaves = _surfaceData.BasicWaveSettings;
                var a = basicWaves.Amplitude;
                var d = basicWaves.Direction;
                var l = basicWaves.WaveLength;
                var numWave = basicWaves.NumWaves;
                _waves = new Wave[numWave];

                var r = 1f / numWave;

                for (var i = 0; i < numWave; i++)
                {
                    var p = Mathf.Lerp(0.5f, 1.5f, i * r);
                    var amp = a * p * Random.Range(0.8f, 1.2f);
                    var dir = d + Random.Range(-90f, 90f);
                    var len = l * p * Random.Range(0.6f, 1.4f);
                    _waves[i] = new Wave(amp, dir, len, Vector2.zero, false);
                    Random.InitState(_surfaceData.RandomSeed + i + 1);
                }
                Random.state = backupSeed;
            }
            else
            {
                _waves = _surfaceData.Waves.ToArray();
            }
        }
        
        private void BeginCameraRendering(ScriptableRenderContext src, Camera renderingCamera)
        {
            const float quantizeValue = 6.25f;
            const float forwards = 10f;
            const float yOffset = -0.25f;
            
            var newPosition = renderingCamera.transform.TransformPoint(Vector3.forward * forwards);
            newPosition.y = yOffset;
            newPosition.x = quantizeValue * (int) (newPosition.x / quantizeValue);
            newPosition.z = quantizeValue * (int) (newPosition.z / quantizeValue);

            var matrix = Matrix4x4.TRS(
                newPosition + _cachedTransform.position, 
                quaternion.identity, 
                _cachedTransform.localScale);
            
            foreach (var mesh in _resources.DefaultWaterMeshes)
            {
                Graphics.DrawMesh(mesh,
                    matrix,
                    _resources.DefaultSeaMaterial,
                    gameObject.layer,
                    renderingCamera,
                    0, null,
                    ShadowCastingMode.Off,
                    true,
                    null,
                    LightProbeUsage.Off,
                    null);
            }
        }
    }
}
