namespace Client.ViewModels.MainWindow
{
    public class MainWindowViewModel
    {
        public MainWindowViewModel(
            TickersViewModel tickersViewModel,
            ConnectivityStatusViewModel connectivityStatusViewModel
            )
        {
            this.TickersViewModel = tickersViewModel;
            this.ConnectivityStatusViewModel = connectivityStatusViewModel;
        }

        public TickersViewModel TickersViewModel { get; private set; }
        public ConnectivityStatusViewModel ConnectivityStatusViewModel { get; private set; }
    }
}
