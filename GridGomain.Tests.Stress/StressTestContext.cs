using System.Data.Entity;

namespace GridGomain.Tests.Stress
{
    public class StressTestContext : DbContext
    {
        public StressTestContext():base("Server = (local); Database = StressTest; Integrated Security = true; MultipleActiveResultSets = True")
        {
            
        }
        public DbSet<SampleAggregateModel> SampleAgregate { get; set; }
    }
}