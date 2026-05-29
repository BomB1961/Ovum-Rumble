3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900   1) using UnityEngine;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900   2) using DinoAlkkagi.Core;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900   3) 
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900   4) namespace DinoAlkkagi.Environment
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900   5) {
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900   6)     [System.Serializable]
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900   7)     public class LightingPreset
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900   8)     {
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900   9)         public Color directionalLightColor = Color.white;
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900  10)         public float directionalLightIntensity = 1f;
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900  11)         public Vector3 directionalLightRotation = new Vector3(50f, -30f, 0f);
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900  12)         public Color ambientSkyColor = new Color(0.212f, 0.227f, 0.259f);
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900  13)         public Color ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f);
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900  14)         public float ambientIntensity = 1f;
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900  15)     }
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900  16) 
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  17)     [System.Serializable]
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  18)     public class SkyboxPreset
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  19)     {
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  20)         public string displayName;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  21)         public Material skyMaterial;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  22)         public Material cloudMaterial;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  23)         public Vector3 sunDirectionEuler;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  24)         public Vector3 moonDirectionEuler;
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900  25)         public LightingPreset lighting = new LightingPreset();
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  26)     }
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  27) 
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  28)     public class SkyboxManager : MonoBehaviour
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  29)     {
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  30)         [SerializeField] private SkyboxPreset dayPreset;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  31)         [SerializeField] private SkyboxPreset sunsetPreset;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  32)         [SerializeField] private SkyboxPreset nightPreset;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  33) 
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  34)         [SerializeField] private MeshRenderer skySphereRenderer;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  35)         [SerializeField] private MeshRenderer[] cloudRenderers;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  36)         [SerializeField] private Transform sunDir;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  37)         [SerializeField] private Transform moonDir;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  38)         [SerializeField] private DirectionToSkybox directionToSkybox;
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900  39)         [SerializeField] private Light directionalLight;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  40) 
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  41)         private MapId currentMap;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  42) 
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  43)         public void ConfigureForMap(MapId mapId)
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  44)         {
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  45)             currentMap = mapId;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  46)             SkyboxPreset preset = GetPreset(mapId);
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  47)             if (preset == null)
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  48)             {
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  49)                 Debug.LogError($"[SkyboxManager] No preset for {mapId}");
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  50)                 return;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  51)             }
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  52) 
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  53)             ApplyPreset(preset);
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  54)         }
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  55) 
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  56)         private SkyboxPreset GetPreset(MapId mapId)
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  57)         {
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  58)             switch (mapId)
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  59)             {
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  60)                 case MapId.Ice: return dayPreset;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  61)                 case MapId.Desert: return sunsetPreset;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  62)                 case MapId.Terrian: return nightPreset;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  63)                 default: return dayPreset;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  64)             }
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  65)         }
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  66) 
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  67)         private void ApplyPreset(SkyboxPreset preset)
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  68)         {
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  69)             if (preset.skyMaterial != null && skySphereRenderer != null)
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  70)                 skySphereRenderer.sharedMaterial = preset.skyMaterial;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  71) 
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  72)             if (preset.cloudMaterial != null && cloudRenderers != null)
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  73)             {
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  74)                 foreach (var renderer in cloudRenderers)
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  75)                 {
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  76)                     if (renderer != null)
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  77)                         renderer.sharedMaterial = preset.cloudMaterial;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  78)                 }
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  79)             }
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  80) 
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  81)             if (sunDir != null)
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  82)                 sunDir.localEulerAngles = preset.sunDirectionEuler;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  83) 
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  84)             if (moonDir != null)
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  85)                 moonDir.localEulerAngles = preset.moonDirectionEuler;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  86) 
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  87)             if (directionToSkybox != null)
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  88)             {
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  89)                 directionToSkybox.targetMaterial = preset.skyMaterial;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  90)                 directionToSkybox.targetMaterialCloudTA = preset.cloudMaterial;
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  91)             }
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  92) 
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900  93)             ApplyLighting(preset.lighting);
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900  94) 
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  95)             Debug.Log($"[SkyboxManager] Applied preset: {preset.displayName}");
3e1e7ffd (Imsehoon 2026-05-22 18:02:03 +0900  96)         }
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900  97) 
859d64ff (Imsehoon 2026-05-26 11:26:25 +0900  98)         private void ApplyLighting(Lighting