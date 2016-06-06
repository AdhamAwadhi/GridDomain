using System;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.DI.Core;
using Akka.Routing;
using GridDomain.CQRS;
using GridDomain.Logging;
using NLog;

namespace GridDomain.Node.AkkaMessaging.Routing
{
    public abstract class AkkaRoutingActor : TypedActor, IHandler<CreateHandlerRoute>,
                                                         IHandler<CreateActorRoute>
    {
        private readonly IHandlerActorTypeFactory _actorTypeFactory;
        protected readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly IActorSubscriber _subscriber;

        protected readonly RouterConfig DefaultRouter = new RandomPool(Environment.ProcessorCount);

        protected AkkaRoutingActor(IHandlerActorTypeFactory actorTypeFactory,
            IActorSubscriber subscriber)
        {
            _subscriber = subscriber;
            _actorTypeFactory = actorTypeFactory;
        }
       
        public void Handle(CreateActorRoute e)
        {
            var aggregateActorOpenType = typeof(AggregateHostActor<>);
            var actorType = aggregateActorOpenType.MakeGenericType(e.AggregateType);

            string actorName = $"Message_handler_for_{e.AggregateType.Name}_{Guid.NewGuid()}";
            var handleActor = CreateHandleActor(e, actorType, CreateActorRouter, actorName);

            foreach (var msgRoute in e.Routes)
                _subscriber.Subscribe(msgRoute.MessageType, handleActor, Self);
        }

        public void Handle(CreateHandlerRoute e)
        {
            var actorType = _actorTypeFactory.GetActorTypeFor(e.MessageType, e.HandlerType);

            string actorName = $"Message_handler_for_{e.MessageType.Name}_{Guid.NewGuid()}";
            var handleActor = CreateHandleActor(e, actorType, CreateRouter, actorName);
            _log.Trace($"Created message handling actor for {e.ToPropsString()}");

            _subscriber.Subscribe(e.MessageType, handleActor, Self);
        }

        protected abstract RouterConfig CreateActorRouter(CreateActorRoute msg);
        protected abstract RouterConfig CreateRouter(CreateHandlerRoute handlerRouteConfigMessage);

        private IActorRef CreateHandleActor<TMessage>(TMessage msg, 
                                                      Type actorType,
                                                      Func<TMessage, RouterConfig> routerFactory,
                                                      string actorName = null)
        {
            var handleActorProps = Context.System.DI().Props(actorType);
            var routeConfig = routerFactory(msg);
            handleActorProps = handleActorProps.WithRouter(routeConfig);

            var handleActor = Context.System.ActorOf(handleActorProps, actorName);
            return handleActor;
        }

        protected ConsistentHashMapping GetCorrelationPropertyFromMessage(CreateHandlerRoute handlerRouteConfigMessage)
        {
            return m =>
            {
                var msgType = m.GetType();
                if (msgType != handlerRouteConfigMessage.MessageType)
                {
                    _log.Trace($"Bad message type. Expected:{handlerRouteConfigMessage.MessageType}, got:{msgType}");
                    return null;
                }

                var value = msgType.GetProperty(handlerRouteConfigMessage.MessageCorrelationProperty)
                    .GetValue(m);
                if (!(value is Guid))
                    throw new InvalidCorrelationPropertyValue(value);

                _log.Trace($"created correlation id for message {m.GetType()}: {value}");
                return value;
            };
        }
    }


}