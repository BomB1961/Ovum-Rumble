using UnityEngine;
using UnityEditor;

namespace DinoAlkkagi.Editor
{
    public static class YoshiEggPrefabCreator
    {
        [MenuItem("Tools/Create Yoshi Egg Prefab")]
        static void CreatePrefab()
        {
            string glbPath = "Assets/_Project/Art/Models/yoshi_egg.glb";
            string prefabPath = "Assets/_Project/Prefabs/Eggs/YoshiEgg.prefab";

            // glb 에셋을 로드 (glTFast가 임포트한 메인 GameObject)
            Object mainAsset = AssetDatabase.LoadMainAssetAtPath(glbPath);
            if (mainAsset == null)
            {
                Debug.LogError($"[YoshiEggPrefabCreator] Could not load asset at {glbPath}");
                return;
            }

            GameObject glbGameObject = mainAsset as GameObject;
            if (glbGameObject == null)
            {
                Debug.LogError($"[YoshiEggPrefabCreator] Main asset is not a GameObject. Type: {mainAsset.GetType()}");
                return;
            }

            // 씬에 임시 인스턴스 생성
            GameObject temp = PrefabUtility.InstantiatePrefab(glbGameObject) as GameObject;
            if (temp == null)
            {
                Debug.LogError("[YoshiEggPrefabCreator] Failed to instantiate glb asset.");
                return;
            }

            // 기존 Egg.prefab과 동일한 물리 컴포넌트 추가
            // Layer 8 (Egg)
            temp.layer = 8;

            // SphereCollider
            if (temp.GetComponent<SphereCollider>() == null)
            {
                var col = temp.AddComponent<SphereCollider>();
                col.radius = 1.8f;
                col.center = new Vector3(0f, 0.3f, -0.075f);
                // PhysicMaterial이 있다면 적용 (기존 Egg와 동일)
                string physMatPath = "Assets/_Project/Physics/Egg_PhysicMaterial.physicMaterial";
                PhysicsMaterial physMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(physMatPath);
                if (physMat != null)
                    col.material = physMat;
            }

            // Rigidbody
            if (temp.GetComponent<Rigidbody>() == null)
            {
                var rb = temp.AddComponent<Rigidbody>();
                rb.mass = 1f;
                rb.linearDamping = 0.2f;
                rb.angularDamping = 0.3f;
                rb.useGravity = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            // EggController
            if (temp.GetComponent<EggController>() == null)
            {
                var ec = temp.AddComponent<EggController>();
                ec.Initialize(0);
            }

            // 프리팹으로 저장
            PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);

            // 임시 오브젝트 삭제
            Object.DestroyImmediate(temp);

            AssetDatabase.Refresh();
            Debug.Log($"[YoshiEggPrefabCreator] YoshiEgg prefab created at {prefabPath}");
        }
    }
}
