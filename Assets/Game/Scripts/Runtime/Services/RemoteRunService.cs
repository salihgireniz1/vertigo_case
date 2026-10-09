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
        int sequence;

        public RemoteRunService(string baseUrl, string playerToken)
        {
            this.baseUrl = baseUrl.TrimEnd('/');
            this.playerToken = playerToken;
        }

        public UniTask<RunSnapshot> StartAsync(CancellationToken cancellationToken) => Post<RunSnapshot>("run/start", cancellationToken);
        public UniTask<SpinOutcome> SpinAsync(CancellationToken cancellationToken) => Post<SpinOutcome>("run/spin", cancellationToken);
        public UniTask<RunSnapshot> LeaveAsync(CancellationToken cancellationToken) => Post<RunSnapshot>("run/leave", cancellationToken);
        public UniTask<RunSnapshot> ReviveAsync(CancellationToken cancellationToken) => Post<RunSnapshot>("run/revive", cancellationToken);
        public UniTask<RunSnapshot> RestartAsync(CancellationToken cancellationToken) => Post<RunSnapshot>("run/restart", cancellationToken);

        [Serializable]
        sealed class IntentRequest
        {
            /// <summary>Idempotency key: the server answers a retried sequence number with the stored result instead of re-rolling.</summary>
            public int Sequence;
        }

        async UniTask<T> Post<T>(string path, CancellationToken cancellationToken)
        {
            var body = JsonUtility.ToJson(new IntentRequest { Sequence = ++sequence });
            using var request = new UnityWebRequest($"{baseUrl}/{path}", UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {playerToken}");
            await request.SendWebRequest().WithCancellation(cancellationToken);
            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException($"{path} failed: {request.responseCode} {request.error}");
            return JsonUtility.FromJson<T>(request.downloadHandler.text);
        }
    }
}
