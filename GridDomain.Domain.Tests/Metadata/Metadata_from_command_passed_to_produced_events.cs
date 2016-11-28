﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.EventSourcing.Sagas.FutureEvents;
using GridDomain.Node.AkkaMessaging.Waiting;
using GridDomain.Tests.CommandsExecution;
using GridDomain.Tests.Sagas.InstanceSagas;
using GridDomain.Tests.SampleDomain;
using GridDomain.Tests.SampleDomain.Commands;
using GridDomain.Tests.SampleDomain.Events;
using NUnit.Framework;

namespace GridDomain.Tests.Metadata
{
    [TestFixture]
    class Metadata_from_aggregate_command_passed_to_produced_events : SampleDomainCommandExecutionTests
    {
        private IMessageMetadataEnvelop<SampleAggregateCreatedEvent> _answer;
        private CreateSampleAggregateCommand _command;
        private MessageMetadata _commandMetadata;

        [OneTimeSetUp]
        public void When_execute_aggregate_command_with_metadata()
        {
            _command = new CreateSampleAggregateCommand(1,Guid.NewGuid());
            _commandMetadata = new MessageMetadata(_command.Id, Guid.NewGuid());

            var res = GridNode.NewCommandWaiter()
                           .Expect<IMessageMetadataEnvelop<SampleAggregateCreatedEvent>>()
                           .Create()
                           .Execute(_command, _commandMetadata)
                           .Result;

            _answer = res.Message<IMessageMetadataEnvelop<SampleAggregateCreatedEvent>>();
        }

        [Test]
        public void Result_contains_metadata()
        {
            Assert.NotNull(_answer.Metadata);
        }

        [Test]
        public void Result_contains_message()
        {
            Assert.NotNull(_answer.Message);
        }

        [Test]
        public void Result_message_has_expected_type()
        {
            Assert.AreEqual(typeof(SampleAggregateCreatedEvent),_answer.Message);
        }

        [Test]
        public void Result_message_has_expected_id()
        {
            Assert.AreEqual(_command.AggregateId, _answer.Message.SourceId);
        }

        [Test]
        public void Result_message_has_expected_value()
        {
            Assert.AreEqual(_command.Parameter.ToString(), _answer.Message.Value);
        }
        [Test]
        public void Result_metadata_has_message_id_same_as_produced_event_id()
        {
            Assert.AreEqual(_answer.Message.SourceId, _answer.Metadata.MessageId);
        }

        [Test]
        public void Result_metadata_has_command_id_as_casuation_id()
        {
            Assert.AreEqual(_command.Id, _answer.Metadata.CasuationId);
        }


        [Test]
        public void Result_metadata_has_correlation_id_same_as_command_metadata()
        {
            Assert.AreEqual(_commandMetadata.CorrelationId, _answer.Metadata.CorrelationId);
        }

        [Test]
        public void Result_metadata_has_processed_history_filled_from_aggregate()
        {
            Assert.AreEqual(1, _answer.Metadata.History?.Steps.Count);
        }

        [Test]
        public void Result_metadata_has_processed_correct_filled_history_step()
        {
            var step = _answer.Metadata.History.Steps.First();
            Assert.AreEqual(typeof(SampleAggregate).Name,step.Who);
            Assert.AreEqual(ProcessReasons.AggregateCommandExecution,step.What);
        }

    }

    [TestFixture]
    class Metadata_from_saga_received_event_passed_to_produced_commands : SoftwareProgrammingInstanceSagaTest
    {

    }
  
}
