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
    public class TcpingClient
    {
        public TcpingClient(IPAddress[] addresses, int port, bool realRtt)
        {
            Addresses = addresses;
            Port = port;
            RealRtt = realRtt;

            _stats = new Dictionary<IPAddress, List<double>>();
            
            foreach (var address in Addresses)
            {
                _stats.Add(address, new List<double>());
            }
        }

        public delegate void TcpingRespondedEventHandler(object? sender, TcpingRespondedEventArgs e);
        
        public event TcpingRespondedEventHandler? TcpingResponded;

        private readonly Dictionary<IPAddress, List<double>> _stats;
        private readonly List<Thread> _workers = new List<Thread>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public IPAddress[] Addresses { get; }
        public int Port { get; }
        public int Timeout { get; set; } = 5000;
        public bool RealRtt { get; set; } = false;
        public ReadOnlyDictionary<IPAddress, ReadOnlyCollection<double>> Stats 
            => new ReadOnlyDictionary<IPAddress, ReadOnlyCollection<double>>(
                _stats.ToDictionary(
                    x => x.Key, x=> new ReadOnlyCollection<double>(x.Value)
                    )
                );
        public bool IsActive => _workers.Any(x => x.IsAlive);
        public bool IsCancellationRequested => _cancellationTokenSource.IsCancellationRequested;

        public void Stop(bool force = false)
        {
            if (!(IsActive || force)) throw new InvalidOperationException("Tcping client hasn't started yet.");
            
            _cancellationTokenSource.Cancel();
        }
        
        public void Start()
        {
            if (IsActive) throw new InvalidOperationException("Tcping client has already started.");
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
                            throw new TimeoutException("timeout");
                        }
                        if (_cancellationTokenSource.IsCancellationRequested) return;
                        
                        if (RealRtt)
                            statsList.Add(time / 2);
                        else
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