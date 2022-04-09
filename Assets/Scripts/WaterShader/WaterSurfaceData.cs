using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace WaterShader
{
    [CreateAssetMenu(fileName = "WaterSurfaceData", menuName = "WaterSystem/Surface Data", order = 0)]
    public class WaterSurfaceData : ScriptableObject
    {
        public float WaterMaxVisibility = 40.0f;
        public Gradient AbsorptionRamp;
        public Gradient ScatterRamp;
        public List<Wave> Waves = new List<Wave>();
        public bool CustomWaves = false;
        public int RandomSeed = 3234;
        public BasicWaves BasicWaveSettings = new BasicWaves(1.5f, 45.0f, 5.0f);
        public FoamSettings FoamSettings = new FoamSettings();
        [SerializeField]
        public bool _init = false;
    }
    
    [System.Serializable]
    public struct Wave
    {
        public float Amplitude; // height of the wave in units(m)
        public float Direction; // direction the wave travels in degrees from Z+
        public float WaveLength; // distance between crest>crest
        public float2 Origin; // Omi directional point of origin
        public float OnmiDir; // Is omni?

        public Wave(float amp, float dir, float length, float2 org, bool omni)
        {
            Amplitude = amp;
            Direction = dir;
            WaveLength = length;
            Origin = org;
            OnmiDir = omni ? 1 : 0;
        }
    }
    
    [System.Serializable]
    public class BasicWaves
    {
        public int NumWaves = 6;
        public float Amplitude;
        public float Direction;
        public float WaveLength;

        public BasicWaves(float amp, float dir, float len)
        {
            NumWaves = 6;
            Amplitude = amp;
            Direction = dir;
            WaveLength = len;
        }
    }

    [System.Serializable]
    public class FoamSettings
    {
        public int FoamType; // 0=default, 1=simple, 3=custom
        public AnimationCurve BasicFoam;
        public AnimationCurve LiteFoam;
        public AnimationCurve MediumFoam;
        public AnimationCurve DenseFoam;

        // Foam curves
        public FoamSettings()
        {
            FoamType = 0;
            BasicFoam = new AnimationCurve(new Keyframe[2]{new Keyframe(0.25f, 0f),
                new Keyframe(1f, 1f)});
            LiteFoam = new AnimationCurve(new Keyframe[3]{new Keyframe(0.2f, 0f),
                new Keyframe(0.4f, 1f),
                new Keyframe(0.7f, 0f)});
            MediumFoam = new AnimationCurve(new Keyframe[3]{new Keyframe(0.4f, 0f),
                new Keyframe(0.7f, 1f),
                new Keyframe(1f, 0f)});
            DenseFoam = new AnimationCurve(new Keyframe[2]{new Keyframe(0.7f, 0f),
                new Keyframe(1f, 1f)});
        }
    }
}
