using System.Threading;
using Cysharp.Threading.Tasks;
using Wheel.Rules;

namespace Game.Runtime.Services
{
    /// <summary>
    /// The client's only way to change the run. The implementation decides outcomes: the server in production
    /// (<see cref="RemoteRunService"/>), the shared rules in-process for the demo (<see cref="LocalRunService"/>).
    /// The client never computes a result; it sends intents and renders what comes back.
    /// </summary>
    public interface IRunService
    {
        UniTask<RunSnapshot> StartAsync(CancellationToken cancellationToken);
        UniTask<SpinOutcome> SpinAsync(CancellationToken cancellationToken);
        UniTask<RunSnapshot> LeaveAsync(CancellationToken cancellationToken);
        UniTask<RunSnapshot> ReviveAsync(CancellationToken cancellationToken);
        UniTask<RunSnapshot> RestartAsync(CancellationToken cancellationToken);
    }
}
