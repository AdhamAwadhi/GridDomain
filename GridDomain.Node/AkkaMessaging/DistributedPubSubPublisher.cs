using System;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using GridDomain.CQRS.Messaging;
using GridDomain.Logging;
using NLog;

namespace GridDomain.Node.AkkaMessaging
{
    public class DistributedPubSubPublisher : IPublisher
    {
        private readonly IActorRef _publisherActor;
        private readonly Logger _log = LogManager.GetCurrentClassLogger();

        public DistributedPubSubPublisher(ActorSystem system)
        {
            var distributedPubSub = DistributedPubSub.Get(system);
            if (distributedPubSub == null)
                throw new CannotGetDistributedPubSubException();

            _publisherActor = distributedPubSub.Mediator;
        }

        public void Publish<T>(T msg)
        {
            var topic = msg.GetType().FullName;
            _log.Trace($"Publishing message {msg.ToPropsString()} to akka distributed pub sub with topic {topic}");
            _publisherActor.Tell(new Publish(topic, msg));
        }
    }

    public class CannotGetDistributedPubSubException : Exception
    {
    }
}