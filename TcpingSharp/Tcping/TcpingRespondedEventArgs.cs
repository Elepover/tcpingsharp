using System;
using System.Net;

namespace TcpingSharp.Tcping
{
    /// <summary>
    /// Provides data for the <see cref="TcpingClient.TcpingResponded"/> event.
    /// </summary>
    public class TcpingRespondedEventArgs : EventArgs
    {
        public TcpingRespondedEventArgs(IPAddress address, int port, int sequence, double time, Exception? exception = null)
        {
            Address = address;
            Port = port;
            Sequence = sequence;
            Time = time;
            Exception = exception;
        }
        /// <summary>
        /// Indicates whether the tcping attempt was successful.
        /// </summary>
        public bool IsSuccessful => Exception is null;
        /// <summary>
        /// The tcping target's IP address.
        /// </summary>
        public IPAddress Address { get; }
        /// <summary>
        /// The tcping target's port number.
        /// </summary>
        public int Port { get; }
        /// <summary>
        /// # of tcping attempt.
        /// </summary>
        public int Sequence { get; }
        /// <summary>
        /// Time spent to get this result.
        /// </summary>
        public double Time { get; }
        /// <summary>
        /// Exception details. If there's no exception thrown (succeeded), this property will be <see langword="null"/>.
        /// </summary>
        public Exception? Exception { get; }
    }
}