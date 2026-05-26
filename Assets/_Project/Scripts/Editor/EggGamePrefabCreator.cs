using DinoAlkkagi.Presentation;
using UnityEditor;
using UnityEngine;

namespace DinoAlkkagi.Editor
{
    public static class EggGamePrefabCreator
    {
        private const string PhysicMaterialPath = "Assets/_Project/Physics/Egg_PhysicMaterial.physicMaterial";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Eggs";

        private static readonly EggPrefabSpec[] Specs =
        {
            new EggPrefabSpec(
                "EmbercoreEgg",
                "Assets/_Project/Art/Models/EggSkins_TextureProjected/Embercore/EggSkin_Embercore_Seamless.glb",
                EggSkinFxTheme.Embercore),
            new EggPrefabSpec(
                "PrismhornEgg",
                "Assets/_Project/Art/Models/EggSkins_TextureProjected/Prismhorn/EggSkin_Prismhorn_Seamless.glb",
                EggSkinFxTheme.Prismhorn),
            new EggPrefabSpec(
                "TidecrestEgg",
                "Assets/_Project/Art/Models/EggSkins_TextureProjected/Tidecrest/EggSkin_Tidecrest_Seamless.glb",
                EggSkinFxTheme.Tidecrest),
            new EggPrefabSpec(
                "ToxitideEgg",
                "Assets/_Project/Art/Models/EggSkins_TextureProjected/Toxitide/EggSkin_Toxitide_Seamless.glb",
                EggSkinFxTheme.Toxitide)
        };

        [MenuItem("Tools/Ovum Rumble/Create Game Egg Prefabs")]
        public static void CreateAll()
        {
            PhysicsMaterial physicMaterial =
                AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(PhysicMaterialPath);
            if (physicMaterial == null)
            {
                Debug.LogError($"[{nameof(EggGamePrefabCreator)}] Missing physic material: {PhysicMaterialPath}");
                return;
            }

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                Debug.LogError($"[{nameof(EggGamePrefabCreator)}] Missing prefab folder: {PrefabFolder}");
                return;
            }

            foreach (EggPrefabSpec spec in Specs)
            {
                CreatePrefab(spec, physicMaterial);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreatePrefab(EggPrefabSpec spec, PhysicsMaterial physicMaterial)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(spec.ModelPath);
            if (source == null)
            {
                Debug.LogError($"[{nameof(EggGamePrefabCreator)}] Missing model: {spec.ModelPath}");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"[{nameof(EggGamePrefabCreator)}] Failed to instantiate model: {spec.ModelPath}");
                return;
            }

            instance.name = spec.PrefabName;
            instance.layer = LayerMask.NameToLayer("Egg");
            AddPhysics(instance, physicMaterial);
            AddEggController(instance);
            AddTheme(instance, spec.Theme);

            string prefabPath = $"{PrefabFolder}/{spec.PrefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            Debug.Log($"[{nameof(EggGamePrefabCreator)}] Created {prefabPath}");
        }

        private static void AddPhysics(GameObject instance, PhysicsMaterial physicMaterial)
        {
            SphereCollider collider = instance.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = instance.AddComponent<SphereCollider>();
            }

            collider.material = physicMaterial;
            FitColliderToRenderers(instance, collider);

            Rigidbody rigidbody = instance.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = instance.AddComponent<Rigidbody>();
            }

            rigidbody.mass = 1f;
            rigidbody.linearDamping = 0.2f;
            rigidbody.angularDamping = 0.3f;
            rigidbody.useGravity = true;
            rigidbody.isKinematic = false;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private static void FitColliderToRenderers(GameObject instance, SphereCollider collider)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                collider.center = new Vector3(0f, 0.5f, 0f);
                collider.radius = 0.5f;
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            collider.center = instance.transform.InverseTransformPoint(bounds.center);
            collider.radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        }

        private static void AddEggController(GameObject instance)
        {
            EggController eggController = instance.GetComponent<EggController>();
            if (eggController == null)
            {
                eggController = instance.AddComponent<EggController>();
            }

            eggController.Initialize(0);
        }

        private static void AddTheme(GameObject instance, EggSkinFxTheme theme)
        {
            EggSkinTheme skinTheme = instance.GetComponent<EggSkinTheme>();
            if (skinTheme == null)
            {
                skinTheme = instance.AddComponent<EggSkinTheme>();
            }

            SerializedObject serializedObject = new SerializedObject(skinTheme);
            serializedObject.FindProperty("theme").enumValueIndex = (int)theme;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private readonly struct EggPrefabSpec
        {
            public EggPrefabSpec(string prefabName, string modelPath, EggSkinFxTheme theme)
            {
                PrefabName = prefabName;
                ModelPath = modelPath;
                Theme = theme;
            }

            public string PrefabName { get; }
            public string ModelPath { get; }
            public EggSkinFxTheme Theme { get; }
        }
    }
}
