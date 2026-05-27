using UnityEngine;
using System.Collections.Generic;
using DinoAlkkagi.Core;
using DinoAlkkagi.Data;

namespace DinoAlkkagi.Rules
{
    /// <summary>
    /// Person B 전용 — 모든 알의 정지 상태를 감시하고 턴 종료를 판정한다.
    /// Resolving 중 FixedDeltaTime을 줄여 물리가 더 자주 시뮬레이션되도록 한다.
    /// Time.timeScale은 1.0으로 유지되어 UI/오디오/애니메이션에 영향을 주지 않는다.
    /// 기획서 6-2 정지 판정 및 강제 턴 종료 알고리즘(6-2, 6-3)을 구현한다.
    /// </summary>
    public class MotionResolver : MonoBehaviour
    {
        [SerializeField] private GameSettings settings;

        private List<EggController> trackedEggs = new List<EggController>();
        private float stopTimer = 0f;
        private float resolveTimer = 0f;
        private bool isResolving = false;

        private float originalFixedDeltaTime;
        private float originalMaxDeltaTime;

        private bool isClientOnly => GameLaunchContext.IsNetworkClient;

        public bool IsResolving => isResolving;
        public float ResolveTime => resolveTimer;

        private void Awake()
        {
            originalFixedDeltaTime = Time.fixedDeltaTime;
            originalMaxDeltaTime = Time.maximumDeltaTime;
        }

        private void OnEnable()
        {
            GameEvents.OnGameStarted += HandleOnGameStarted;
            GameEvents.OnEggLaunched += HandleOnEggLaunched;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= HandleOnGameStarted;
            GameEvents.OnEggLaunched -= HandleOnEggLaunched;

            // 안전장치: 원래 값으로 복원
            Time.fixedDeltaTime = originalFixedDeltaTime;
            Time.maximumDeltaTime = originalMaxDeltaTime;
        }

        private void HandleOnGameStarted()
        {
            if (isClientOnly) return;
            isResolving = false;
            stopTimer = 0f;
            resolveTimer = 0f;

            // 물리 파라미터 초기화 (재시작 대응)
            Time.fixedDeltaTime = originalFixedDeltaTime;
            Time.maximumDeltaTime = originalMaxDeltaTime;
            Time.timeScale = 1f;
        }

        private void HandleOnEggLaunched(EggController egg)
        {
            if (isClientOnly) return;
            isResolving = true;
            stopTimer = 0f;
            resolveTimer = 0f;

            // fixedDeltaTime을 줄이면: 같은 시간에 더 많은 FixedUpdate 실행 → 물리 2.5배 빠르게
            // maximumDeltaTime을 늘리면: 프레임 드롭 시 따라잡기 제한 완화
            // Time.timeScale은 1.0 유지 → UI/오디오/애니메이션 정속
            Time.fixedDeltaTime = originalFixedDeltaTime / settings.resolveTimeScale;
            Time.maximumDeltaTime = originalMaxDeltaTime * settings.resolveTimeScale;
            Debug.Log($"[MotionResolver] Resolving started. FixedDeltaTime: {Time.fixedDeltaTime:F4}s (x{settings.resolveTimeScale})");
        }

        private void Update()
        {
            if (isClientOnly) return;
            if (!isResolving) return;

            // resolveTimer: 게임-시간 기준으로 측정 (실제로는 resolveTimeScale 배 빠르게 흐름)
            resolveTimer += Time.unscaledDeltaTime * settings.resolveTimeScale;

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
                // stopTimer: 실제 시간 기준 (언스케일드)
                stopTimer += Time.unscaledDeltaTime;
                if (stopTimer >= settings.stopHoldTime)
                {
                    Debug.Log("[MotionResolver] All eggs stopped. Ending resolve.");
                    EndResolve();
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
                trackedEggs[i].StopImmediately();
            }

            EndResolve();
            GameEvents.TriggerAllEggsStopped();
        }

        private void EndResolve()
        {
            isResolving = false;
            Time.fixedDeltaTime = originalFixedDeltaTime;
            Time.maximumDeltaTime = originalMaxDeltaTime;
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
