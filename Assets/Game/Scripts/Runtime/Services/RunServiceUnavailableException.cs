using System;

namespace Game.Runtime.Services
{
    /// <summary>
    /// The authority could not be reached or did not answer (connection error, timeout, 5xx).
    /// Transient by definition: the flow shows the error popup and resends the same intent on RETRY.
    /// Anything else (a refused intent, a bug) is not this exception and is not retried.
    /// </summary>
    public sealed class RunServiceUnavailableException : Exception
    {
        public RunServiceUnavailableException(string message, Exception inner = null) : base(message, inner) { }
    }
}
