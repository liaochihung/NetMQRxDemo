using Client.Comms.Transport;
using Common;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Comms
{
    public class NetMQHeartBeatClient
    {
        //private readonly NetMQContext context;
        private readonly string address;
        //private Actor<object> actor;
        private NetMQActor actor;
        private Subject<ConnectionInfo> subject;
        private static NetMQHeartBeatClient instance = null;
        private static object syncLock = new object();
        protected int requiresInitialisation = 1;

        class ShimHandler : IShimHandler
        {
            //private NetMQContext context;
            private SubscriberSocket subscriberSocket;
            private Subject<ConnectionInfo> subject;
            private string address;
            private NetMQPoller poller;
            private NetMQTimer timeoutTimer;
            private NetMQHeartBeatClient parent;

            public ShimHandler(object parent, Subject<ConnectionInfo> subject, string address)
            {
                this.address = address;
                this.subject = subject;
                // 原專案中忘了呼叫此函式
                Initialise(parent);
            }

            public void Initialise(object state)
            {
                parent = (NetMQHeartBeatClient)state;
            }

            public void Run(PairSocket shim)
            {
                // we should signal before running the poller but this will block the application
                shim.SignalOK();

                this.poller = new NetMQPoller();

                shim.ReceiveReady += OnShimReady;
                poller.Add(shim);

                timeoutTimer = new NetMQTimer(StreamingProtocol.Timeout);
                timeoutTimer.Elapsed += TimeoutElapsed;
                poller.Add(timeoutTimer);

                Connect();

                poller.Run();

                if (subscriberSocket != null)
                {
                    subscriberSocket.Dispose();
                }
            }

            private void Connect()
            {
                //subscriberSocket = context.CreateSubscriberSocket();
                subscriberSocket = new SubscriberSocket();
                subscriberSocket.Subscribe(StreamingProtocol.HeartbeatTopic);
                subscriberSocket.Connect(string.Format("tcp://{0}:{1}", address, StreamingProtocol.Port));

                subject.OnNext(new ConnectionInfo(ConnectionStatus.Connecting, this.address));


                subscriberSocket.ReceiveReady += OnSubscriberReady;

                poller.Add(subscriberSocket);



                // reset timeout timer
                timeoutTimer.Enable = false;
                timeoutTimer.Enable = true;
            }

            private void TimeoutElapsed(object sender, NetMQTimerEventArgs e)
            {
                // no need to reconnect, the client would be recreated because of RX

                // because of RX internal stuff invoking on the poller thread block the entire application, so calling on Thread Pool
                Task.Run(() =>
                {
                    parent.requiresInitialisation = 1;
                    subject.OnNext(new ConnectionInfo(ConnectionStatus.Closed, this.address));
                });
            }

            private void OnShimReady(object sender, NetMQSocketEventArgs e)
            {
                string command = e.Socket.ReceiveFrameString();

                //if (command == ActorKnownMessages.END_PIPE)
                if (command == NetMQActor.EndShimMessage)
                {
                    poller.Stop();
                }
            }

            private void OnSubscriberReady(object sender, NetMQSocketEventArgs e)
            {
                string topic = subscriberSocket.ReceiveFrameString();

                if (topic == StreamingProtocol.HeartbeatTopic)
                {
                    subject.OnNext(new ConnectionInfo(ConnectionStatus.Connected, this.address));

                    // reset timeout timer
                    timeoutTimer.Enable = false;
                    timeoutTimer.Enable = true;
                }
            }

        }







        private NetMQHeartBeatClient(string address)
        {
            //this.context = context;
            this.address = address;
            InitialiseComms();
        }

        public static NetMQHeartBeatClient CreateInstance(string address)
        {
            if (instance == null)
            {
                lock (syncLock)
                {
                    if (instance == null)
                    {
                        instance = new NetMQHeartBeatClient(address);
                    }
                }
            }
            return instance;
        }

        public void InitialiseComms()
        {

            if (Interlocked.CompareExchange(ref requiresInitialisation, 0, 1) == 1)
            {
                if (actor != null)
                {
                    this.actor.Dispose();
                }

                subject = new Subject<ConnectionInfo>();
                //this.actor = new Actor<object>(context, new ShimHandler(context, subject, address), this);
                actor = NetMQActor.Create(new ShimHandler(this, subject, address));
            }


        }

        public IObservable<ConnectionInfo> GetConnectionStatusStream()
        {
            return subject.AsObservable();
        }


        public static NetMQHeartBeatClient Instance
        {
            get { return instance; }

        }





    }
}
