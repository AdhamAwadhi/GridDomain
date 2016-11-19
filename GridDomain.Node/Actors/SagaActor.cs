﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Akka;
using Akka.Actor;
using Akka.Monitoring;
using Akka.Monitoring.Impl;
using Akka.Persistence;
using Automatonymous;
using CommonDomain;
using CommonDomain.Core;
using CommonDomain.Persistence;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Sagas;
using GridDomain.EventSourcing.Sagas.InstanceSagas;
using GridDomain.EventSourcing.Sagas.StateSagas;
using GridDomain.Logging;
using GridDomain.Node.AkkaMessaging;

namespace GridDomain.Node.Actors
{
    //TODO: add status info, e.g. was any errors during execution or recover

    /// <summary>
    ///     Name should be parse by AggregateActorName
    /// </summary>
    /// <typeparam name="TSaga"></typeparam>
    /// <typeparam name="TSagaState"></typeparam>
    public class SagaActor<TSaga, TSagaState> :
        ReceivePersistentActor where TSaga : class,ISagaInstance 
        where TSagaState : AggregateBase
    {
        private readonly ISagaProducer<TSaga> _producer;
        private readonly IPublisher _publisher;
        private TSaga _saga;
        private IAggregate _sagaData;
        private readonly ISoloLogger _log = LogManager.GetLogger();
        public readonly Guid Id;
        public TSaga Saga => _saga ?? (_saga = _producer.Create(_sagaData));
     //   private readonly AggregateFactory _aggregateFactory = new AggregateFactory();
        private readonly HashSet<Type> _sagaStartMessageTypes;
        private readonly List<IActorRef> _persistenceWaiters = new List<IActorRef>();
        private readonly SnapshotsSavePolicy _snapshotsPolicy;
      //  private readonly List<IActorRef> _persistenceWaiters = new List<IActorRef>();




        private Guid GetSagaId(DomainEvent msg)
        {
            var type = msg.GetType();
            string fieldName;
         
            if (_sagaIdFields.TryGetValue(type, out fieldName))
                return (Guid) type.GetProperty(fieldName).GetValue(msg);

            return msg.SagaId;
        }

        public SagaActor(ISagaProducer<TSaga> producer,
                         IPublisher publisher,
                         SnapshotsSavePolicy snapshotsSavePolicy,
                         IConstructAggregates aggregatesConstructor)
        {
            _aggregatesConstructor = aggregatesConstructor;
            _producer = producer;
            _publisher = publisher;
            _sagaStartMessageTypes = new HashSet<Type>(producer.KnownDataTypes.Where(t => typeof(DomainEvent).IsAssignableFrom(t)));
            _sagaIdFields = producer.Descriptor.AcceptMessages.ToDictionary(m => m.MessageType, m => m.CorrelationField);
            _snapshotsPolicy = snapshotsSavePolicy;
            //id from name is used due to saga.Data can be not initialized before messages not belonging to current saga will be received
            Id = AggregateActorName.Parse<TSagaState>(PersistenceId).Id;
            _sagaData = aggregatesConstructor.Build(typeof(TSagaState), Id, null);

            Command<GracefullShutdownRequest>(req =>
            {
                _monitor.IncrementMessagesReceived();
                Shutdown();
            });

            Command<CheckHealth>(s => Sender.Tell(new HealthStatus(s.Payload), Self));

            Command<NotifyOnPersistenceEvents>(c =>
            {
                var waiter = c.Waiter ?? Sender;
                if (IsRecoveryFinished)
                {
                    waiter.Tell(RecoveryCompleted.Instance,Self);
                }
                else _persistenceWaiters.Add(waiter);
            });

            Command<DomainEvent>(msg =>
            {
                _monitor.IncrementMessagesReceived();
                if (_sagaStartMessageTypes.Contains(msg.GetType()))
                    _saga = _producer.Create(msg);
                ProcessSaga(msg);
            }, e => GetSagaId(e) == Id);

            Command<IFault>(fault =>
            {
                _monitor.IncrementMessagesReceived();
                ProcessSaga(fault);
            }, fault => fault.SagaId == Id);


            //recover messages will be provided only to right saga by using peristenceId
            Recover<SnapshotOffer>(offer =>
            {
                _sagaData = _aggregatesConstructor.Build(typeof(TSagaState),Id,(IMemento)offer.Snapshot);
            });
            Recover<DomainEvent>(e =>
            {
                _sagaData.ApplyEvent(e);
                _snapshotsPolicy.RefreshActivity(e.CreatedTime);
            });
            Recover<RecoveryCompleted>(message =>
            {
                Log.Debug("Recovery for actor {Id} is completed", PersistenceId);
                NotifyWatchers(message);
                //_persistenceWaiters.Clear();
            });
            
        }

        void Terminating()
        {
            Command<DeleteSnapshotsSuccess>(s =>
            {
                NotifyWatchers(s);
                Context.Stop(Self);
            });
            Command<DeleteSnapshotsFailure>(s =>
            {
                NotifyWatchers(s);
                Context.Stop(Self);
            });
        }

        private void NotifyWatchers(object msg)
        {
            foreach (var watcher in _persistenceWaiters)
                watcher.Tell(msg);
        }

        protected override void OnPersistFailure(Exception cause, object @event, long sequenceNr)
        {
            Log.Error("Additional persistence diagnostics on failure {error} {actor} {event}", cause, Self.Path.Name, @event);
            base.OnPersistFailure(cause, @event, sequenceNr);
        }

        protected override void OnPersistRejected(Exception cause, object @event, long sequenceNr)
        {
            Log.Error("Additional persistence diagnostics on rejected {error} {actor} {event}", cause, Self.Path.Name, @event);
            base.OnPersistRejected(cause, @event, sequenceNr);
        }


        protected virtual void Shutdown()
        {
            //TODO: raise faults for all messages in stash
            DeleteSnapshots(_snapshotsPolicy.SnapshotsToDelete());
            Become(Terminating);
        }

        private void ProcessSaga(object message)
        {
            try
            {
                Saga.Transit(message);
            }
            catch (Exception ex)
            {
                var processorType = _producer.Descriptor.StateMachineType;

                _log.Error(ex,"Saga {saga} {id} raised an error on {@message}", processorType, Id, message);
                var fault = Fault.NewGeneric(message, ex, processorType, Id);
                _publisher.Publish(fault);
            }

            var stateChange = ProcessSagaStateChange();

            ProcessSagaCommands();

            if(_snapshotsPolicy.ShouldSave(stateChange))
                SaveSnapshot(Saga.Data.GetSnapshot());
        }
        

        private void ProcessSagaCommands()
        {
            foreach (var msg in Saga.CommandsToDispatch)
                 _publisher.Publish(msg);

            Saga.ClearCommandsToDispatch();
        }

        private object[] ProcessSagaStateChange()
        {
            var stateChangeEvents = Saga.Data.GetUncommittedEvents().Cast<object>().ToArray();
            PersistAll(stateChangeEvents, e => _publisher.Publish(e));
            Saga.Data.ClearUncommittedEvents();
            return stateChangeEvents;
        }

        private readonly ActorMonitor _monitor = new ActorMonitor(Context, typeof(TSaga).Name);
        private readonly Dictionary<Type, string> _sagaIdFields;
        private readonly IConstructAggregates _aggregatesConstructor;

        protected override void PreStart()
        {
            _monitor.IncrementActorStarted();
        }

        protected override void PostStop()
        {
            _monitor.IncrementActorStopped();
        }
        protected override void PreRestart(Exception reason, object message)
        {
            _monitor.IncrementActorRestarted();
        }

        public override string PersistenceId => Self.Path.Name;
    }
}