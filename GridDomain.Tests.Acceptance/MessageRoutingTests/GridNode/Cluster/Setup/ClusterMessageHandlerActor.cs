using GridDomain.Node.AkkaMessaging;
using GridDomain.Tests.Acceptance.MessageRoutingTests.GridNode.SingleSystem.Setup;

namespace GridDomain.Tests.Acceptance.MessageRoutingTests.GridNode.Cluster.Setup
{
    class ClusterMessageHandlerActor : MessageHandlingActor<ClusterMessage, TestHandler>
    {
        public ClusterMessageHandlerActor(TestHandler handler) : base(handler)
        {
        }

        protected override void OnReceive(object msg)
        {
            ((ClusterMessage)msg).ProcessorActorSystemAdress = Akka.Cluster.Cluster.Get(Context.System).SelfAddress;
            base.OnReceive(msg);
        }
    }
}