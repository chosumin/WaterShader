using UnityEngine;

namespace WaterShader
{
    [CreateAssetMenu(fileName = "WaterResources", menuName = "WaterSystem/Resource", order = 0)]
    public class WaterResources : ScriptableObject
    {
        /// <summary>
        /// Foam Ramp
        /// </summary>
        public Texture2D DefaultFoamRamp;
        
        /// <summary>
        /// Foam Texture Map
        /// </summary>
        public Texture2D DefaultFoamMap;
        
        /// <summary>
        /// Normal / Caustic Map
        /// </summary>
        public Texture2D DefaultSurfaceMap;
        
        public Material DefaultSeaMaterial;
        public Mesh[] DefaultWaterMeshes;
    }
}