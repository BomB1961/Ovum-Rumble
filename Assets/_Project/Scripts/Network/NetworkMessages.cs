using Mirror;
using UnityEngine;

public struct JoinGameMessage : NetworkMessage
{
    public string playerName;
}

public struct JoinAcceptedMessage : NetworkMessage
{
    public int assignedPlayerId;
}

public struct LaunchInputMessage : NetworkMessage
{
    public uint eggNetId;
    public Vector3 direction;
    public float force;
}

public struct EggState
{
    public uint netId;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public bool isAlive;
    public int ownerPlayerId;
}

public struct StateSnapshotMessage : NetworkMessage
{
    public int currentPlayerId;
    public int p1AliveCount;
    public int p2AliveCount;
    public EggState[] eggStates;
}

public struct TurnChangeMessage : NetworkMessage
{
    public int playerId;
}

public struct GameResultMessage : NetworkMessage
{
    public int result; // 0=None, 1=Player1Win, 2=Player2Win, 3=Draw
}

public struct RestartRequestMessage : NetworkMessage
{
    public bool playerReady;
}

public struct RestartConfirmedMessage : NetworkMessage
{
    public bool restartApproved;
}

public struct MapSelectMessage : NetworkMessage
{
    public int mapId;
}

public struct LoadSceneMessage : NetworkMessage
{
    public string sceneName;
}
