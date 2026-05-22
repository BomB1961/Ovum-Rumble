using UnityEngine;
using DinoAlkkagi.Core;

namespace DinoAlkkagi.Environment
{
    [System.Serializable]
    public class SkyboxPreset
    {
        public string displayName;
        public Material skyMaterial;
        public Material cloudMaterial;
        public Vector3 sunDirectionEuler;
        public Vector3 moonDirectionEuler;
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

            Debug.Log($"[SkyboxManager] Applied preset: {preset.displayName}");
        }
    }
}
