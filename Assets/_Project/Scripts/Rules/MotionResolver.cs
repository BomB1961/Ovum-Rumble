using UnityEngine;
using System.Collections.Generic;
using DinoAlkkagi.Core;
using DinoAlkkagi.Data;

namespace DinoAlkkagi.Rules
{
    /// <summary>
    /// Person B 전용 — 모든 알의 정지 상태를 감시하고 턴 종료를 판정한다.
    /// 기획서 6-2 정지 판정 및 강제 턴 종료 알고리즘(6-2, 6-3)을 구현한다.
    /// </summary>
    public class MotionResolver : MonoBehaviour
    {
        [SerializeField] private GameSettings settings;

        private List<EggController> trackedEggs = new List<EggController>();
        private float stopTimer = 0f;
        private float resolveTimer = 0f;
        private bool isResolving = false;

        public bool IsResolving => isResolving;
        public float ResolveTime => resolveTimer;

        private void OnEnable()
        {
            GameEvents.OnGameStarted += HandleOnGameStarted;
            GameEvents.OnEggLaunched += HandleOnEggLaunched;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= HandleOnGameStarted;
            GameEvents.OnEggLaunched -= HandleOnEggLaunched;
        }

        private void HandleOnGameStarted()
        {
            isResolving = false;
            stopTimer = 0f;
            resolveTimer = 0f;
            ResetEggDamping();
        }

        private void HandleOnEggLaunched(EggController egg)
        {
            isResolving = true;
            stopTimer = 0f;
            resolveTimer = 0f;
            Debug.Log("[MotionResolver] Resolving started.");
        }

        private void Update()
        {
            if (!isResolving) return;

            resolveTimer += Time.deltaTime;

            // 느린 알에 동적 댐핑 적용 (구덩이/계곡 진동 빠르게 감쇠)
            ApplyDynamicDamping();

            // [6-3] 강제 턴 종료: 최대 해석 시간 초과 시 모든 알 강제 정지
            if (resolveTimer >= settings.maxResolveTime)
            {
                Debug.Log($"[MotionResolver] Max resolve time ({settings.maxResolveTime}s) reached. Forcing stop.");
                ForceStopAllEggs();
                return;
            }

            bool allStopped = CheckAllEggsStopped();

            if (allStopped)
            {
                stopTimer += Time.deltaTime;
                if (stopTimer >= settings.stopHoldTime)
                {
                    Debug.Log("[MotionResolver] All eggs stopped. Ending resolve.");
                    isResolving = false;
                    ResetEggDamping();
                    GameEvents.TriggerAllEggsStopped();
                }
            }
            else
            {
                stopTimer = 0f;
            }
        }

        /// <summary>
        /// 느리게 움직이는 알의 댐핑을 높여 진동을 빠르게 감쇠시킨다.
        /// 빠른 알(발사 직후)은 정상 물리 유지.
        /// </summary>
        private void ApplyDynamicDamping()
        {
            for (int i = trackedEggs.Count - 1; i >= 0; i--)
            {
                if (trackedEggs[i] == null)
                {
                    trackedEggs.RemoveAt(i);
                    continue;
                }
                if (!trackedEggs[i].IsAlive) continue;

                float speed = trackedEggs[i].Rigidbody.linearVelocity.magnitude;
                if (speed < settings.dampingSpeedThreshold)
                {
                    float t = Mathf.Clamp01(speed / settings.dampingSpeedThreshold);
                    float damping = Mathf.Lerp(settings.highDamping, 0f, t);
                    trackedEggs[i].Rigidbody.linearDamping = damping;
                    trackedEggs[i].Rigidbody.angularDamping = damping * 0.5f;
                }
                else
                {
                    trackedEggs[i].Rigidbody.linearDamping = 0f;
                    trackedEggs[i].Rigidbody.angularDamping = 0f;
                }
            }
        }

        /// <summary>
        /// 모든 알의 댐핑을 기본값(0)으로 복원.
        /// </summary>
        private void ResetEggDamping()
        {
            for (int i = trackedEggs.Count - 1; i >= 0; i--)
            {
                if (trackedEggs[i] == null)
                {
                    trackedEggs.RemoveAt(i);
                    continue;
                }
                if (!trackedEggs[i].IsAlive) continue;
                trackedEggs[i].Rigidbody.linearDamping = 0f;
                trackedEggs[i].Rigidbody.angularDamping = 0f;
            }
        }

        /// <summary>
        /// [6-2] 정지 판정: 모든 생존 알의 속도가 임계값 이하인지 확인한다.
        /// </summary>
        private bool CheckAllEggsStopped()
        {
            for (int i = trackedEggs.Count - 1; i >= 0; i--)
            {
                if (trackedEggs[i] == null)
                {
                    trackedEggs.RemoveAt(i);
                    continue;
                }
                if (!trackedEggs[i].IsAlive) continue;
                if (trackedEggs[i].Rigidbody.linearVelocity.magnitude > settings.stopVelocity)
                    return false;
            }
            return true;
        }

        private void ForceStopAllEggs()
        {
            for (int i = trackedEggs.Count - 1; i >= 0; i--)
            {
                if (trackedEggs[i] == null)
                {
                    trackedEggs.RemoveAt(i);
                    continue;
                }
                if (!trackedEggs[i].IsAlive) continue;
                trackedEggs[i].Rigidbody.linearVelocity = Vector3.zero;
                trackedEggs[i].Rigidbody.angularVelocity = Vector3.zero;
            }
            isResolving = false;
            GameEvents.TriggerAllEggsStopped();
        }

        public void RegisterEgg(EggController egg)
        {
            if (egg != null && !trackedEggs.Contains(egg))
                trackedEggs.Add(egg);
        }

        public void UnregisterEgg(EggController egg)
        {
            trackedEggs.Remove(egg);
        }

        public void ClearEggs()
        {
            trackedEggs.Clear();
        }
    }
}
