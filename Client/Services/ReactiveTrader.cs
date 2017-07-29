using Client.Comms;
using Client.Comms.Transport;
using Client.Factory;
using Client.Hub;
using Client.Repositories;
using log4net;
using System;
using System.Reactive.Linq;

namespace Client.Services
{
    public class ReactiveTrader : IReactiveTrader, IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ReactiveTrader));

        public void Initialize(string username, string server)
        {
            var concurrencyService = new ConcurrencyService();

            //context = NetMQContext.Create();

            var tickerClient = new TickerClient(server);
            var netMQHeartBeatClient = NetMQHeartBeatClient.CreateInstance(server);
            HeartBeatClient = new HeartBeatClient();

            var tickerFactory = new TickerFactory();
            TickerRepository = new TickerRepository(tickerClient, tickerFactory);
        }

        public ITickerRepository TickerRepository { get; private set; }
        public IHeartBeatClient HeartBeatClient { get; private set; }

        public IObservable<ConnectionInfo> ConnectionStatusStream
        {
            get
            {
                return HeartBeatClient.ConnectionStatusStream()
                    .Repeat()
                    .Publish()
                    .RefCount();
            }
        }

        public void Dispose()
        {
            //context.Dispose();
        }
    }

}
