using System;
using GridDomain.CQRS;

namespace GridDomain.Tests.SyncProjection.SampleDomain
{
    public class ChangeAggregateCommand : Command
    {
        public ChangeAggregateCommand(int parameter, Guid aggregateId)
        {
            Parameter = parameter;
            AggregateId = aggregateId;
        }

        public Guid AggregateId { get; }
        public int Parameter { get; }
    }

    public class LongOperationCommand : Command
    {
        public LongOperationCommand(int parameter, Guid aggregateId)
        {
            Parameter = parameter;
            AggregateId = aggregateId;
        }

        public Guid AggregateId { get; }
        public int Parameter { get; }
    }
}