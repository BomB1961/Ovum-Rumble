using UnityEngine;

namespace DinoAlkkagi.Environment
{
    [ExecuteAlways]
    public class DirectionToSkybox : MonoBehaviour
    {
        public GameObject sun;
        public GameObject moon;
        public Material targetMaterial;
        public Material targetMaterialCloudTA;
        public Material targetMaterialCloudTB;
        public string sunDirectionPropertyName = "_SunDirection";
        public string moonDirectionPropertyName = "_MoonDirection";

        private Matrix4x4 lastMoonLocalToWorld;
        private Vector3 lastSunDirection;
        private Vector3 lastMoonDirection;
        private Material lastTargetMaterial;
        private Material lastTargetMaterialCloudTA;
        private Material lastTargetMaterialCloudTB;
        private string lastSunPropertyName;
        private string lastMoonPropertyName;
        private bool hasSynced;

        private void OnEnable()
        {
            hasSynced = false;
            SynchronizeDirections();
        }

        private void Update()
        {
            SynchronizeDirections();
        }

        private void SynchronizeDirections()
        {
            if (targetMaterial == null)
            {
                hasSynced = false;
                return;
            }

            bool targetChanged = !hasSynced
                || targetMaterial != lastTargetMaterial
                || targetMaterialCloudTA != lastTargetMaterialCloudTA
                || targetMaterialCloudTB != lastTargetMaterialCloudTB
                || sunDirectionPropertyName != lastSunPropertyName
                || moonDirectionPropertyName != lastMoonPropertyName;

            if (moon)
            {
                Matrix4x4 moonLocalToWorld = moon.transform.localToWorldMatrix;
                if (targetChanged || moonLocalToWorld != lastMoonLocalToWorld)
                {
                    targetMaterial.SetMatrix("LToW", moonLocalToWorld);
                    lastMoonLocalToWorld = moonLocalToWorld;
                }
            }

            if (sun)
            {
                Vector3 sunDirection = -sun.transform.forward.normalized;
                if (targetChanged || sunDirection != lastSunDirection)
                {
                    targetMaterial.SetVector(sunDirectionPropertyName, sunDirection);
                    if (targetMaterialCloudTA) targetMaterialCloudTA.SetVector(sunDirectionPropertyName, sunDirection);
                    if (targetMaterialCloudTB) targetMaterialCloudTB.SetVector(sunDirectionPropertyName, sunDirection);
                    lastSunDirection = sunDirection;
                }
            }

            if (moon)
            {
                Vector3 moonDirection = -moon.transform.forward.normalized;
                if (targetChanged || moonDirection != lastMoonDirection)
                {
                    targetMaterial.SetVector(moonDirectionPropertyName, moonDirection);
                    if (targetMaterialCloudTA) targetMaterialCloudTA.SetVector(moonDirectionPropertyName, moonDirection);
                    if (targetMaterialCloudTB) targetMaterialCloudTB.SetVector(moonDirectionPropertyName, moonDirection);
                    lastMoonDirection = moonDirection;
                }
            }

            lastTargetMaterial = targetMaterial;
            lastTargetMaterialCloudTA = targetMaterialCloudTA;
            lastTargetMaterialCloudTB = targetMaterialCloudTB;
            lastSunPropertyName = sunDirectionPropertyName;
            lastMoonPropertyName = moonDirectionPropertyName;
            hasSynced = true;
        }
    }
}
