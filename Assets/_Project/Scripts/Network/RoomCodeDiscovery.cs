using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace DinoAlkkagi.Network
{
    /// <summary>
    /// UDP 브로드캐스트 기반 방 코드 발견 시스템.
    /// 방장은 4자리 코드를 생성해 표시하고, 참가자는 코드로 방을 검색한다.
    /// 포트 7778을 사용 (KcpTransport 7777과 충돌 방지).
    /// </summary>
    public class RoomCodeDiscovery : MonoBehaviour
    {
        [SerializeField] private int broadcastPort = 7778;
        [SerializeField] private float listenTimeout = 10f;

        private string roomCode;
        private Thread listenThread;
        private UdpClient udpClient;
        private volatile string receivedHostIp;
        private volatile bool foundHost;
        private float listenTimer;
        private bool isListening;
        private bool discoveryComplete;

        public event Action<string, string> OnRoomFound; // (ip, roomCode)
        public event Action<string> OnListenStarted;     // (roomCode)
        public event Action<string> OnListenError;       // (errorMessage)

        public string RoomCode => roomCode;

        private void OnDestroy()
        {
            StopListening();
        }

        private void Update()
        {
            if (isListening && !discoveryComplete)
            {
                listenTimer += Time.unscaledDeltaTime;
                if (listenTimer >= listenTimeout)
                {
                    StopListening();
                    Debug.LogWarning("[RoomCodeDiscovery] Discovery timeout. No host found.");
                }
            }

            if (foundHost && !discoveryComplete)
            {
                discoveryComplete = true;
                StopListening();
                string ip = receivedHostIp;
                string code = roomCode;
                receivedHostIp = null;
                OnRoomFound?.Invoke(ip, code);
            }
        }

        /// <summary>
        /// 방장: 랜덤 4자리 코드 생성 + 브로드캐스트 리스너 시작
        /// </summary>
        public string StartHostBroadcast()
        {
            roomCode = GenerateRoomCode();
            Debug.Log($"[RoomCodeDiscovery] Host room code: {roomCode}");
            StartListening(respondToLookup: true);
            OnListenStarted?.Invoke(roomCode);
            return roomCode;
        }

        /// <summary>
        /// 참가자: 코드로 방 검색 시작
        /// </summary>
        public void StartClientLookup(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length != 4)
            {
                Debug.LogError("[RoomCodeDiscovery] Invalid room code. Must be 4 digits.");
                return;
            }

            roomCode = code;
            foundHost = false;
            discoveryComplete = false;
            listenTimer = 0f;

            StartListening(respondToLookup: false);
            SendBroadcastLookup(code);
        }

        /// <summary>
        /// UDP 리스너 시작
        /// </summary>
        private void StartListening(bool respondToLookup)
        {
            StopListening();

            try
            {
                udpClient = new UdpClient();
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort));

                isListening = true;

                listenThread = new Thread(() =>
                {
                    try
                    {
                        while (isListening)
                        {
                            // Receive with timeout: 1초마다 isListening 체크 가능
                            udpClient.Client.ReceiveTimeout = 1000;
                            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                            byte[] data = udpClient.Receive(ref remoteEndPoint);
                            string message = Encoding.UTF8.GetString(data);
                            ProcessMessage(message, remoteEndPoint, respondToLookup);
                        }
                    }
                    catch (ThreadAbortException) { }
                    catch (ObjectDisposedException) { }
                    catch (SocketException) { /* timeout or closed socket */ }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[RoomCodeDiscovery] Listener error: {ex.Message}");
                    }
                })
                {
                    IsBackground = true
                };
                listenThread.Start();

                Debug.Log($"[RoomCodeDiscovery] Listening on port {broadcastPort}");
            }
            catch (Exception ex)
            {
                isListening = false;
                string errorMsg = $"Failed to start listener: {ex.Message}";
                Debug.LogError($"[RoomCodeDiscovery] {errorMsg}");
                OnListenError?.Invoke(errorMsg);
            }
        }

        private void StopListening()
        {
            isListening = false;
            try
            {
                if (udpClient != null)
                {
                    udpClient.Close();
                    udpClient = null;
                }
            }
            catch { }

            // Thread.Abort() 사용 안 함 — udpClient.Close()가 Receive()를 중단시켜서
            // ObjectDisposedException이 발생하고 while(isListening)이 종료됨
            listenThread = null;
        }

        private void ProcessMessage(string message, IPEndPoint sender, bool amHost)
        {
            message = message.Trim();

            if (amHost && message.StartsWith("LOOKUP:"))
            {
                // 누군가 방을 찾고 있음 → 코드 확인 후 응답
                string lookupCode = message.Substring(7).Trim();
                if (lookupCode == roomCode)
                {
                    string response = $"HERE:{roomCode}";
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    udpClient.Send(responseData, responseData.Length, sender);
                    Debug.Log($"[RoomCodeDiscovery] Responded to lookup from {sender.Address}");
                }
            }
            else if (!amHost && message.StartsWith("HERE:"))
            {
                // 방장이 응답함 → IP 저장
                string parts = message.Substring(5).Trim();
                if (parts == roomCode)
                {
                    receivedHostIp = sender.Address.ToString();
                    foundHost = true;
                    Debug.Log($"[RoomCodeDiscovery] Found host at {receivedHostIp}");
                }
            }
        }

        private void SendBroadcastLookup(string code)
        {
            try
            {
                using (UdpClient broadcastClient = new UdpClient())
                {
                    broadcastClient.EnableBroadcast = true;
                    IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
                    string message = $"LOOKUP:{code}";
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    broadcastClient.Send(data, data.Length, broadcastEndPoint);
                    Debug.Log($"[RoomCodeDiscovery] Sent lookup for code {code}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomCodeDiscovery] Broadcast send failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 1000~9999 범위의 랜덤 4자리 코드 생성
        /// </summary>
        private static string GenerateRoomCode()
        {
            return UnityEngine.Random.Range(1000, 10000).ToString();
        }

        public void Cancel()
        {
            StopListening();
            foundHost = false;
            discoveryComplete = false;
            roomCode = null;
        }
    }
}
