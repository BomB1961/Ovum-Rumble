using UnityEngine;
using System.Collections.Generic;
using DinoAlkkagi.Core;
using DinoAlkkagi.Data;

namespace DinoAlkkagi.Rules
{
    /// <summary>
    /// Person B 전용 — 모든 알의 정지 상태를 감시하고 턴 종료를 판정한다.
    /// Resolving 중 Time.timeScale을 높여 물리를 빠르게 시뮬레이션하고
    /// 알이 자연스럽게 멈출 때까지 기다린다.
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
            Time.timeScale = 1f; // 안전장치
        }

        private void HandleOnGameStarted()
        {
            isResolving = false;
            stopTimer = 0f;
            resolveTimer = 0f;
            Time.timeScale = 1f;
        }

        private void HandleOnEggLaunched(EggController egg)
        {
            isResolving = true;
            stopTimer = 0f;
            resolveTimer = 0f;
            Time.timeScale = settings.resolveTimeScale;
            Debug.Log($"[MotionResolver] Resolving started. Time scale: {settings.resolveTimeScale}x");
        }

        private void Update()
        {
            if (!isResolving) return;

            resolveTimer += Time.deltaTime;

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
                    Time.timeScale = 1f;
                    GameEvents.TriggerAllEggsStopped();
                }
            }
            else
            {
                stopTimer = 0f;
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
            Time.timeScale = 1f;
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
