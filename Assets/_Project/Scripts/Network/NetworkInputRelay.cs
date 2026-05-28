using UnityEngine;
using Mirror;

public class NetworkInputRelay : MonoBehaviour
{
    private static NetworkInputRelay instance;

    public static NetworkInputRelay Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<NetworkInputRelay>();
            return instance;
        }
    }

    private void Awake()
    {
        instance = this;
    }

    public void SendLaunchInput(uint eggNetId, Vector3 direction, float force)
    {
        if (!NetworkClient.active)
        {
            Debug.LogWarning("[NetworkInputRelay] Cannot send launch input: not connected to server.");
            return;
        }

        LaunchInputMessage msg = new LaunchInputMessage
        {
            eggNetId = eggNetId,
            direction = direction,
            force = force
        };
        NetworkClient.Send(msg);
        Debug.Log($"[NetworkInputRelay] Sent LaunchInput: egg={eggNetId}, dir={direction}, force={force}");
    }

    public void SendRestartRequest(bool ready)
    {
        if (!NetworkClient.active)
        {
            Debug.LogWarning("[NetworkInputRelay] Cannot send restart request: not connected to server.");
            return;
        }

        RestartRequestMessage msg = new RestartRequestMessage { playerReady = ready };
        NetworkClient.Send(msg);
    }
}
