using System;

namespace GridGomain.Tests.Stress
{
    public class SampleAggregateCreatedNotification
    {
        public Guid Id { get; }
        public string Value { get; set; }

        public SampleAggregateCreatedNotification(Guid id, string value)
        {
            Id = id;
            Value = value;
        }
    }
}