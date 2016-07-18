using System;
using GridDomain.EventSourcing.Sagas;
using Microsoft.Practices.Unity;

namespace GridDomain.Scheduling.Integration
{
    public class ScheduledCommandProcessingSagaRegistrator
    {
        public static void Register(IUnityContainer container)
        {
            container.RegisterType<ISagaFactory<ScheduledCommandProcessingSaga, ScheduledCommandProcessingSagaState>, ScheduledCommandProcessingSagaFactory>();
            container.RegisterType<ISagaFactory<ScheduledCommandProcessingSaga, ScheduledCommandProcessingStarted>, ScheduledCommandProcessingSagaFactory>();
            container.RegisterType<ISagaFactory<ScheduledCommandProcessingSaga,Guid>, ScheduledCommandProcessingSagaFactory>();
        }
    }
}