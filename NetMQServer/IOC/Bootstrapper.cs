using Autofac;
using NetMQServer.Ticker;

namespace NetMQServer.IOC
{
    public class Bootstrapper
    {
        public IContainer Build()
        {
            var builder = new ContainerBuilder();

            // NetMQ
            builder.RegisterType<NetMQPublisher>().As<ITickerPublisher>().SingleInstance();
            builder.RegisterType<TickerRepository>().As<ITickerRepository>().SingleInstance();

            // UI
            builder.RegisterType<MainWindow>().SingleInstance();
            builder.RegisterType<MainWindowViewModel>().SingleInstance();

            return builder.Build();
        }
    }
}
