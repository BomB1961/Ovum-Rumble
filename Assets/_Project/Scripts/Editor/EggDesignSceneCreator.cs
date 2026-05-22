using System.IO;
using DinoAlkkagi.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DinoAlkkagi.Editor
{
    public static class EggDesignSceneCreator
    {
        private const string ScenePath = "Assets/_Project/Scenes/EggDesign.unity";
        private const string EggPath = "Assets/_Project/Art/Models/EggSkins_TextureProjected/Embercore/EggSkin_Embercore_Seamless.glb";
        private const string ImpactFxPath = "Assets/_Project/Art/Models/EggSkins_TextureProjected/Embercore/EggSkin_Embercore_ImpactFX.glb";

        [MenuItem("Tools/Ovum Rumble/Create Egg Design Scene")]
        public static void CreateScene()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Assets/_Project/Scenes");

            GameObject eggAsset = AssetDatabase.LoadAssetAtPath<GameObject>(EggPath);
            GameObject impactFxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ImpactFxPath);

            if (eggAsset == null)
            {
                Debug.LogError($"[EggDesignSceneCreator] Could not load egg asset at {EggPath}");
                return;
            }

            if (impactFxAsset == null)
            {
                Debug.LogError($"[EggDesignSceneCreator] Could not load impact FX asset at {ImpactFxPath}");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "EggDesign";

            CreateFloor();
            GameObject egg = CreateEgg(eggAsset);
            GameObject impactFx = CreateImpactFx(impactFxAsset);
            CreatePreviewController(egg.transform, impactFx.transform);
            CreateLighting();
            CreateCamera();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[EggDesignSceneCreator] Created {ScenePath}");
        }

        private static void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Preview_Floor";
            floor.transform.position = new Vector3(0f, -0.04f, 0f);
            floor.transform.localScale = new Vector3(8f, 0.08f, 6f);

            Material floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            floorMaterial.name = "MAT_EggDesign_PreviewFloor";
            floorMaterial.color = new Color(0.18f, 0.18f, 0.16f, 1f);

            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
        }

        private static GameObject CreateEgg(GameObject eggAsset)
        {
            GameObject egg = PrefabUtility.InstantiatePrefab(eggAsset) as GameObject;
            if (egg == null)
            {
                throw new MissingReferenceException("Failed to instantiate Embercore egg asset.");
            }

            egg.name = "Embercore_Egg_Visual";
            egg.transform.position = Vector3.zero;
            egg.transform.rotation = Quaternion.identity;
            FitObjectHeight(egg, 2.2f);
            MoveBottomToY(egg, 0f);

            GameObject colliderRoot = new GameObject("Embercore_Egg_GameCollider_PreviewOnly");
            colliderRoot.transform.SetParent(egg.transform, false);

            SphereCollider sphereCollider = colliderRoot.AddComponent<SphereCollider>();
            sphereCollider.radius = 0.78f;
            sphereCollider.center = new Vector3(0f, 0.82f, 0f);

            Rigidbody rigidbody = colliderRoot.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            return egg;
        }

        private static GameObject CreateImpactFx(GameObject impactFxAsset)
        {
            GameObject impactFx = PrefabUtility.InstantiatePrefab(impactFxAsset) as GameObject;
            if (impactFx == null)
            {
                throw new MissingReferenceException("Failed to instantiate Embercore impact FX asset.");
            }

            impactFx.name = "Embercore_ImpactFX_LoopingPreview";
            impactFx.transform.position = new Vector3(0f, 1.08f, -0.82f);
            impactFx.transform.rotation = Quaternion.identity;
            impactFx.transform.localScale = Vector3.one;

            return impactFx;
        }

        private static void CreatePreviewController(Transform eggRoot, Transform impactFxRoot)
        {
            GameObject controller = new GameObject("EggDesign_ImpactFXPreviewController");
            EggDesignImpactFxPreview preview = controller.AddComponent<EggDesignImpactFxPreview>();

            SerializedObject serializedObject = new SerializedObject(preview);
            serializedObject.FindProperty("eggRoot").objectReferenceValue = eggRoot;
            serializedObject.FindProperty("impactFxRoot").objectReferenceValue = impactFxRoot;
            serializedObject.FindProperty("cycleDuration").floatValue = 2.4f;
            serializedObject.FindProperty("activeDuration").floatValue = 1.1f;
            serializedObject.FindProperty("shardTravel").floatValue = 0.85f;
            serializedObject.FindProperty("sparkTravel").floatValue = 1.25f;
            serializedObject.FindProperty("eggSpinSpeed").floatValue = 12f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateLighting()
        {
            GameObject directional = new GameObject("Directional Light");
            Light directionalLight = directional.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 2.8f;
            directionalLight.color = new Color(1f, 0.92f, 0.82f, 1f);
            directional.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            GameObject lavaLight = new GameObject("Preview_Impact_Orange_PointLight");
            Light point = lavaLight.AddComponent<Light>();
            point.type = LightType.Point;
            point.range = 4.5f;
            point.intensity = 4.5f;
            point.color = new Color(1f, 0.28f, 0.04f, 1f);
            lavaLight.transform.position = new Vector3(0f, 1.05f, -0.8f);
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 35f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(0f, 1.45f, -5.2f);
            cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.08f, 0f) - cameraObject.transform.position, Vector3.up);
        }

        private static Bounds GetRendererBounds(GameObject gameObject)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(gameObject.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void FitObjectHeight(GameObject gameObject, float targetHeight)
        {
            Bounds bounds = GetRendererBounds(gameObject);
            if (bounds.size.y <= 0.0001f)
            {
                return;
            }

            float scale = targetHeight / bounds.size.y;
            gameObject.transform.localScale *= scale;
        }

        private static void MoveBottomToY(GameObject gameObject, float y)
        {
            Bounds bounds = GetRendererBounds(gameObject);
            gameObject.transform.position += new Vector3(0f, y - bounds.min.y, 0f);
        }
    }
}
