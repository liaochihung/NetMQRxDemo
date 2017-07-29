using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Comms.Transport;
using Client.Services;
using log4net;

namespace Client.ViewModels
{
    public class TickerViewModel : INPCBase
    {
        private decimal _price;
        private bool _isUp;
        private bool _stale;
        private bool _disconnected;
        private static readonly ILog Log = LogManager.GetLogger(typeof(TickerViewModel));

        public TickerViewModel(
            IReactiveTrader reactiveTrader,
            IConcurrencyService concurrencyService,
            string name)
        {
            Name = name;

            reactiveTrader.ConnectionStatusStream
                .ObserveOn(concurrencyService.Dispatcher)
                .SubscribeOn(concurrencyService.TaskPool)
                .Subscribe(
                    OnStatusChange,
                    ex => Log.Error("An error occurred within the connection status stream.", ex));
        }

        public string Name { get; private set; }

        public void AcceptNewPrice(decimal newPrice)
        {
            IsUp = newPrice > _price;
            Price = newPrice;
        }

        public decimal Price
        {
            get { return _price; }
            private set
            {
                _price = value;
                base.OnPropertyChanged("Price");
            }
        }

        public bool IsUp
        {
            get { return _isUp; }
            private set
            {
                _isUp = value;
                base.OnPropertyChanged("IsUp");
            }
        }

        public bool Stale
        {
            get { return _stale; }
            set
            {
                _stale = value;
                base.OnPropertyChanged("Stale");
            }
        } 
        
        public bool Disconnected
        {
            get { return _disconnected; }
            set
            {
                _disconnected = value;
                base.OnPropertyChanged("Disconnected");
            }
        }

        private void OnStatusChange(ConnectionInfo connectionInfo)
        {
            switch (connectionInfo.ConnectionStatus)
            {
                case ConnectionStatus.Connecting:
                    Disconnected = true;
                    break;
                case ConnectionStatus.Connected:
                    Disconnected = false;
                    break;
                case ConnectionStatus.Closed:
                    Disconnected = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }
}
