using Common;

namespace NetMQServer.Ticker
{
    public interface ITickerPublisher
    {
        void Start();
        void Stop();
        void PublishTrade(TickerDto ticker);
    }
}
