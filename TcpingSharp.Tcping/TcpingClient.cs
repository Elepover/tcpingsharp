using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpingSharp.Tcping
{
    /// <summary>
    /// Provides methods to ping hosts via TCP connection.
    /// </summary>
    public class TcpingClient
    {
        /// <summary>
        /// Construct a <see cref="TcpingClient"/> object.
        /// </summary>
        /// <param name="addresses">Ping targets.</param>
        /// <param name="port">Target port.</param>
        public TcpingClient(IPAddress[] addresses, int port)
        {
            Addresses = addresses;
            Port = port;

            _stats = new Dictionary<IPAddress, List<double>>();

            foreach (var address in Addresses)
            {
                _stats.Add(address, new List<double>());
            }
        }

        private readonly Dictionary<IPAddress, List<double>> _stats;
        private readonly List<Thread> _workers = new List<Thread>();
        
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Raised when a TCP ping result is generated.
        /// </summary>
        public event TcpingRespondedEventHandler? TcpingResponded;

        public delegate void TcpingRespondedEventHandler(object? sender, TcpingRespondedEventArgs e);

        /// <summary>
        /// Target IP addresses.
        /// </summary>
        public IPAddress[] Addresses { get; }

        /// <summary>
        /// Target port.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Ping timeout.
        /// </summary>
        public int Timeout { get; set; } = 5000;

        /// <summary>
        /// Decides whether to output 2xRTT/2 values of ping time or not.<br />
        /// Affects outputs in <see cref="Stats"/> and <see cref="TcpingRespondedEventArgs"/>.<br />
        /// Doesn't affect how statistics data are stored internally (always 2xRTT).
        /// </summary>
        public bool RealRtt { get; set; } = false;

        /// <summary>
        /// Statistics data.<br />
        /// Depends on <see cref="RealRtt"/> option, this will deliver different results.
        /// </summary>
        public ReadOnlyDictionary<IPAddress, ReadOnlyCollection<double>> Stats
        {
            get
            {
                var tempDictionary = new Dictionary<IPAddress, ReadOnlyCollection<double>>();
                foreach (var (address, latencies) in _stats)
                {
                    tempDictionary.Add(
                        address,
                        new ReadOnlyCollection<double>(
                            latencies.Select(x =>
                            {
                                if (RealRtt) return x / 2;
                                return x;
                            }).ToList()
                        )
                    );
                }

                return new ReadOnlyDictionary<IPAddress, ReadOnlyCollection<double>>(tempDictionary);
            }
        }

        /// <summary>
        /// Indicates whether current <see cref="TcpingClient"/> is busy.
        /// </summary>
        public bool IsActive => _workers.Any(x => x.IsAlive);

        /// <summary>
        /// Indicates whether cancellation of current <see cref="TcpingClient"/> is requested.
        /// </summary>
        public bool IsCancellationRequested => _cancellationTokenSource.IsCancellationRequested;

        /// <summary>
        /// Stop pinging targets.
        /// </summary>
        /// <param name="force">Set the internal <see cref="CancellationToken"/> to cancel. Regardless of current <see cref="TcpingClient"/> status.</param>
        /// <exception cref="InvalidOperationException">The <see cref="TcpingClient"/> isn't active or is shutting down.</exception>
        public void Stop(bool force = false)
        {
            if (!(IsActive || force)) throw new InvalidOperationException($"The {nameof(TcpingClient)} isn't active.");
            if (IsCancellationRequested)
                throw new InvalidOperationException($"The {nameof(TcpingClient)} has been requested to stop.");

            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Stop pinging targets and return when complete.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the task.</param>
        /// <inheritdoc cref="Stop"/>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            Stop();
            while (IsActive && !(cancellationToken.IsCancellationRequested))
                await Task.Delay(10);
        }

        /// <summary>
        /// Start this <see cref="TcpingClient"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="TcpingClient"/> is already busy pinging targets or has been shut down. You should always check the <see cref="IsActive"/> property before calling it.</exception>
        public void Start()
        {
            if (IsActive) throw new InvalidOperationException($"{nameof(TcpingClient)} has already started.");
            if (IsCancellationRequested)
                throw new InvalidOperationException("Cannot start again after stop requested.");

            foreach (var address in Addresses)
            {
                var thread = new Thread(() => TcpingInternal(address));
                _workers.Add(thread);
                thread.Start();
            }
        }

        /// <inheritdoc cref="PingAsync"/>
        public static TimeSpan Ping(IPAddress address, int port, int timeout = 5000)
        {
            var task = PingAsync(address, port, timeout);
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Ping a host via TCP protocol.
        /// </summary>
        /// <param name="address">Host IP address.</param>
        /// <param name="port">Target port.</param>
        /// <param name="timeout">Ping timeout.</param>
        /// <param name="token"><see cref="CancellationToken"/> used to cancel this action.</param>
        /// <returns>Time spent to establish a TCP connection.<br />Approximately 2x round-trip time per TCP protocol design.</returns>
        /// <exception cref="SocketException"/>
        /// <exception cref="TimeoutException">Timed out connecting to server.</exception>
        public static async Task<TimeSpan> PingAsync(IPAddress address, int port, int timeout = 5000,
            CancellationToken token = default)
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) {Blocking = true})
            {
                // initialize timing
                var sw = new Stopwatch();
                var timedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                timedTokenSource.CancelAfter(timeout);
                sw.Start();

                // try connecting to the server, with time limit
                try
                {
                    var connectTask = socket.ConnectAsync(address, port);
                    await connectTask.WaitAsync(timedTokenSource.Token);
                    // no TaskCanceledException thrown, not cancelled, so job done
                    return sw.Elapsed;
                }
                catch (TaskCanceledException)
                {
                    // not cancelled by user -> timed out
                    if (!token.IsCancellationRequested)
                        throw new TimeoutException("Timed out waiting for response");

                    // do nothing: cancelled by user
                    return TimeSpan.Zero;
                }
                catch (AggregateException ex)
                {
                    throw ex.Unwrap();
                }
            }
        }

        private void TcpingInternal(IPAddress address)
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var statsList = _stats[address];
                // use PingAsync's result value is most accurate, so if nothing happened, we'll always use that value
                // but just in case: in the catch() block we need this time.
                var faultStopwatch = new Stopwatch();
                try
                {
                    var task = PingAsync(address, Port, Timeout, _cancellationTokenSource.Token);
                    faultStopwatch.Start();
                    task.Wait();
                    var time = task.Result.TotalMilliseconds;
                    statsList.Add(time);
                    TcpingResponded?.Invoke(this,
                        new TcpingRespondedEventArgs(address, Port, statsList.Count, RealRtt ? time / 2 : time));
                }
                catch (Exception ex)
                {
                    faultStopwatch.Stop();
                    if (ex is TaskCanceledException) return;
                    if (ex is OperationCanceledException) return;
                    // unwrap the AggregateException if thrown by Wait()
                    ex = ex!.Unwrap();
                    statsList.Add(0);
                    TcpingResponded?.Invoke(this,
                        new TcpingRespondedEventArgs(address, Port, statsList.Count,
                            faultStopwatch.Elapsed.TotalMilliseconds,
                            ex));
                }

                try
                {
                    // ReSharper disable once MethodSupportsCancellation
                    Task.Delay(1000, _cancellationTokenSource.Token).Wait();
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }
            }
        }
    }
}
