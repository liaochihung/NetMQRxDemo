using Common;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Client.Comms
{
    public class TickerClient : ITickerClient
    {
        private readonly string _address;

        public TickerClient(string address)
        {
            _address = address;
        }

        public IObservable<TickerDto> GetTickerStream()
        {
            return Observable.Create<TickerDto>(observer =>
            {
                var client = new NetMQTickerClient(_address);

                var disposable = client.GetTickerStream().Subscribe(observer);
                return new CompositeDisposable { client, disposable };
            })
            .Publish()
            .RefCount();
        }


        //public IObservable<ConnectionInfo> ConnectionStatusStream()
        //{
        //    return Observable.Create<ConnectionInfo>(observer =>
        //    {
        //        NetMQHeartBeatClient.Instance.InitialiseComms();

        //        var disposable = NetMQHeartBeatClient.Instance.GetConnectionStatusStream().Subscribe(observer);
        //        return new CompositeDisposable { disposable };
        //    })
        //    .Publish()
        //    .RefCount();
        //}
    }
}
