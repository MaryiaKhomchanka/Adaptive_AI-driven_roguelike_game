using System;

namespace Generation.Core
{
    /// <summary>Thrown when generation could not produce a valid layout within MaxGenerationAttempts. A configuration error, not a normal runtime condition.</summary>
    public sealed class GenerationException : Exception
    {
        public GenerationException(string message) : base(message) { }
    }

    /// <summary>Internal signal that the current attempt failed some rule and should be retried with a derived seed.</summary>
    sealed class RetryableGenerationException : Exception
    {
        public RetryableGenerationException(string message) : base(message) { }
    }
}
