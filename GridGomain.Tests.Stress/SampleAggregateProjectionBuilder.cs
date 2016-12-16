using System;
using GridDomain.CQRS.Messaging;
using GridDomain.Tests.SampleDomain.Events;

namespace GridGomain.Tests.Stress
{
    public class SampleAggregateProjectionBuilder : IEventHandler<SampleAggregateCreatedEvent>,
                                                    IEventHandler<SampleAggregateChangedEvent>
    {
        private IPublisher _publisher;
        private Func<StressTestContext> _contextFactory;

        public SampleAggregateProjectionBuilder(Func<StressTestContext> apiContextFactory, IPublisher publisher)
        {
            _publisher = publisher;
            _contextFactory = apiContextFactory;
        }


        public void Handle(SampleAggregateCreatedEvent message)
        {
            using (var ctx = _contextFactory())
            {
                ctx.SampleAgregate.Add(new SampleAggregateModel() {Id = message.SourceId, Value = message.Value});
                ctx.SaveChanges();
            }
            _publisher.Publish(new SampleAggregateCreatedNotification(message.SourceId, message.Value));
        }

        public void Handle(SampleAggregateChangedEvent message)
        {
            using (var ctx = _contextFactory())
            {
                var aggregate = ctx.SampleAgregate.Find(message.SourceId);
                aggregate.Value = message.Value;
                ctx.SaveChanges();
            }
            _publisher.Publish(new SampleAggregateChangedNotification(message.SourceId, message.Value));

        }
    }
}