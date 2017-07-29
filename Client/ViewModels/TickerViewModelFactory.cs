using Client.Services;

namespace Client.ViewModels
{
    public class TickerViewModelFactory
    {
        private readonly IReactiveTrader _reactiveTrader;
        private readonly IConcurrencyService _concurrencyService;

        public TickerViewModelFactory(
            IReactiveTrader reactiveTrader,
            IConcurrencyService concurrencyService)
        {
            _reactiveTrader = reactiveTrader;
            _concurrencyService = concurrencyService;
        }

        public TickerViewModel Create(string name)
        {
            return new TickerViewModel(_reactiveTrader, _concurrencyService, name);
        }
    }
}
