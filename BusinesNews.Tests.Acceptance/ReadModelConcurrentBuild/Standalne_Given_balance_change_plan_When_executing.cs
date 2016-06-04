using GridDomain.Balance.Node;
using GridDomain.Node;
using GridDomain.Node.Configuration;

namespace GridDomain.Tests.Acceptance.Balance.ReadModelConcurrentBuild
{
    public class Standalne_Given_balance_change_plan_When_executing : Given_balance_change_plan_When_executing
    {
        protected override AkkaConfiguration AkkaConf => new AutoTestAkkaConfiguration(true);

        protected override GridDomainNode GreateGridDomainNode(AkkaConfiguration akkaConf, IDbConfiguration dbConfig)
        {
            return new GridDomainNode(CreateUnityContainer(dbConfig),
                new BalanceCommandsRouting(),
                TransportMode.Standalone, ActorSystemFactory.CreateActorSystem(akkaConf));
        }

        public Standalne_Given_balance_change_plan_When_executing() : base(
            new AutoTestAkkaConfiguration().ToStandAloneSystemConfig())
        {
        }

        protected override int BusinessNum => 10;
        protected override int ChangesPerBusiness => 10;
    }
}