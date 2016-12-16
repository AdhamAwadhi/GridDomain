using System;

namespace GridGomain.Tests.Stress
{
    public class SampleAggregateChangedNotification
    {
        public Guid Id { get; }
        public string Value { get; set; }

        public SampleAggregateChangedNotification(Guid id, string value)
        {
            Id = id;
            Value = value;
        }
    }
}