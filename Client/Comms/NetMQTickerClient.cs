using Client.Comms.Transport;
using Common;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Client.Comms
{
    public class NetMQTickerClient : IDisposable
    {
        //private Actor<object> actor;
        private readonly NetMQActor _actor;
        private Subject<TickerDto> subject;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        class ShimHandler : IShimHandler
        {
            private SubscriberSocket _subscriberSocket;
            private Subject<TickerDto> _subject;
            private readonly string _address;
            private NetMQPoller _poller;
            private NetMQTimer _timeoutTimer;

            public ShimHandler(Subject<TickerDto> subject, string address)
            {
                this._address = address;
                this._subject = subject;
            }

            public void Initialise(object state)
            {

            }

            public void Run(PairSocket shim)
            {
                // we should signal before running the poller but this will block the application
                shim.SignalOK();

                _poller = new NetMQPoller();

                shim.ReceiveReady += OnShimReady;
                _poller.Add(shim);

                _timeoutTimer = new NetMQTimer(StreamingProtocol.Timeout);
                _timeoutTimer.Elapsed += TimeoutElapsed;
                _poller.Add(_timeoutTimer);

                Connect();

                _poller.Run();

                if (_subscriberSocket != null)
                {
                    _subscriberSocket.Dispose();
                }
            }

            private void Connect()
            {
                // getting the snapshot
                using (var requestSocket = new RequestSocket())
                {

                    requestSocket.Connect(string.Format("tcp://{0}:{1}", _address, SnapshotProtocol.Port));

                    requestSocket.SendFrame(SnapshotProtocol.GetTradessCommand);

                    var json = string.Empty;

                    if (requestSocket.TryReceiveFrameString(SnapshotProtocol.RequestTimeout, out json) == false)
                    {
                        Task.Run(() => _subject.OnError(new Exception("No response from server")));
                        return;
                    }

                    while (json != SnapshotProtocol.EndOfTickers)
                    {
                        PublishTicker(json);

                        json = requestSocket.ReceiveFrameString();
                    }
                }

                //subscriberSocket = context.CreateSubscriberSocket();
                _subscriberSocket = new SubscriberSocket();
                _subscriberSocket.Subscribe(StreamingProtocol.TradesTopic);
                _subscriberSocket.Subscribe(StreamingProtocol.HeartbeatTopic);
                _subscriberSocket.Connect(string.Format("tcp://{0}:{1}", _address, StreamingProtocol.Port));
                _subscriberSocket.ReceiveReady += OnSubscriberReady;

                _poller.Add(_subscriberSocket);

                // reset timeout timer
                _timeoutTimer.Enable = false;
                _timeoutTimer.Enable = true;
            }

            private void TimeoutElapsed(object sender, NetMQTimerEventArgs e)
            {
                // no need to reconnect, the client would be recreated because of RX

                // because of RX internal stuff invoking on the poller thread block the entire application, so calling on Thread Pool
                Task.Run(() => _subject.OnError(new Exception("Disconnected from server")));
            }

            private void OnShimReady(object sender, NetMQSocketEventArgs e)
            {
                var command = e.Socket.ReceiveFrameString();

                if(command==NetMQActor.EndShimMessage)
                //if (command == ActorKnownMessages.END_PIPE)
                {
                    _poller.Stop();
                }
            }

            private void OnSubscriberReady(object sender, NetMQSocketEventArgs e)
            {
                var topic = _subscriberSocket.ReceiveFrameString();

                if (topic == StreamingProtocol.TradesTopic)
                {
                    var json = _subscriberSocket.ReceiveFrameString();
                    PublishTicker(json);

                    // reset timeout timer also when a quote is received
                    _timeoutTimer.Enable = false;
                    _timeoutTimer.Enable = true;
                }
                else if (topic == StreamingProtocol.HeartbeatTopic)
                {
                    // reset timeout timer
                    _timeoutTimer.Enable = false;
                    _timeoutTimer.Enable = true;
                }
            }

            private void PublishTicker(string json)
            {
                TickerDto tickerDto = JsonConvert.DeserializeObject<TickerDto>(json);
                _subject.OnNext(tickerDto);
            }
        }

        public NetMQTickerClient(string address)
        {
            subject = new Subject<TickerDto>();

            _actor = NetMQActor.Create(new ShimHandler(subject, address));
            _disposables.Add(this._actor);

            _disposables.Add(NetMQHeartBeatClient.Instance.GetConnectionStatusStream()
                .Where(x => x.ConnectionStatus == ConnectionStatus.Closed)
                .Subscribe(x =>
                    this.subject.OnError(new InvalidOperationException("Connection to server has been lost"))));
        }

        public IObservable<TickerDto> GetTickerStream()
        {
            return subject.AsObservable();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
