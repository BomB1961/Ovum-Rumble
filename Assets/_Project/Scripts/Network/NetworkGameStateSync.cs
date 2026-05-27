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
    private float bufferDelay = 0.1f;
    private float maxExtrapolationTime = 0.25f;

    private GameSessionController session;
    private TurnController turnController;
    private GameSessionUiBridge uiBridge;
    private float snapshotTimer;
    private bool isResolving;
    private bool isServerActive;
    private bool gameEndSent;

    // 클라이언트 스냅샷 버퍼
    private struct TimestampedSnapshot
    {
        public float timestamp;
        public EggState[] states;
    }
    private Queue<TimestampedSnapshot> snapshotBuffer = new Queue<TimestampedSnapshot>();
    private float clientTime;

    private void Awake()
    {
        settings ??= Resources.Load<GameSettings>("GameSettings_Default");
        if (settings == null)
            settings = FindFirstObjectByType<GameSettings>();
    }

    private void OnEnable()
    {
        GameEvents.OnEggLaunched += HandleEggLaunched;
        GameEvents.OnAllEggsStopped += HandleAllEggsStopped;
        GameEvents.OnGameEnded += HandleOnGameEnded;
        GameEvents.OnGameStarted += HandleOnGameStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnEggLaunched -= HandleEggLaunched;
        GameEvents.OnAllEggsStopped -= HandleAllEggsStopped;
        GameEvents.OnGameEnded -= HandleOnGameEnded;
        GameEvents.OnGameStarted -= HandleOnGameStarted;
    }

    private void Start()
    {
        session = FindFirstObjectByType<GameSessionController>();
        turnController = FindFirstObjectByType<TurnController>();
        uiBridge = FindFirstObjectByType<GameSessionUiBridge>();
        isServerActive = GameLaunchContext.IsNetworkHost || !GameLaunchContext.IsNetwork;
    }

    private void HandleOnGameStarted()
    {
        gameEndSent = false;
        snapshotBuffer.Clear();
        clientTime = 0f;
        isResolving = false;
        snapshotTimer = 0f;
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
            if (gameEndSent) return;
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
        if (gameEndSent) return;

        gameEndSent = true;

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
            SendSnapshot();
            GameResultMessage resultMsg = new GameResultMessage { result = resultValue };
            NetworkServer.SendToAll(resultMsg);
        }
    }

    private void Update()
    {
        if (NetworkServer.active && isServerActive)
        {
            snapshotTimer += Time.deltaTime;
            if (snapshotTimer >= snapshotInterval)
            {
                snapshotTimer = 0f;
                SendSnapshot();
            }
        }

        if (GameLaunchContext.IsNetworkClient && !NetworkServer.active)
        {
            clientTime += Time.deltaTime;
            InterpolateFromBuffer();
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
        GameSessionUiBridge bridge = uiBridge ?? FindFirstObjectByType<GameSessionUiBridge>();
        float gameTime = 0f;
        float turnTime = 0f;
        if (bridge != null)
        {
            gameTime = bridge.GetGameElapsedTime();
            turnTime = bridge.GetTurnElapsedTime();
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
        if (NetworkServer.active) return;

        session ??= FindFirstObjectByType<GameSessionController>();
        if (session == null) return;

        GameSessionUiBridge bridge = uiBridge ?? FindFirstObjectByType<GameSessionUiBridge>();
        if (bridge != null)
        {
            bridge.ApplyServerTime(msg.gameElapsedTime, msg.turnElapsedTime);
        }

        snapshotBuffer.Enqueue(new TimestampedSnapshot
        {
            timestamp = clientTime,
            states = msg.eggStates
        });

        while (snapshotBuffer.Count > 30)
        {
            snapshotBuffer.Dequeue();
        }
    }

    private void InterpolateFromBuffer()
    {
        if (session == null) return;
        if (snapshotBuffer.Count < 2) return;

        float renderTime = clientTime - bufferDelay;

        TimestampedSnapshot[] buf = snapshotBuffer.ToArray();
        TimestampedSnapshot before = default;
        TimestampedSnapshot after = default;
        bool found = false;

        for (int i = 0; i < buf.Length - 1; i++)
        {
            if (buf[i].timestamp <= renderTime && buf[i + 1].timestamp >= renderTime)
            {
                before = buf[i];
                after = buf[i + 1];
                found = true;
                break;
            }
        }

        if (!found)
        {
            if (renderTime > buf[buf.Length - 1].timestamp)
            {
                before = buf[buf.Length - 2];
                after = buf[buf.Length - 1];
                float extraTime = renderTime - after.timestamp;
                if (extraTime > maxExtrapolationTime)
                {
                    ApplyStatesDirectly(after.states);
                    return;
                }
            }
            else
            {
                return;
            }
        }

        while (snapshotBuffer.Count > 2 && snapshotBuffer.Peek().timestamp < before.timestamp)
        {
            snapshotBuffer.Dequeue();
        }

        float duration = after.timestamp - before.timestamp;
        float t = duration > 0.001f ? (renderTime - before.timestamp) / duration : 1f;
        t = Mathf.Clamp01(t);

        foreach (var afterState in after.states)
        {
            EggController egg = FindEggByEggId((int)afterState.netId, session.AllEggs);
            if (egg == null) continue;

            EggState beforeState = default;
            bool hasBefore = false;
            foreach (var bs in before.states)
            {
                if (bs.netId == afterState.netId)
                {
                    beforeState = bs;
                    hasBefore = true;
                    break;
                }
            }

            if (hasBefore)
            {
                egg.transform.position = Vector3.Lerp(beforeState.position, afterState.position, t);
                egg.transform.rotation = Quaternion.Slerp(beforeState.rotation, afterState.rotation, t);
            }
            else
            {
                egg.transform.position = afterState.position;
                egg.transform.rotation = afterState.rotation;
            }

            if (!afterState.isAlive && egg.IsAlive)
            {
                egg.MarkFallen();
            }
            egg.gameObject.SetActive(afterState.isAlive);
        }
    }

    private void ApplyStatesDirectly(EggState[] states)
    {
        if (session == null) return;
        foreach (var state in states)
        {
            EggController egg = FindEggByEggId((int)state.netId, session.AllEggs);
            if (egg == null) continue;
            egg.transform.position = state.position;
            egg.transform.rotation = state.rotation;
            if (!state.isAlive && egg.IsAlive)
                egg.MarkFallen();
            egg.gameObject.SetActive(state.isAlive);
        }
    }

    private EggController FindEggByEggId(int eggId, IReadOnlyList<EggController> eggs)
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
