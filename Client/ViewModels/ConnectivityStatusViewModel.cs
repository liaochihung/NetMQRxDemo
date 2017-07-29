using Client.Comms.Transport;
using Client.Services;
using log4net;
using System;
using System.Reactive.Linq;

namespace Client.ViewModels
{
    public class ConnectivityStatusViewModel : INPCBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ConnectivityStatusViewModel));
        private string _server;
        private string _status;
        private bool _disconnected;

        public ConnectivityStatusViewModel(
            IReactiveTrader reactiveTrader,
            IConcurrencyService concurrencyService)
        {
            reactiveTrader.ConnectionStatusStream
                .ObserveOn(concurrencyService.Dispatcher)
                .SubscribeOn(concurrencyService.TaskPool)
                .Subscribe(
                OnStatusChange,
                ex => Log.Error("An error occurred within the connection status stream.", ex));
        }

        private void OnStatusChange(ConnectionInfo connectionInfo)
        {
            Server = connectionInfo.Server;

            switch (connectionInfo.ConnectionStatus)
            {
                case ConnectionStatus.Connecting:
                    Status = "Connecting...";
                    Disconnected = true;
                    break;
                case ConnectionStatus.Connected:
                    Status = "Connected";
                    Disconnected = false;
                    break;
                case ConnectionStatus.Closed:
                    Status = "Disconnected";
                    Disconnected = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public string Server
        {
            get { return this._server; }
            set
            {
                this._server = value;
                base.OnPropertyChanged("Server");
            }
        }

        public string Status
        {
            get { return this._status; }
            set
            {
                this._status = value;
                base.OnPropertyChanged("Status");
            }
        }



        public bool Disconnected
        {
            get { return this._disconnected; }
            set
            {
                this._disconnected = value;
                base.OnPropertyChanged("Disconnected");
            }
        }

    }

}
