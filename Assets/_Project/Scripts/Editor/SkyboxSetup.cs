using UnityEngine;
using UnityEditor;
using System.IO;
using DinoAlkkagi.Environment;

namespace DinoAlkkagi.Editor
{
    public static class SkyboxSetup
    {
        private const string BasePath = "Assets/_Project/Art/Skybox";
        private const string ShaderPath = BasePath + "/Shaders";
        private const string TexturePath = BasePath + "/Textures";
        private const string MaterialPath = BasePath + "/Materials";

        [MenuItem("Tools/Skybox/Generate IrradianceMap")]
        public static void GenerateIrradianceMap()
        {
            EnsureFolder(TexturePath);

            int width = 256;
            int height = 1;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);

            for (int x = 0; x < width; x++)
            {
                float t = (float)x / (width - 1);
                float r = Mathf.SmoothStep(0f, 1f, t);
                float g = Mathf.SmoothStep(0f, 0.7f, t) * (1f - Mathf.SmoothStep(0.7f, 1f, t)) * 4f;
                tex.SetPixel(x, 0, new Color(r, g, 0f, 1f));
            }

            tex.Apply();
            string path = Path.Combine(TexturePath, "IrradianceMap.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            Debug.Log($"[SkyboxSetup] Generated IrradianceMap at {path}");
        }

        [MenuItem("Tools/Skybox/Create All Materials")]
        public static void CreateAllMaterials()
        {
            EnsureFolder(MaterialPath);

            Shader skyShader = AssetDatabase.LoadAssetAtPath<Shader>(ShadersPath("GenshinSky.shader"));
            Shader cloudShader = AssetDatabase.LoadAssetAtPath<Shader>(ShadersPath("GenshinCloud.shader"));

            if (skyShader == null) { Debug.LogError("GenshinSky.shader not found"); return; }
            if (cloudShader == null) { Debug.LogError("GenshinCloud.shader not found"); return; }

            Texture2D irradianceMap = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath("IrradianceMap.png"));
            Texture2D galaxyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath("GalaxyTex.png"));
            Texture2D moonTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath("MoonTex.jpg"));
            Texture2D noiseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath("NoiseMap.png"));
            Texture2D starLut = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath("StarColorLut.png"));
            Texture2D starDot = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath("StarDotMap.png"));
            Texture2D cloudA = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath("CloudMap_A.png"));
            Texture2D cloudB = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath("CloudMap_B.png"));

            // ===== Sky_Day (from 5456 1.mat) =====
            Material skyDay = CreateMaterial(skyShader, "Sky_Day", MaterialPath);
            skyDay.SetColor("_upPartSunColor", new Color(0.940f, 0.865f, 0.818f));
            skyDay.SetColor("_upPartSkyColor", new Color(0.028f, 0.453f, 0.808f));
            skyDay.SetColor("_downPartSunColor", new Color(0.757f, 0.939f, 0.933f));
            skyDay.SetColor("_downPartSkyColor", new Color(0.412f, 0.706f, 0.816f));
            skyDay.SetFloat("_IrradianceMapR_maxAngleRange", 0.432f);
            skyDay.SetFloat("_mainColorSunGatherFactor", 1.904f);
            skyDay.SetColor("_SunAdditionColor", new Color(0.847f, 0.887f, 0.672f));
            skyDay.SetFloat("_SunAdditionIntensity", 0.269f);
            skyDay.SetFloat("_IrradianceMapG_maxAngleRange", 0.234f);
            skyDay.SetFloat("_SunRadius", 4.1f);
            skyDay.SetFloat("_SunInnerBoundary", 0f);
            skyDay.SetFloat("_SunOuterBoundary", 3.182f);
            skyDay.SetFloat("_sun_disk_power_999", 663.6f);
            skyDay.SetFloat("_SunScattering", 1f);
            skyDay.SetFloat("_sun_color_intensity", 1.043f);
            skyDay.SetColor("_sun_color", new Color(0.994f, 0.969f, 0.939f));
            skyDay.SetColor("_sun_color_Scat", new Color(0.906f, 0.430f, 0.117f));
            skyDay.SetVector("_SunDirection", new Vector4(-0.261f, 0.122f, -0.958f, 0));
            skyDay.SetVector("_MoonDirection", new Vector4(-0.333f, -0.119f, 0.935f, 0));
            skyDay.SetFloat("_MoonRadius", 3f);
            skyDay.SetFloat("_MoonMaskRadius", 5f);
            skyDay.SetFloat("_Moon_color_intensity", 1.185f);
            skyDay.SetFloat("_mainColorMoonGatherFactor", 0.313f);
            skyDay.SetColor("_MoonScatteringColor", Color.white);
            skyDay.SetColor("_Moon_color", new Color(0.906f, 0.430f, 0.117f));
            skyDay.SetFloat("_NoiseSpeed", 0f);
            skyDay.SetFloat("_galaxy_INT", 0.2f);
            skyDay.SetFloat("_galaxy_intensity", 0f);
            skyDay.SetFloat("_starColorIntensity", 0f);
            if (irradianceMap) skyDay.SetTexture("_IrradianceMap", irradianceMap);
            if (moonTex) skyDay.SetTexture("_MoonTex", moonTex);

            // ===== Sky_Sunset (from 5456.mat) =====
            Material skySunset = CreateMaterial(skyShader, "Sky_Sunset", MaterialPath);
            skySunset.SetColor("_upPartSunColor", new Color(1.000f, 0.879f, 0.593f));
            skySunset.SetColor("_upPartSkyColor", new Color(0.318f, 0.596f, 0.805f));
            skySunset.SetColor("_downPartSunColor", new Color(0.929f, 0.937f, 0.863f));
            skySunset.SetColor("_downPartSkyColor", new Color(0.648f, 0.859f, 0.955f));
            skySunset.SetFloat("_IrradianceMapR_maxAngleRange", 0.347f);
            skySunset.SetFloat("_mainColorSunGatherFactor", 1.718f);
            skySunset.SetColor("_SunAdditionColor", new Color(0.920f, 0.894f, 0.651f));
            skySunset.SetFloat("_SunAdditionIntensity", 0.624f);
            skySunset.SetFloat("_IrradianceMapG_maxAngleRange", 0.625f);
            skySunset.SetFloat("_SunRadius", 8.3f);
            skySunset.SetFloat("_SunInnerBoundary", 0f);
            skySunset.SetFloat("_SunOuterBoundary", 0.822f);
            skySunset.SetFloat("_sun_disk_power_999", 164.4f);
            skySunset.SetFloat("_SunScattering", 0.194f);
            skySunset.SetFloat("_sun_color_intensity", 1.241f);
            skySunset.SetColor("_sun_color", new Color(0.997f, 0.840f, 0.678f));
            skySunset.SetColor("_sun_color_Scat", new Color(0.283f, 0.283f, 0.283f));
            skySunset.SetVector("_SunDirection", new Vector4(0f, 0.014f, -1.0f, 0));
            skySunset.SetVector("_MoonDirection", new Vector4(-0.905f, 0.342f, -0.254f, 0));
            skySunset.SetFloat("_MoonRadius", 2.15f);
            skySunset.SetFloat("_MoonMaskRadius", 5.49f);
            skySunset.SetFloat("_Moon_color_intensity", 1.39f);
            skySunset.SetFloat("_mainColorMoonGatherFactor", 0.74f);
            skySunset.SetColor("_MoonScatteringColor", new Color(0.134f, 0.364f, 0.557f));
            skySunset.SetColor("_Moon_color", new Color(0.538f, 0.538f, 0.538f));
            skySunset.SetFloat("_NoiseSpeed", 0.079f);
            skySunset.SetFloat("_galaxy_INT", 0.2f);
            skySunset.SetFloat("_galaxy_intensity", 0f);
            skySunset.SetFloat("_starColorIntensity", 8.033f);
            skySunset.SetFloat("_starIntensityLinearDamping", 0.557f);
            skySunset.SetVector("_StarDotMap_ST", new Vector4(1, 1.2f, 0, 0));
            skySunset.SetVector("_StarColorLut_ST", new Vector4(0.5f, 1, 0, 0));
            if (irradianceMap) skySunset.SetTexture("_IrradianceMap", irradianceMap);
            if (moonTex) skySunset.SetTexture("_MoonTex", moonTex);
            if (noiseMap) skySunset.SetTexture("_NoiseMap", noiseMap);
            if (starDot) skySunset.SetTexture("_StarDotMap", starDot);
            if (starLut) skySunset.SetTexture("StarColorLut", starLut);
            if (galaxyTex)
            {
                skySunset.SetTexture("_galaxyTex", galaxyTex);
                skySunset.SetVector("_galaxyTex_ST", new Vector4(0.45f, 0.61f, 0.55f, 0.6f));
            }

            // ===== Sky_Night (from 333.mat) =====
            Material skyNight = CreateMaterial(skyShader, "Sky_Night", MaterialPath);
            skyNight.SetColor("_upPartSunColor", new Color(0.003f, 0.182f, 0.631f));
            skyNight.SetColor("_upPartSkyColor", new Color(0.029f, 0.161f, 0.279f));
            skyNight.SetColor("_downPartSunColor", new Color(0.308f, 0.346f, 0.246f));
            skyNight.SetColor("_downPartSkyColor", new Color(0.043f, 0.262f, 0.470f));
            skyNight.SetFloat("_IrradianceMapR_maxAngleRange", 0.103f);
            skyNight.SetFloat("_mainColorSunGatherFactor", 0.493f);
            skyNight.SetColor("_SunAdditionColor", new Color(0.325f, 0.951f, 1.0f));
            skyNight.SetFloat("_SunAdditionIntensity", 0.286f);
            skyNight.SetFloat("_IrradianceMapG_maxAngleRange", 0.672f);
            skyNight.SetFloat("_SunRadius", 0.5f);
            skyNight.SetFloat("_SunInnerBoundary", 1f);
            skyNight.SetFloat("_SunOuterBoundary", 1f);
            skyNight.SetFloat("_sun_disk_power_999", 287f);
            skyNight.SetFloat("_SunScattering", 0.623f);
            skyNight.SetFloat("_sun_color_intensity", 0f);
            skyNight.SetColor("_sun_color", new Color(1f, 0.682f, 0f));
            skyNight.SetColor("_sun_color_Scat", new Color(0.274f, 0.274f, 0.274f));
            skyNight.SetFloat("_MoonRadius", 3f);
            skyNight.SetFloat("_MoonMaskRadius", 5f);
            skyNight.SetFloat("_Moon_color_intensity", 1.185f);
            skyNight.SetFloat("_mainColorMoonGatherFactor", 0.313f);
            skyNight.SetColor("_MoonScatteringColor", Color.white);
            skyNight.SetColor("_Moon_color", new Color(0.906f, 0.430f, 0.117f));
            skyNight.SetVector("_SunDirection", new Vector4(-0.261f, 0.122f, -0.958f, 0));
            skyNight.SetVector("_MoonDirection", new Vector4(-0.333f, -0.119f, 0.935f, 0));
            skyNight.SetFloat("_starColorIntensity", 0.847f);
            skyNight.SetFloat("_starIntensityLinearDamping", 0.808f);
            skyNight.SetFloat("_NoiseSpeed", 0.293f);
            skyNight.SetFloat("_galaxy_INT", 0f);
            skyNight.SetFloat("_galaxy_intensity", 0f);
            skyNight.SetVector("_StarDotMap_ST", new Vector4(10, 10, 0, 0));
            skyNight.SetVector("_StarColorLut_ST", new Vector4(0.5f, 1, 0, 0));
            if (irradianceMap) skyNight.SetTexture("_IrradianceMap", irradianceMap);
            if (noiseMap) skyNight.SetTexture("_NoiseMap", noiseMap);
            if (starDot) skyNight.SetTexture("_StarDotMap", starDot);
            if (starLut) skyNight.SetTexture("StarColorLut", starLut);

            // ===== Cloud_Day (from Cloud.mat) =====
            Material cloudDay = CreateMaterial(cloudShader, "Cloud_Day", MaterialPath);
            cloudDay.SetColor("_CloudColorA", new Color(0.505f, 0.704f, 0.915f));
            cloudDay.SetColor("_CloudColorB", new Color(0.986f, 0.986f, 1.0f));
            cloudDay.SetColor("_CloudColorC", new Color(0.863f, 0.948f, 1.0f));
            cloudDay.SetColor("_CloudColorD", Color.white);
            cloudDay.SetColor("_Cloud_edgeColor", Color.white);
            cloudDay.SetFloat("_Cloud_SDF_TSb", 0.003f);
            cloudDay.SetFloat("_SunMoon", 0f);
            if (cloudA) cloudDay.SetTexture("_CloudMap", cloudA);
            if (noiseMap) cloudDay.SetTexture("_NoiseMap", noiseMap);

            // ===== Cloud_Sunset (from Cloud 2.mat) =====
            Material cloudSunset = CreateMaterial(cloudShader, "Cloud_Sunset", MaterialPath);
            cloudSunset.SetColor("_CloudColorA", new Color(0.610f, 0.738f, 0.791f));
            cloudSunset.SetColor("_CloudColorB", new Color(0.799f, 0.638f, 0.624f));
            cloudSunset.SetColor("_CloudColorC", new Color(0.905f, 0.922f, 0.776f));
            cloudSunset.SetColor("_CloudColorD", new Color(0.913f, 0.597f, 0.156f));
            cloudSunset.SetColor("_Cloud_edgeColor", new Color(0.799f, 0.624f, 0.565f));
            cloudSunset.SetFloat("_Cloud_SDF_TSb", 0.047f);
            cloudSunset.SetFloat("_SunMoon", 0f);
            if (cloudB) cloudSunset.SetTexture("_CloudMap", cloudB);
            if (noiseMap) cloudSunset.SetTexture("_NoiseMap", noiseMap);

            // ===== Cloud_Night (from Cloud 1.mat) =====
            Material cloudNight = CreateMaterial(cloudShader, "Cloud_Night", MaterialPath);
            cloudNight.SetColor("_CloudColorA", new Color(0.100f, 0.142f, 0.255f));
            cloudNight.SetColor("_CloudColorB", new Color(0.007f, 0.068f, 0.171f));
            cloudNight.SetColor("_CloudColorC", Color.white);
            cloudNight.SetColor("_CloudColorD", Color.white);
            cloudNight.SetColor("_Cloud_edgeColor", Color.white);
            cloudNight.SetFloat("_Cloud_SDF_TSb", 0.122f);
            cloudNight.SetFloat("_SunMoon", 0f);
            if (cloudB) cloudNight.SetTexture("_CloudMap", cloudB);
            if (noiseMap) cloudNight.SetTexture("_NoiseMap", noiseMap);

            AssetDatabase.SaveAssets();
            Debug.Log("[SkyboxSetup] All 6 materials created.");
        }

        [MenuItem("Tools/Skybox/Setup Skybox System in Scene")]
        public static void SetupSkyboxSystem()
        {
            SkyboxManager existing = Object.FindFirstObjectByType<SkyboxManager>();
            if (existing != null)
            {
                Debug.LogWarning("[SkyboxSetup] SkyboxManager already exists in scene. Remove it first to re-create.");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // Load assets
            GameObject skySpherePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePath + "/Meshes/SkySphere.fbx");
            GameObject cloudCardsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePath + "/Meshes/Procedural Skybox.fbx");
            Material skyDay = AssetDatabase.LoadAssetAtPath<Material>(MatPath("Sky_Day.mat"));
            Material cloudDay = AssetDatabase.LoadAssetAtPath<Material>(MatPath("Cloud_Day.mat"));

            if (skySpherePrefab == null) { Debug.LogError("SkySphere.fbx not found"); return; }
            if (skyDay == null) { Debug.LogError("Sky_Day.mat not found — run Create All Materials first"); return; }

            // Create root
            GameObject root = new GameObject("SkyboxSystem");
            Undo.RegisterCreatedObjectUndo(root, "Create SkyboxSystem");

            // SkySphere
            GameObject skySphere = (GameObject)PrefabUtility.InstantiatePrefab(skySpherePrefab, root.transform);
            skySphere.name = "SkySphere";
            skySphere.transform.localPosition = Vector3.zero;
            skySphere.transform.localScale = Vector3.one * 90f;
            MeshRenderer skyRend = skySphere.GetComponent<MeshRenderer>();
            if (skyRend) { skyRend.sharedMaterial = skyDay; skyRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; skyRend.receiveShadows = false; }

            // SunDir
            GameObject sunDir = new GameObject("SunDir");
            sunDir.transform.SetParent(root.transform);
            sunDir.transform.localPosition = Vector3.zero;

            // MoonDir
            GameObject moonDir = new GameObject("MoonDir");
            moonDir.transform.SetParent(root.transform);
            moonDir.transform.localPosition = Vector3.zero;

            // SkyDirection
            GameObject skyDir = new GameObject("SkyDirection");
            skyDir.transform.SetParent(root.transform);
            DirectionToSkybox dts = skyDir.AddComponent<DirectionToSkybox>();
            dts.sun = sunDir;
            dts.moon = moonDir;
            dts.targetMaterial = skyDay;
            dts.targetMaterialCloudTA = cloudDay;

            // Clouds
            GameObject clouds = new GameObject("Clouds");
            clouds.transform.SetParent(root.transform);
            if (cloudCardsPrefab)
            {
                GameObject cloudHigh = (GameObject)PrefabUtility.InstantiatePrefab(cloudCardsPrefab, clouds.transform);
                cloudHigh.name = "CloudHigh";
                cloudHigh.transform.localPosition = Vector3.up * 5f;
                MeshRenderer cloudRend = cloudHigh.GetComponent<MeshRenderer>();
                if (cloudRend) { cloudRend.sharedMaterial = cloudDay; cloudRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; cloudRend.receiveShadows = false; }
            }

            // SkyboxManager
            SkyboxManager manager = root.AddComponent<SkyboxManager>();

            // Assign through serialized fields via undo/record
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("skySphereRenderer").objectReferenceValue = skySphere.GetComponent<MeshRenderer>();
            so.FindProperty("sunDir").objectReferenceValue = sunDir.transform;
            so.FindProperty("moonDir").objectReferenceValue = moonDir.transform;
            so.FindProperty("directionToSkybox").objectReferenceValue = dts;
            so.ApplyModifiedProperties();

            // Set cloud renderers
            if (cloudCardsPrefab)
            {
                var cloudObjects = GameObject.FindGameObjectsWithTag("Untagged");
                // Re-find by name under Clouds parent
                var cloudChildren = clouds.transform.GetComponentsInChildren<MeshRenderer>();
                if (cloudChildren.Length > 0)
                {
                    so.FindProperty("cloudRenderers").arraySize = 1;
                    so.FindProperty("cloudRenderers.Array.data[0]").objectReferenceValue = cloudChildren[0];
                }
            }

            // Set presets
            SkyboxPreset dayPreset = new SkyboxPreset { displayName = "Day", skyMaterial = skyDay, cloudMaterial = cloudDay, sunDirectionEuler = new Vector3(35f, 225f, 0), moonDirectionEuler = new Vector3(-30f, 45f, 0) };
            SkyboxPreset sunsetPreset = new SkyboxPreset { displayName = "Sunset", skyMaterial = AssetDatabase.LoadAssetAtPath<Material>(MatPath("Sky_Sunset.mat")), cloudMaterial = AssetDatabase.LoadAssetAtPath<Material>(MatPath("Cloud_Sunset.mat")), sunDirectionEuler = new Vector3(0f, 180f, 0), moonDirectionEuler = new Vector3(-10f, 0, 0) };
            SkyboxPreset nightPreset = new SkyboxPreset { displayName = "Night", skyMaterial = AssetDatabase.LoadAssetAtPath<Material>(MatPath("Sky_Night.mat")), cloudMaterial = AssetDatabase.LoadAssetAtPath<Material>(MatPath("Cloud_Night.mat")), sunDirectionEuler = new Vector3(-45f, 0, 0), moonDirectionEuler = new Vector3(45f, 90f, 0) };

            // Apply presets using the public field via reflection since it's not exposed in the serialized object easily
            // We'll set via SetValue helper
            SetPresetField(so, "dayPreset", dayPreset);
            SetPresetField(so, "sunsetPreset", sunsetPreset);
            SetPresetField(so, "nightPreset", nightPreset);
            so.ApplyModifiedProperties();

            // Link to StaticBoardLoader
            StaticBoardLoader loader = Object.FindFirstObjectByType<StaticBoardLoader>();
            if (loader)
            {
                SerializedObject loaderSo = new SerializedObject(loader);
                loaderSo.FindProperty("skyboxManager").objectReferenceValue = manager;
                loaderSo.ApplyModifiedProperties();
                Debug.Log("[SkyboxSetup] Linked SkyboxManager to StaticBoardLoader.");
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = Color.black;
                if (mainCam.farClipPlane < 500f)
                    mainCam.farClipPlane = 1000f;
            }

            Selection.activeGameObject = root;
            Debug.Log("[SkyboxSetup] SkyboxSystem created in scene. Configure presets in Inspector.");
        }

        private static Material CreateMaterial(Shader shader, string name, string folder)
        {
            string path = Path.Combine(folder, name + ".mat");
            AssetDatabase.DeleteAsset(path);
            Material mat = new Material(shader);
            mat.name = name;
            AssetDatabase.CreateAsset(mat, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static void EnsureFolder(string path)
        {
            string parent = Path.GetDirectoryName(path);
            string folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(path))
            {
                if (!AssetDatabase.IsValidFolder(parent))
                    EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static void SetPresetField(SerializedObject so, string fieldName, SkyboxPreset preset)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null) return;

            prop.FindPropertyRelative("displayName").stringValue = preset.displayName;
            prop.FindPropertyRelative("skyMaterial").objectReferenceValue = preset.skyMaterial;
            prop.FindPropertyRelative("cloudMaterial").objectReferenceValue = preset.cloudMaterial;
            prop.FindPropertyRelative("sunDirectionEuler").vector3Value = preset.sunDirectionEuler;
            prop.FindPropertyRelative("moonDirectionEuler").vector3Value = preset.moonDirectionEuler;
        }

        private static string ShadersPath(string file) => Path.Combine(ShaderPath, file);
        private static string TexturesPath(string file) => Path.Combine(TexturePath, file);
        private static string MatPath(string file) => Path.Combine(MaterialPath, file);
    }
}
