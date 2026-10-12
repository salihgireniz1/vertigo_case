using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Wheel.Rules;

namespace Game.Runtime.Services
{
    /// <summary>
    /// Server-authoritative transport. The server runs the same Wheel.Rules GameSession and answers with the DTOs
    /// from that library, so the client stays a renderer. Not wired in the demo: no backend is deployed.
    /// </summary>
    public sealed class RemoteRunService : IRunService
    {
        readonly string baseUrl;
        readonly string playerToken;
        readonly int timeoutSeconds;
        int sequence;
        /// <summary>The intent whose answer has not arrived yet. A retry resends it with the same sequence number.</summary>
        string pendingPath;

        public RemoteRunService(string baseUrl, string playerToken, int timeoutSeconds = 10)
        {
            this.baseUrl = baseUrl.TrimEnd('/');
            this.playerToken = playerToken;
            this.timeoutSeconds = timeoutSeconds;
        }

        public UniTask<RunSnapshot> StartAsync(CancellationToken cancellationToken) => Post<RunSnapshot>("run/start", cancellationToken);
        public UniTask<SpinOutcome> SpinAsync(CancellationToken cancellationToken) => Post<SpinOutcome>("run/spin", cancellationToken);
        public UniTask<RunSnapshot> LeaveAsync(CancellationToken cancellationToken) => Post<RunSnapshot>("run/leave", cancellationToken);
        public UniTask<RunSnapshot> ReviveAsync(CancellationToken cancellationToken) => Post<RunSnapshot>("run/revive", cancellationToken);
        public UniTask<RunSnapshot> RestartAsync(CancellationToken cancellationToken) => Post<RunSnapshot>("run/restart", cancellationToken);

        [Serializable]
        sealed class IntentRequest
        {
            /// <summary>Idempotency key: the server answers a repeated sequence number with the stored result instead of re-rolling.</summary>
            public int Sequence;
        }

        async UniTask<T> Post<T>(string path, CancellationToken cancellationToken)
        {
            if (pendingPath != null && pendingPath != path)
                throw new InvalidOperationException($"'{pendingPath}' is still unanswered; it must be resent before '{path}'");
            if (pendingPath == null)
            {
                sequence++;
                pendingPath = path;
            }

            var body = JsonUtility.ToJson(new IntentRequest { Sequence = sequence });
            using var request = new UnityWebRequest($"{baseUrl}/{path}", UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = timeoutSeconds;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {playerToken}");
            try
            {
                await request.SendWebRequest().WithCancellation(cancellationToken);
            }
            catch (UnityWebRequestException e) when (e.ResponseCode >= 400 && e.ResponseCode < 500)
            {
                pendingPath = null; // the server refused the intent; resending the same thing would not help
                throw new InvalidOperationException($"{path} refused: {e.ResponseCode} {e.Error}", e);
            }
            catch (UnityWebRequestException e)
            {
                throw new RunServiceUnavailableException($"{path}: {e.Error}", e); // no answer or 5xx: keep the sequence, let the player retry
            }

            pendingPath = null;
            return JsonUtility.FromJson<T>(request.downloadHandler.text);
        }
    }
}
