using GridDomain.CQRS.Messaging.MessageRouting;
using GridDomain.EventSourcing;
using GridDomain.Tests.SampleDomain.Events;
using Microsoft.Practices.Unity;

namespace GridGomain.Tests.Stress
{
    public class SampleAggregateProjectionGroup : ProjectionGroup
    {
        public SampleAggregateProjectionGroup(IUnityContainer locator) : base(locator)
        {
            Add<SampleAggregateCreatedEvent, SampleAggregateProjectionBuilder>(nameof(DomainEvent.SourceId));
            Add<SampleAggregateChangedEvent, SampleAggregateProjectionBuilder>(nameof(DomainEvent.SourceId));
        }
    }
}