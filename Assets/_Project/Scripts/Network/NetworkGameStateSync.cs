using UnityEngine;
using System.Collections.Generic;
using Mirror;
using DinoAlkkagi.Core;
using DinoAlkkagi.Rules;
using DinoAlkkagi.Data;
using DinoAlkkagi.Presentation;

public class NetworkGameStateSync : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private float snapshotInterval = 0.05f;

    private GameSessionController session;
    private TurnController turnController;
    private float snapshotTimer;
    private bool isResolving;
    private bool isServerActive;

    private void Awake()
    {
        settings ??= FindFirstObjectByType<GameSettings>();
    }

    private void OnEnable()
    {
        GameEvents.OnEggLaunched += HandleEggLaunched;
        GameEvents.OnAllEggsStopped += HandleAllEggsStopped;
        GameEvents.OnGameEnded += HandleOnGameEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnEggLaunched -= HandleEggLaunched;
        GameEvents.OnAllEggsStopped -= HandleAllEggsStopped;
        GameEvents.OnGameEnded -= HandleOnGameEnded;
    }

    private void Start()
    {
        session = FindFirstObjectByType<GameSessionController>();
        turnController = FindFirstObjectByType<TurnController>();
        isServerActive = GameLaunchContext.IsNetworkHost || !GameLaunchContext.IsNetwork;
    }

    private void HandleEggLaunched(EggController egg)
    {
        if (!isServerActive) return;
        isResolving = true;
        snapshotTimer = 0f;
    }

    private void HandleAllEggsStopped()
    {
        if (!isServerActive) return;
        isResolving = false;
        if (NetworkServer.active)
        {
            SendSnapshot();
            if (turnController != null)
            {
                TurnChangeMessage turnMsg = new TurnChangeMessage { playerId = turnController.CurrentPlayerId };
                NetworkServer.SendToAll(turnMsg);
            }
        }
    }

    private void HandleOnGameEnded(GameResult result)
    {
        if (!NetworkServer.active || !isServerActive) return;

        int resultValue;
        switch (result)
        {
            case GameResult.Player1Win: resultValue = 1; break;
            case GameResult.Player2Win: resultValue = 2; break;
            case GameResult.Draw:       resultValue = 3; break;
            default:                    resultValue = 0; break;
        }

        if (resultValue > 0)
        {
            GameResultMessage resultMsg = new GameResultMessage { result = resultValue };
            NetworkServer.SendToAll(resultMsg);
        }
    }

    private void Update()
    {
        if (!NetworkServer.active || !isServerActive || !isResolving) return;

        snapshotTimer += Time.deltaTime;
        if (snapshotTimer >= snapshotInterval)
        {
            snapshotTimer = 0f;
            SendSnapshot();
        }
    }

    private void SendSnapshot()
    {
        if (session == null) session = FindFirstObjectByType<GameSessionController>();
        if (session == null) return;

        var eggs = session.AllEggs;
        if (eggs == null || eggs.Count == 0) return;

        int p1Alive = 0;
        int p2Alive = 0;
        List<EggState> states = new List<EggState>(eggs.Count);

        foreach (var egg in eggs)
        {
            if (egg == null) continue;

            EggState state = new EggState
            {
                netId = (uint)egg.NetworkEggId,
                position = egg.transform.position,
                rotation = egg.transform.rotation,
                velocity = egg.Rigidbody != null ? egg.Rigidbody.linearVelocity : Vector3.zero,
                isAlive = egg.IsAlive,
                ownerPlayerId = egg.OwnerPlayerId
            };
            states.Add(state);

            if (egg.IsAlive)
            {
                if (egg.OwnerPlayerId == 1) p1Alive++;
                else if (egg.OwnerPlayerId == 2) p2Alive++;
            }
        }

        TurnController turnCtrl = turnController ?? FindFirstObjectByType<TurnController>();
        GameSessionUiBridge uiBridge = FindFirstObjectByType<GameSessionUiBridge>();
        float gameTime = 0f;
        float turnTime = 0f;
        if (uiBridge != null)
        {
            gameTime = uiBridge.GetGameElapsedTime();
            turnTime = uiBridge.GetTurnElapsedTime();
        }

        StateSnapshotMessage msg = new StateSnapshotMessage
        {
            currentPlayerId = turnCtrl != null ? turnCtrl.CurrentPlayerId : 1,
            p1AliveCount = p1Alive,
            p2AliveCount = p2Alive,
            gameElapsedTime = gameTime,
            turnElapsedTime = turnTime,
            eggStates = states.ToArray()
        };

        NetworkServer.SendToAll(msg);
    }

    public void ApplySnapshot(StateSnapshotMessage msg)
    {
        session ??= FindFirstObjectByType<GameSessionController>();
        if (session == null) return;

        // 서버 타이머 동기화
        GameSessionUiBridge uiBridge = FindFirstObjectByType<GameSessionUiBridge>();
        if (uiBridge != null)
        {
            uiBridge.ApplyServerTime(msg.gameElapsedTime, msg.turnElapsedTime);
        }

        bool isClientOnly = GameLaunchContext.IsNetworkClient;

        foreach (var eggState in msg.eggStates)
        {
            EggController egg = FindEggByEggId((int)eggState.netId, session.AllEggs);
            if (egg == null) continue;

            egg.transform.position = eggState.position;
            egg.transform.rotation = eggState.rotation;

            if (egg.Rigidbody != null)
            {
                egg.Rigidbody.linearVelocity = eggState.velocity;

                // 클라이언트는 물리 직접 실행 안 함 — kinematic 상태 유지
                // 서버(호스트)에서만 물리 상태를 스냅샷과 동기화
                if (!isClientOnly)
                {
                    egg.Rigidbody.isKinematic = !eggState.isAlive;
                    egg.Rigidbody.useGravity = eggState.isAlive;
                }
            }

            if (!eggState.isAlive && egg.IsAlive)
            {
                egg.MarkFallen();
            }

            egg.gameObject.SetActive(eggState.isAlive);
        }
    }

    private EggController FindEggByEggId(int eggId, System.Collections.Generic.IReadOnlyList<EggController> eggs)
    {
        if (eggs == null) return null;
        foreach (var egg in eggs)
        {
            if (egg == null) continue;
            if (egg.NetworkEggId == eggId)
                return egg;
        }
        return null;
    }
}
