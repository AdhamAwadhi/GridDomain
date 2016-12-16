using System;
using System.Collections.Generic;

namespace GridDomain.CQRS.Messaging.MessageRouting
{
    public interface IProjectionGroupDescriptor
    {
        IReadOnlyCollection<MessageRoute> AcceptMessages { get; }
        IDictionary<Type, List<Type>> HandlersMap { get; }
    }
}