using UnityEngine;
using DinoAlkkagi.Core;

namespace DinoAlkkagi.Environment
{
    [System.Serializable]
    public class LightingPreset
    {
        public Color directionalLightColor = Color.white;
        public float directionalLightIntensity = 1f;
        public Vector3 directionalLightRotation = new Vector3(50f, -30f, 0f);
        public Color ambientSkyColor = new Color(0.212f, 0.227f, 0.259f);
        public Color ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f);
        public float ambientIntensity = 1f;
    }

    [System.Serializable]
    public class SkyboxPreset
    {
        public string displayName;
        public Material skyMaterial;
        public Material cloudMaterial;
        public Vector3 sunDirectionEuler;
        public Vector3 moonDirectionEuler;
        public LightingPreset lighting = new LightingPreset();
    }

    public class SkyboxManager : MonoBehaviour
    {
        [SerializeField] private SkyboxPreset dayPreset;
        [SerializeField] private SkyboxPreset sunsetPreset;
        [SerializeField] private SkyboxPreset nightPreset;

        [SerializeField] private MeshRenderer skySphereRenderer;
        [SerializeField] private MeshRenderer[] cloudRenderers;
        [SerializeField] private Transform sunDir;
        [SerializeField] private Transform moonDir;
        [SerializeField] private DirectionToSkybox directionToSkybox;
        [SerializeField] private Light directionalLight;

        private MapId currentMap;

        public void ConfigureForMap(MapId mapId)
        {
            currentMap = mapId;
            SkyboxPreset preset = GetPreset(mapId);
            if (preset == null)
            {
                Debug.LogError($"[SkyboxManager] No preset for {mapId}");
                return;
            }

            ApplyPreset(preset);
        }

        private SkyboxPreset GetPreset(MapId mapId)
        {
            switch (mapId)
            {
                case MapId.Ice: return dayPreset;
                case MapId.Desert: return sunsetPreset;
                case MapId.Terrian: return nightPreset;
                default: return dayPreset;
            }
        }

        private void ApplyPreset(SkyboxPreset preset)
        {
            if (preset.skyMaterial != null && skySphereRenderer != null)
                skySphereRenderer.sharedMaterial = preset.skyMaterial;

            if (preset.cloudMaterial != null && cloudRenderers != null)
            {
                foreach (var renderer in cloudRenderers)
                {
                    if (renderer != null)
                        renderer.sharedMaterial = preset.cloudMaterial;
                }
            }

            if (sunDir != null)
                sunDir.localEulerAngles = preset.sunDirectionEuler;

            if (moonDir != null)
                moonDir.localEulerAngles = preset.moonDirectionEuler;

            if (directionToSkybox != null)
            {
                directionToSkybox.targetMaterial = preset.skyMaterial;
                directionToSkybox.targetMaterialCloudTA = preset.cloudMaterial;
            }

            ApplyLighting(preset.lighting);

            Debug.Log($"[SkyboxManager] Applied preset: {preset.displayName}");
        }

        private void ApplyLighting(LightingPreset lighting)
        {
            if (lighting == null) return;

            if (directionalLight != null)
            {
                directionalLight.color = lighting.directionalLightColor;
                directionalLight.intensity = lighting.directionalLightIntensity;
                directionalLight.transform.localEulerAngles = lighting.directionalLightRotation;
            }

            RenderSettings.ambientSkyColor = lighting.ambientSkyColor;
            RenderSettings.ambientEquatorColor = lighting.ambientEquatorColor;
            RenderSettings.ambientIntensity = lighting.ambientIntensity;
        }
    }
}
