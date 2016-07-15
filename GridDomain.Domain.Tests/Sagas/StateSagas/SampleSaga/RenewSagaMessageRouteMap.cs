using GridDomain.CQRS.Messaging;
using GridDomain.CQRS.Messaging.MessageRouting;

namespace GridDomain.Tests.Sagas.StateSagas.SampleSaga
{
    public class RenewSagaMessageRouteMap : IMessageRouteMap
    {
        public void Register(IMessagesRouter router)
        {
            router.RegisterSaga(SoftwareProgrammingSaga.Descriptor);
        }
    }
}