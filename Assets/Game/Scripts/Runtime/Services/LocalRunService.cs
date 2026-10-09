using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Data;
using Wheel.Rules;

namespace Game.Runtime.Services
{
    /// <summary>Runs the shared rules in-process. Same GameSession the server would run; only the transport differs.</summary>
    public sealed class LocalRunService : IRunService
    {
        readonly GameSession session;
        readonly Dictionary<WheelTier, WheelDefinition> wheels = new();

        public LocalRunService(GameConfig config, IRandom random)
        {
            session = new GameSession(config.Rules, random);
            foreach (var tier in (WheelTier[])Enum.GetValues(typeof(WheelTier)))
                wheels[tier] = config.Progression.GetWheel(tier).ToDefinition();
        }

        public UniTask<RunSnapshot> StartAsync(CancellationToken cancellationToken) => UniTask.FromResult(session.Snapshot());

        public UniTask<SpinOutcome> SpinAsync(CancellationToken cancellationToken) =>
            UniTask.FromResult(session.Spin(wheels[session.CurrentTier]));

        public UniTask<RunSnapshot> LeaveAsync(CancellationToken cancellationToken) => Apply(session.TryLeave(), "leave");
        public UniTask<RunSnapshot> ReviveAsync(CancellationToken cancellationToken) => Apply(session.TryRevive(), "revive");
        public UniTask<RunSnapshot> RestartAsync(CancellationToken cancellationToken) => Apply(session.TryRestart(), "restart");

        UniTask<RunSnapshot> Apply(bool accepted, string action)
        {
            if (!accepted) throw new InvalidOperationException($"'{action}' is not allowed while {session.State} at zone {session.Zone}");
            return UniTask.FromResult(session.Snapshot());
        }
    }
}
