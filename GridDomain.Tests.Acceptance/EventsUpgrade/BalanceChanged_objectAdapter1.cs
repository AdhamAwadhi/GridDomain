using GridDomain.EventSourcing.Adapters;
using GridDomain.Tests.Unit.EventsUpgrade.Domain.Events;

namespace GridDomain.Tests.Acceptance.EventsUpgrade
{
    class BalanceChanged_objectAdapter1 : ObjectAdapter<BalanceChangedEvent_V0, BalanceChangedEvent_V1>
    {
        public override BalanceChangedEvent_V1 Convert(BalanceChangedEvent_V0 evt)
        {
            return new BalanceChangedEvent_V1(evt.AmplifiedAmountChange, evt.SourceId);
        }
    }
}