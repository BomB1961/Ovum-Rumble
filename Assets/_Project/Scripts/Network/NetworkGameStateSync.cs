using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using DinoAlkkagi.Core;
using DinoAlkkagi.Rules;
using DinoAlkkagi.Data;

public class NetworkGameStateSync : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private float snapshotInterval = 0.1f;

    private GameSessionController session;
    private WinConditionChecker winChecker;
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
    }

    private void OnDisable()
    {
        GameEvents.OnEggLaunched -= HandleEggLaunched;
        GameEvents.OnAllEggsStopped -= HandleAllEggsStopped;
    }

    private void Start()
    {
        session = FindFirstObjectByType<GameSessionController>();
        winChecker = FindFirstObjectByType<WinConditionChecker>();
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
            if (winChecker != null && winChecker.GameEnded)
            {
                int resultValue = 0;
                var eggs = session != null ? session.AllEggs : null;
                if (eggs != null)
                {
                    int p1Alive = eggs.Count(e => e != null && e.OwnerPlayerId == 1 && e.IsAlive);
                    int p2Alive = eggs.Count(e => e != null && e.OwnerPlayerId == 2 && e.IsAlive);
                    if (p1Alive == 0 && p2Alive > 0) resultValue = 2;
                    else if (p2Alive == 0 && p1Alive > 0) resultValue = 1;
                    else if (p1Alive == 0 && p2Alive == 0) resultValue = 3;
                }
                if (resultValue > 0)
                {
                    GameResultMessage resultMsg = new GameResultMessage { result = resultValue };
                    NetworkServer.SendToAll(resultMsg);
                }
            }
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
        StateSnapshotMessage msg = new StateSnapshotMessage
        {
            currentPlayerId = turnCtrl != null ? turnCtrl.CurrentPlayerId : 1,
            p1AliveCount = p1Alive,
            p2AliveCount = p2Alive,
            eggStates = states.ToArray()
        };

        NetworkServer.SendToAll(msg);
    }

    public void ApplySnapshot(StateSnapshotMessage msg)
    {
        session ??= FindFirstObjectByType<GameSessionController>();
        if (session == null) return;

        foreach (var eggState in msg.eggStates)
        {
            EggController egg = FindEggByEggId((int)eggState.netId, session.AllEggs);
            if (egg == null) continue;

            egg.transform.position = eggState.position;
            egg.transform.rotation = eggState.rotation;

            if (egg.Rigidbody != null)
            {
                egg.Rigidbody.linearVelocity = eggState.velocity;
                egg.Rigidbody.isKinematic = !eggState.isAlive;
                egg.Rigidbody.useGravity = eggState.isAlive;
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
