using GridDomain.Node.Configuration;

namespace GridDomain.Tests.Acceptance
{
    public class AutoTestAkkaConfiguration : AkkaConfiguration
    {
        public AutoTestAkkaConfiguration(bool writeConfig = false,LogVerbosity verbosity = LogVerbosity.Warning)
            : base(new AutoTestAkkaNetworkAddress(),
                    new AutoTestAkkaDbConfiguration(),
                verbosity,
                writeConfig)
        {
        }
    }
}