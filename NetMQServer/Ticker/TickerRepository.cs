using Common;
using System.Collections.Generic;
using System.Linq;

namespace NetMQServer.Ticker
{
    public class TickerRepository : ITickerRepository
    {
        private readonly Queue<TickerDto> _tickers = new Queue<TickerDto>();
        private readonly object _syncLock = new object();
        private const int MaxTrades = 50;

        public TickerRepository()
        {
            _tickers.Enqueue(new TickerDto() { Name = "Yahoo", Price = 1.2m });
            _tickers.Enqueue(new TickerDto() { Name = "Google", Price = 1022m });
            _tickers.Enqueue(new TickerDto() { Name = "Apple", Price = 523m });
            _tickers.Enqueue(new TickerDto() { Name = "Facebook", Price = 49m });
            _tickers.Enqueue(new TickerDto() { Name = "Microsoft", Price = 37m });
            _tickers.Enqueue(new TickerDto() { Name = "Twitter", Price = 120m });
        }

        public TickerDto GetNextTicker()
        {
            return _tickers.Dequeue();
        }

        public void StoreTicker(TickerDto tickerInfo)
        {
            lock (_syncLock)
            {
                _tickers.Enqueue(tickerInfo);

                if (_tickers.Count > MaxTrades)
                {
                    _tickers.Dequeue();
                }
            }
        }

        public IList<TickerDto> GetAllTickers()
        {
            IList<TickerDto> newTickers;

            lock (_syncLock)
            {
                newTickers = _tickers.ToList();
            }

            return newTickers;
        }
    }
}

