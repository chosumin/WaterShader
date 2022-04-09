using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WaterShader
{
    [ExecuteAlways]
    public class PlanarReflection : MonoBehaviour
    {
        private readonly int PlanarReflectionTextureId = Shader.PropertyToID("_PlanarReflectionTexture");
        
        [SerializeField] private GameObject _target;
        [SerializeField] private float _planeOffset;
        [SerializeField] private float _clipPlaneOffset = 0.07f;
        [SerializeField] private LayerMask _reflectLayers = -1;

        private Camera _reflectionCamera;
        private RenderTexture _reflectionRenderTexture;
        private int2 _oldReflectionTextureSize;

        public static event Action<ScriptableRenderContext, Camera> BeginPlanarReflections;

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += ExecutePlanarReflections;
        }
        
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= ExecutePlanarReflections;
        }

        private void ExecutePlanarReflections(ScriptableRenderContext context, Camera mainCamera)
        {
            if (mainCamera.cameraType == CameraType.Reflection ||
                mainCamera.cameraType == CameraType.Preview)
                return;

            UpdateReflectionCamera(mainCamera);
            PlanarReflectionTexture(mainCamera);
 
            BeginPlanarReflections?.Invoke(context, _reflectionCamera);
            UniversalRenderPipeline.RenderSingleCamera(context, _reflectionCamera);
            
            Shader.SetGlobalTexture(PlanarReflectionTextureId, _reflectionRenderTexture);
        }

        private void UpdateReflectionCamera(Camera mainCamera)
        {
            //hack : it would be better to create it by Editor rather than the code.
            if (_reflectionCamera == null)
                _reflectionCamera = CreateMirrorObjects();
            
            Vector3 position = Vector3.zero;
            Vector3 normal = Vector3.up;

            if (_target != null)
            {
                position = _target.transform.position + Vector3.up * _planeOffset;
                normal = _target.transform.up;
            }

            UpdateCamera(mainCamera, _reflectionCamera);

            var mainCameraTransform = mainCamera.transform;
            var oldPosition = mainCameraTransform.position - new Vector3(0, position.y * 2, 0);
            var newPosition = new Vector3(oldPosition.x, -oldPosition.y, oldPosition.z);
            
            float d = -Vector3.Dot(normal, position) - _clipPlaneOffset;
            Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

            Matrix4x4 reflection = Matrix4x4.identity * Matrix4x4.Scale(new Vector3(1, -1, 1));

            CalculateReflectionMatrix(ref reflection, reflectionPlane);
            _reflectionCamera.transform.forward = Vector3.Scale(mainCameraTransform.forward, new Vector3(1, -1, 1));
            _reflectionCamera.worldToCameraMatrix = mainCamera.worldToCameraMatrix * reflection;
            
            var clipPlane = CameraSpacePlane(_reflectionCamera, position - Vector3.up * 0.1f, normal, 1.0f);
            var projection = mainCamera.CalculateObliqueMatrix(clipPlane);
            _reflectionCamera.projectionMatrix = projection;
            _reflectionCamera.cullingMask = _reflectLayers; // never render water layer
            _reflectionCamera.transform.position = newPosition;
        }

        private void PlanarReflectionTexture(Camera cam)
        {
            if (_reflectionRenderTexture == null)
            {
                var res = ReflectionResolution(cam, UniversalRenderPipeline.asset.renderScale);
                bool useHdr10 = RenderingUtils.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float);
                RenderTextureFormat hdrFormat = useHdr10 ? RenderTextureFormat.RGB111110Float : RenderTextureFormat.DefaultHDR;
                _reflectionRenderTexture = RenderTexture.GetTemporary(res.x, res.y, 16,
                    GraphicsFormatUtility.GetGraphicsFormat(hdrFormat, true));
            }
            _reflectionCamera.targetTexture =  _reflectionRenderTexture;
        }
        
        private int2 ReflectionResolution(Camera cam, float scale)
        {
            var x = (int)(cam.pixelWidth * scale * 0.33f);
            var y = (int)(cam.pixelHeight * scale * 0.33f);
            return new int2(x, y);
        }

        private Camera CreateMirrorObjects()
        {
            var go = new GameObject("PlanarReflections", typeof(Camera));
            var cameraData = go.AddComponent(
                    typeof(UniversalAdditionalCameraData)) as UniversalAdditionalCameraData;

            if (cameraData != null)
            {
                cameraData.requiresColorOption = CameraOverrideOption.Off;
                cameraData.requiresDepthOption = CameraOverrideOption.Off;
                cameraData.SetRenderer(1);
            }

            var t = transform;
            var reflectionCamera = go.GetComponent<Camera>();
            reflectionCamera.transform.SetPositionAndRotation(t.position, t.rotation);
            reflectionCamera.depth = -10;
            reflectionCamera.enabled = false;
            go.hideFlags = HideFlags.HideAndDontSave;

            return reflectionCamera;
        }

        private void UpdateCamera(Camera source, Camera destination)
        {
            if (destination == null)
                return;

            destination.CopyFrom(source);
            destination.useOcclusionCulling = false;

            if (destination.gameObject.TryGetComponent(out UniversalAdditionalCameraData data))
            {
                data.renderShadows = false;
            }
        }

        private void CalculateReflectionMatrix(ref Matrix4x4 matrix, Vector4 plane)
        {
            matrix.m00 = (1f - 2f * plane[0] * plane[0]);
            matrix.m01 = (-2f * plane[0] * plane[1]);
            matrix.m02 = (-2f * plane[0] * plane[2]);
            matrix.m03 = (-2f * plane[3] * plane[0]);

            matrix.m10 = (-2f * plane[1] * plane[0]);
            matrix.m11 = (1f - 2f * plane[1] * plane[1]);
            matrix.m12 = (-2f * plane[1] * plane[2]);
            matrix.m13 = (-2f * plane[3] * plane[1]);

            matrix.m20 = (-2f * plane[2] * plane[0]);
            matrix.m21 = (-2f * plane[2] * plane[1]);
            matrix.m22 = (1f - 2f * plane[2] * plane[2]);
            matrix.m23 = (-2f * plane[3] * plane[2]);

            matrix.m30 = 0f;
            matrix.m31 = 0f;
            matrix.m32 = 0f;
            matrix.m33 = 1f;
        }
        
        private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
        {
            var offsetPos = pos + normal * _clipPlaneOffset;
            var m = cam.worldToCameraMatrix;
            var cameraPosition = m.MultiplyPoint(offsetPos);
            var cameraNormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z, -Vector3.Dot(cameraPosition, cameraNormal));
        }
    }
}