using System;
using System.Linq;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Adapters;
using GridDomain.Node;
using GridDomain.Tests.Acceptance.EventsUpgrade.SampleDomain;
using GridDomain.Tests.Unit.CommandsExecution;
using NUnit.Framework;

namespace GridDomain.Tests.Acceptance.EventsUpgrade
{
    [TestFixture]
    public class GridNode_should_load_events_stored_in_legacy_wire_format : SampleDomainCommandExecutionTests
    {
        protected override bool ClearDataOnStart { get; } = true;

        public GridNode_should_load_events_stored_in_legacy_wire_format():base(false)
        {
        }

        protected override void OnNodeCreated()
        {
             GridNode.EventsAdaptersCatalog.Register(new BookOrderAdapter());
        }

        [Test]
        public void When_wire_stored_events_loaded_and_saved_back()
        {
            var orderA = new BookOrder_V1("A");
            var orderB = new BookOrder_V1("B");
            var id = Guid.NewGuid();

            var events = new DomainEvent[]
            {
                new EventA(id, orderA),
                new EventB(id, orderB)
            };

            var persistenceId = "testId";
            GridNode_should_convert_and_upgrade_events_stored_in_legacy_wire_format.SaveWithLegacyWire(persistenceId, events);

            var loadedEvents = LoadFromJournal(persistenceId, 2).ToArray();

            Assert.NotNull(loadedEvents);
        }
    }
}