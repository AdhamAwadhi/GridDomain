﻿using System;
using System.Collections.Generic;
using System.Linq;
using Akka;
using Akka.Actor;
using Akka.Persistence;
using Automatonymous;
using CommonDomain.Core;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Sagas;
using GridDomain.EventSourcing.Sagas.InstanceSagas;
using GridDomain.Node.AkkaMessaging;

namespace GridDomain.Node.Actors
{
    /// <summary>
    ///     Name should be parse by AggregateActorName
    /// </summary>
    /// <typeparam name="TSaga"></typeparam>
    /// <typeparam name="TSagaState"></typeparam>
    /// <typeparam name="TStartMessage"></typeparam>
    public class SagaActor<TSaga, TSagaState, TStartMessage> :
        ReceivePersistentActor where TSaga : ISagaInstance
        where TSagaState : AggregateBase
        where TStartMessage : DomainEvent
    {
        private readonly IPublisher _publisher;
        private readonly ISagaFactory<TSaga, TStartMessage> _sagaStarter;
        public TSaga Saga;
        private readonly ISagaFactory<TSaga, TSagaState> _sagaFactory;

        public SagaActor(ISagaFactory<TSaga, TStartMessage> sagaStarter,
                         ISagaFactory<TSaga, TSagaState> sagaFactory,
                         ISagaFactory<TSaga, Guid> emptySagaFactory,
                         IPublisher publisher)
        {
            _sagaStarter = sagaStarter;
            _sagaFactory = sagaFactory;
            _publisher = publisher;

            var id = AggregateActorName.Parse<TSagaState>(PersistenceId).Id;
            Saga = emptySagaFactory.Create(id);

            Command<ICommandFault>(ProcessSaga, fault => fault.SagaId == Saga.Data.Id);

            Command<DomainEvent>(msg =>
            {
               msg.Match()
                  .With<TStartMessage>(start =>
                                  { Saga = _sagaStarter.Create(start); });

                ProcessSaga(msg);

            }, e => e.SagaId == Saga.Data.Id);

            //recover messages will be provided only to right saga by using peristenceId
            Recover<SnapshotOffer>(offer => Saga = _sagaFactory.Create((TSagaState)offer.Snapshot));
            Recover<DomainEvent>(e => Saga.Data.ApplyEvent(e));
        }


        private void ProcessSaga(object message)
        {
            Saga.Transit(message);

            var stateChangeEvents = Saga.Data.GetUncommittedEvents().Cast<object>();
            PersistAll(stateChangeEvents, e =>
            {
                _publisher.Publish(e);
            });

            foreach (var msg in Saga.CommandsToDispatch)
                _publisher.Publish(msg);

            Saga.ClearCommandsToDispatch();
            Saga.Data.ClearUncommittedEvents();
        }

        public override string PersistenceId => Self.Path.Name;
    }
}