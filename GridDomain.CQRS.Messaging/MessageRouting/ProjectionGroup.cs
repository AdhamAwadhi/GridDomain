using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Unity;

namespace GridDomain.CQRS.Messaging.MessageRouting
{
  //  public IProjectionGroupDescriptor Descriptor = new ProjectionGroupDescriptor();


    public class ProjectionGroupWorker : IProjectionGroup
    {

        public ProjectionGroupWorker(IUnityContainer locator, IProjectionGroupDescriptor descriptor)
        {
            var ProjectChain = descriptor.HandlersMap.ToDictionary(
                v => v.Key, v =>
                {
                    var handlersChain = v.Value.Select(handlerType => locator.Resolve(handlerType) )

                })
        }

        public void Project(object message)
        {
            throw new NotImplementedException();
        }
    }

    public class ProjectionGroup: IProjectionGroupDescriptor //IProjectionGroup
    {
        public IDictionary<Type, List<Type>> HandlersMap { get; }= new Dictionary<Type, List<Type>>();

        protected ProjectionGroup(IUnityContainer locator = null)
        {
        }

        public void Add<TMessage, THandler>(string correlationPropertyName)
                                            where THandler : IHandler<TMessage>
                                            where TMessage :class
        {
            List<Type> builderList;

            if (!HandlersMap.TryGetValue(typeof(TMessage), out builderList))
            {
                builderList = new List<Type>();
                HandlersMap[typeof(TMessage)] = builderList;
            }

            builderList.Add(typeof(THandler));

            if (_acceptMessages.All(m => m.MessageType != typeof (TMessage)))
                _acceptMessages.Add(new MessageRoute(typeof(TMessage), correlationPropertyName));
        }

     // private void ProjectMessage<TMessage, THandler>(object msg) where THandler : IHandler<TMessage> where TMessage : class
     // {
     //     var message = msg as TMessage;
     //     if(message == null)
     //         throw new UnknownMessageException();
     //
     //     try
     //     {
     //         var handler = _locator.Resolve<THandler>();
     //         handler.Handle(message);
     //     }
     //     catch (Exception ex)
     //     {
     //         throw new MessageProcessException(typeof(THandler), ex);
     //     }
     // }

       // public void Project(object message)
       // {
       //     var msgType = message.GetType();
       //     foreach(var handler in _handlers[msgType])
       //             handler(message);
       // }

        private readonly List<MessageRoute> _acceptMessages = new List<MessageRoute>();
        public IReadOnlyCollection<MessageRoute> AcceptMessages => _acceptMessages;
    }
}