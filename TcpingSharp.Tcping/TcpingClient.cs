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
            if (IsCancellationRequested) throw new InvalidOperationException($"The {nameof(TcpingClient)} has been requested to stop.");
            
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Stop pinging targets and return when complete.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the task.</param>
        /// <inheritdoc cref="Stop"/>
        public async Task StopAsync(CancellationToken? cancellationToken)
        {
            Stop();
            while (IsActive && !(cancellationToken?.IsCancellationRequested ?? false))
                await Task.Delay(10);
        }
        
        /// <summary>
        /// Start this <see cref="TcpingClient"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="TcpingClient"/> is already busy pinging targets or has been shut down. You should always check the <see cref="IsActive"/> property before calling it.</exception>
        public void Start()
        {
            if (IsActive) throw new InvalidOperationException($"{nameof(TcpingClient)} has already started.");
            if (IsCancellationRequested) throw new InvalidOperationException("Cannot start again after stop requested.");
            
            foreach (var address in Addresses)
            {
                var thread = new Thread(() => Tcping(address));
                _workers.Add(thread);
                thread.Start();
            }
        }
        
        private void Tcping(IPAddress address)
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { Blocking = true })
                {
                    var statsList = _stats[address];
                    var sw = new Stopwatch();
                    sw.Start();
                    try
                    {
                        var task = socket.ConnectAsync(address, Port);
                        task.Wait(Timeout, _cancellationTokenSource.Token);
                        var time = sw.Elapsed.TotalMilliseconds;
                        if (time > Timeout)
                        {
                            // apparently timed out
                            throw new TimeoutException("Timed out waiting for response");
                        }
                        if (_cancellationTokenSource.IsCancellationRequested) return;
                        
                        statsList.Add(time);
                        TcpingResponded?.Invoke(this, new TcpingRespondedEventArgs(address, Port, statsList.Count, RealRtt ? time / 2 : time));
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException) return;
                        if (ex is AggregateException && !(ex.InnerException is null))
                            ex = ex.InnerException;
                        
                        statsList.Add(0);
                        TcpingResponded?.Invoke(this, new TcpingRespondedEventArgs(address, Port, statsList.Count, sw.Elapsed.TotalMilliseconds, ex));
                    }
                    socket.Close();
                }

                try
                {
                    // ReSharper disable once MethodSupportsCancellation
                    Task.Delay(1000, _cancellationTokenSource.Token).Wait();
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch { }
            }
        }
    }
}