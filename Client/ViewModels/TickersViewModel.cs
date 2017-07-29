using Client.Factory;
using Client.Repositories;
using Client.Services;
using log4net;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;


namespace Client.ViewModels
{
    public class TickersViewModel : INPCBase
    {
        private readonly ITickerRepository _tickerRepository;
        private readonly IConcurrencyService _concurrencyService;
        private bool _stale = false;
        private static readonly ILog Log = LogManager.GetLogger(typeof(TickersViewModel));

        public TickersViewModel(IReactiveTrader reactiveTrader,
                                IConcurrencyService concurrencyService,
                                TickerViewModelFactory tickerViewModelFactory)
        {
            Tickers = new ObservableCollection<TickerViewModel>();

            Tickers.Add(tickerViewModelFactory.Create("Yahoo"));
            Tickers.Add(tickerViewModelFactory.Create("Google"));
            Tickers.Add(tickerViewModelFactory.Create("Apple"));
            Tickers.Add(tickerViewModelFactory.Create("Facebook"));
            Tickers.Add(tickerViewModelFactory.Create("Microsoft"));
            Tickers.Add(tickerViewModelFactory.Create("Twitter"));

            _tickerRepository = reactiveTrader.TickerRepository;
            _concurrencyService = concurrencyService;
            //_stale = stale;

            LoadTrades();
        }

        public ObservableCollection<TickerViewModel> Tickers { get; private set; }

        private void LoadTrades()
        {
            _tickerRepository.GetTickerStream()
                            .ObserveOn(_concurrencyService.Dispatcher)
                            .SubscribeOn(_concurrencyService.TaskPool)
                            .Subscribe(
                                AddTicker,
                                ex => Log.Error("An error occurred within the trade stream", ex));
        }

        private void AddTicker(Ticker ticker)
        {
            //var allTickers = incomingTickers as IList<Ticker> ?? incomingTickers.ToList();
            //if (!allTickers.Any())
            //{
            //    // empty list of trades means we are disconnected
            //    stale = true;
            //}
            //else
            //{
            //    if (stale)
            //    {
            //        stale = false;
            //    }
            //}

            //foreach (var ticker in allTickers)
            //{
            //    Tickers.Single(x => x.Name == ticker.Name)
            //        .AcceptNewPrice(ticker.Price);
            //}

            Tickers.Single(x => x.Name == ticker.Name)
                 .AcceptNewPrice(ticker.Price);
        }

        private void UpdateTickerViewModelsStaleState(bool stale)
        {
            foreach (var tickerViewModel in Tickers)
            {
                tickerViewModel.Stale = stale;
            }
        }
    }
}
