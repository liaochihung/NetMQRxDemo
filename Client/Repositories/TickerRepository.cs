using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Client.Factory;
using Client.Comms;

namespace Client.Repositories
{
    class TickerRepository : ITickerRepository
    {
        private readonly ITickerClient _tickerClient;
        private readonly ITickerFactory _tickerFactory;

        public TickerRepository(ITickerClient tickerClient, ITickerFactory tickerFactory)
        {
            this._tickerClient = tickerClient;
            this._tickerFactory = tickerFactory;
        }

        public IObservable<Ticker> GetTickerStream()
        {
            return Observable.Defer(() => _tickerClient.GetTickerStream())
                .Select(_tickerFactory.Create)                
                .Catch<Ticker>(Observable.Empty<Ticker>())
                .Repeat()
                .Publish()
                .RefCount();
        }

    }
}
