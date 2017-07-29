using Common;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

namespace NetMQServer.Ticker
{
    public class NetMQPublisher : ITickerPublisher
    {
        private const string PublishTicker = "P";

        public class ShimHandler : IShimHandler
        {
            private PublisherSocket _publisherSocket;
            private ResponseSocket _snapshotSocket;
            private readonly ITickerRepository tickerRepository;
            private NetMQPoller _poller;
            private NetMQTimer _heartbeatTimer;

            public ShimHandler(ITickerRepository tickerRepository)
            {
                this.tickerRepository = tickerRepository;
            }

            public void Initialise(object state)
            {

            }

            public void Run(PairSocket shim)
            {
                _publisherSocket = new PublisherSocket();
                _publisherSocket.Bind("tcp://*:" + StreamingProtocol.Port);

                _snapshotSocket = new ResponseSocket();
                _snapshotSocket.Bind("tcp://*:" + SnapshotProtocol.Port);
                _snapshotSocket.ReceiveReady += OnSnapshotReady;

                shim.ReceiveReady += OnShimReady;

                _heartbeatTimer = new NetMQTimer(StreamingProtocol.HeartbeatInterval);
                _heartbeatTimer.Elapsed += OnHeartbeatTimerElapsed;

                shim.SignalOK();

                _poller = new NetMQPoller() { shim, _snapshotSocket, _heartbeatTimer };
                _poller.Run();

                _publisherSocket.Dispose();
                _snapshotSocket.Dispose();
            }

            private void OnHeartbeatTimerElapsed(object sender, NetMQTimerEventArgs e)
            {
                _publisherSocket.SendFrame(StreamingProtocol.HeartbeatTopic);
            }

            private void OnSnapshotReady(object sender, NetMQSocketEventArgs e)
            {
                var command = _snapshotSocket.ReceiveFrameString();

                // Currently we only have one type of events
                if (command != SnapshotProtocol.GetTradessCommand)
                    return;

                var tickers = tickerRepository.GetAllTickers();

                // we will send all the tickers in one message
                foreach (var ticker in tickers)
                {
                    _snapshotSocket.SendMoreFrame(JsonConvert.SerializeObject(ticker));
                }

                _snapshotSocket.SendFrame(SnapshotProtocol.EndOfTickers);
            }

            private void OnShimReady(object sender, NetMQSocketEventArgs e)
            {
                var command = e.Socket.ReceiveFrameString();

                switch (command)
                {
                    //case ActorKnownMessages.END_PIPE:
                    case NetMQActor.EndShimMessage:
                        _poller.Stop();
                        break;

                    case PublishTicker:
                        var topic = e.Socket.ReceiveFrameString();
                        var json = e.Socket.ReceiveFrameString();

                        _publisherSocket.
                            SendMoreFrame(topic).
                            SendFrame(json);
                        break;
                }

            }
        }

        private NetMQActor _actor;
        private readonly ITickerRepository _tickerRepository;

        public NetMQPublisher(ITickerRepository tickerRepository)
        {
            _tickerRepository = tickerRepository;
        }

        public void Start()
        {
            if (_actor != null)
                return;

            _actor = NetMQActor.Create(new ShimHandler(_tickerRepository));
        }

        public void Stop()
        {
            if (_actor != null)
            {
                _actor.Dispose();
                _actor = null;
            }
        }

        public void PublishTrade(TickerDto ticker)
        {
            if (_actor == null)
                return;

            _actor.
                SendMoreFrame(PublishTicker).
                SendMoreFrame(StreamingProtocol.TradesTopic).
                SendFrame(JsonConvert.SerializeObject(ticker));
        }

    }
}
