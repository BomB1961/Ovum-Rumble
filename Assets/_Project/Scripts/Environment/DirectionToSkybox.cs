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

        void Update()
        {
            if (targetMaterial == null)
                return;

            if (moon)
            {
                Matrix4x4 LtoW = moon.transform.localToWorldMatrix;
                targetMaterial.SetMatrix("LToW", LtoW);
            }

            if (sun)
            {
                Vector3 sunDirection = -sun.transform.forward.normalized;
                targetMaterial.SetVector(sunDirectionPropertyName, sunDirection);
                if (targetMaterialCloudTA) targetMaterialCloudTA.SetVector(sunDirectionPropertyName, sunDirection);
                if (targetMaterialCloudTB) targetMaterialCloudTB.SetVector(sunDirectionPropertyName, sunDirection);
            }

            if (moon)
            {
                Vector3 moonDirection = -moon.transform.forward.normalized;
                targetMaterial.SetVector(moonDirectionPropertyName, moonDirection);
                if (targetMaterialCloudTA) targetMaterialCloudTA.SetVector(moonDirectionPropertyName, moonDirection);
                if (targetMaterialCloudTB) targetMaterialCloudTB.SetVector(moonDirectionPropertyName, moonDirection);
            }
        }
    }
}
