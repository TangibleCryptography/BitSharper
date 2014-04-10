using System;

namespace BitSharper.Threading.Collections.Generic
{
    /// <summary>
    /// Exception to indicate a queue is already broken.
    /// </summary>
    [Serializable]
    internal class QueueBrokenException : Exception
    {
    }
}