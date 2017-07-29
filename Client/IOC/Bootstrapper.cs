using Autofac;
using Client.Services;
using Client.ViewModels;
using Client.ViewModels.MainWindow;

namespace Client.IOC
{
    public class Bootstrapper
    {
        public IContainer Build()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<ReactiveTrader>().As<IReactiveTrader>().SingleInstance();
            builder.RegisterType<UserProvider>().As<IUserProvider>();
            builder.RegisterType<ConcurrencyService>().As<IConcurrencyService>();

            // UI
            builder.RegisterType<MainWindow>().SingleInstance();
            builder.RegisterType<MainWindowViewModel>().SingleInstance();
            builder.RegisterType<TickersViewModel>().SingleInstance();
            builder.RegisterType<ConnectivityStatusViewModel>().SingleInstance();
            builder.RegisterType<TickerViewModelFactory>().SingleInstance();

            return builder.Build();
        }
    }
}
