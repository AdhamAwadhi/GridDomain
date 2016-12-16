using System;
using System.ComponentModel.DataAnnotations;

namespace GridGomain.Tests.Stress
{
    public class SampleAggregateModel
    {
        [Key]
        public Guid Id { get; set; }
        public string Value { get; set; }
    }
}