using GridDomain.Common;
using GridDomain.Node.Configuration.Composition;
using Microsoft.Practices.Unity;

namespace GridDomain.Tests.Unit.Sagas.InstanceSagas
{
    public class SoftwareProgrammingSagaContainerConfiguration : IContainerConfiguration
    {
        private readonly IContainerConfiguration _sagaConfiguration = SagaConfiguration.Instance<SoftwareProgrammingSaga,
            SoftwareProgrammingSagaData>(
                new SoftwareProgrammingSagaFactory(),
                SoftwareProgrammingSaga.Descriptor);
        public void Register(IUnityContainer container)
        {
            _sagaConfiguration.Register(container);
        }
    }
}