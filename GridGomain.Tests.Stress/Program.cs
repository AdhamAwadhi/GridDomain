﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging;
using GridDomain.Node;
using GridDomain.Node.Actors;
using GridDomain.Node.Configuration.Composition;
using GridDomain.Scheduling.Quartz;
using GridDomain.Tests.CommandsExecution;
using GridDomain.Tests.Framework.Configuration;
using GridDomain.Tests.SampleDomain;
using GridDomain.Tests.SampleDomain.Commands;
using GridDomain.Tests.SampleDomain.Events;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity;
using Ploeh.AutoFixture;

namespace GridGomain.Tests.Stress
{
    public class Program
    {
        [HandleProcessCorruptedStateExceptions]
        public static void Main(params string[] args)
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<StressTestContext>());

            RawCommandExecution(1, 10, 50);

            Console.WriteLine("Finished, press any key to continue");
            Console.ReadKey();

            Console.WriteLine("Terminating in 10 seconds");
            Thread.Sleep(10);
        }

        private static void RawCommandExecution(int totalAggregateScenariosCount, int aggregateScenarioPackSize, int aggregateChangeAmount)
        {
            var dbCfg = new AutoTestAkkaConfiguration();

            using (var connection = new SqlConnection(dbCfg.Persistence.JournalConnectionString))
            {
                connection.Open();
                var sqlText = @"TRUNCATE TABLE Journal";
                var cmdJournal = new SqlCommand(sqlText, connection);
                cmdJournal.ExecuteNonQuery();

                var sqlText1 = @"TRUNCATE TABLE Snapshots";
                var cmdSnapshots = new SqlCommand(sqlText, connection);
                cmdSnapshots.ExecuteNonQuery();
            }

            using (var connection = new SqlConnection("Server=(local); Database = StressTest; Integrated Security = true; MultipleActiveResultSets = True"))
            {
                connection.Open();
                var sqlText = @"TRUNCATE TABLE SampleAggregateModels";
                var cmdJournal = new SqlCommand(sqlText, connection);
                cmdJournal.ExecuteNonQuery();
            }

            var cfg = new CustomContainerConfiguration(
                        c => c.Register(new SampleDomainContainerConfiguration()),
                        c => c.RegisterType<SampleAggregateProjectionGroup>(),
                        c => c.RegisterType<SampleAggregateProjectionBuilder>(),
                        c => c.RegisterType<IPersistentChildsRecycleConfiguration, InsertOptimazedBulkConfiguration>(),
                        c => c.RegisterType<IQuartzConfig, PersistedQuartzConfig>()
                );

            Func<ActorSystem[]> actorSystemFactory = () => new[] {new StressTestAkkaConfiguration().CreateSystem()};

            var map = new CustomRouteMap(r => new SampleRouteMap(null).Register(r),
                                         r => r.RegisterProjectionGroup(new SampleAggregateProjectionGroup(null)));

            var node = new GridDomainNode(cfg, map, actorSystemFactory);

            node.Start().Wait();

            var timer = new Stopwatch();
            timer.Start();

            int timeoutedCommads = 0;
            var random = new Random();
            var commandsInScenario = aggregateScenarioPackSize*(aggregateChangeAmount + 1);
            var totalCommandsToIssue = commandsInScenario*totalAggregateScenariosCount;


            for (int i = 0; i < totalAggregateScenariosCount; i ++)
            {
                var packTimer = new Stopwatch();
                packTimer.Start();

                var tasks = Enumerable.Range(0, aggregateScenarioPackSize)
                                      .Select(t => WaitAggregateCommands(aggregateChangeAmount, node))
                                      .ToArray();
                try
                {
                    Task.WaitAll(tasks);
                }
                catch
                {
                    timeoutedCommads += tasks.Count(t => t.IsCanceled || t.IsFaulted);
                }

                packTimer.Stop();
                var speed = (decimal) (commandsInScenario/packTimer.Elapsed.TotalSeconds);
                var timeLeft = TimeSpan.FromSeconds((double) ((totalCommandsToIssue - i*commandsInScenario)/speed));

                Console.WriteLine($"speed: {speed} cmd/sec," +
                                  $"total errors: {timeoutedCommads}, " +
                                  $"total commands executed: {i*commandsInScenario}/{totalCommandsToIssue}," +
                                  $"approx time remaining: {timeLeft}");
            }


            timer.Stop();
            node.Stop().Wait();

            var speedTotal = (decimal) (totalCommandsToIssue/timer.Elapsed.TotalSeconds);
            Console.WriteLine(
                $"Executed {totalAggregateScenariosCount} batches = {totalCommandsToIssue} commands in {timer.Elapsed}");
            Console.WriteLine($"Average speed was {speedTotal} cmd/sec");

            using (var connection = new SqlConnection(dbCfg.Persistence.JournalConnectionString))
            {
                connection.Open();
                var sqlText = @"SELECT COUNT(*) FROM Journal";
                var cmdJournal = new SqlCommand(sqlText, connection);
                var count = (int) cmdJournal.ExecuteScalar();

                Console.WriteLine(count == totalCommandsToIssue
                    ? "Journal contains all events"
                    : $"Journal contains only {count} of {totalCommandsToIssue}");
            }
        }

        private static GridDomainNode StartSampleDomainNode()
        {
            var unityContainer = new UnityContainer();
            unityContainer.Register(new SampleDomainContainerConfiguration());

            var cfg = new CustomContainerConfiguration(
                c => c.Register(new SampleDomainContainerConfiguration()),
                c => c.RegisterType<IPersistentChildsRecycleConfiguration, InsertOptimazedBulkConfiguration>(),
                c => c.RegisterType<IQuartzConfig, PersistedQuartzConfig>());

            Func<ActorSystem[]> actorSystemFactory = () => new[] {new StressTestAkkaConfiguration().CreateSystem()};

            var node = new GridDomainNode(cfg, new SampleRouteMap(unityContainer), actorSystemFactory);

            node.Start().Wait();
            return node;
        }

        private static AutoTestAkkaConfiguration ClearWriteDb()
        {
            var dbCfg = new AutoTestAkkaConfiguration();

            using (var connection = new SqlConnection(dbCfg.Persistence.JournalConnectionString))
            {
                connection.Open();
                var sqlText = @"TRUNCATE TABLE Journal";
                var cmdJournal = new SqlCommand(sqlText, connection);
                cmdJournal.ExecuteNonQuery();

                var sqlText1 = @"TRUNCATE TABLE Snapshots";
                var cmdSnapshots = new SqlCommand(sqlText, connection);
                cmdSnapshots.ExecuteNonQuery();
            }
            return dbCfg;
        }

        private static Task<IWaitResults> WaitAggregateCommands(int changeNumber, GridDomainNode node)
        {
            var commands = new List<ICommand>(changeNumber + 1);
            int value = 0;
            var createCmd = new CreateSampleAggregateCommand(value++, Guid.NewGuid());
            commands.Add(createCmd);

            var changeCmds = Enumerable.Range(0, changeNumber)
                                       .Select(n => new ChangeSampleAggregateCommand(value++, createCmd.AggregateId))
                                       .ToArray();

            commands.AddRange(changeCmds);


            var lastValue = changeNumber.ToString();

            var waitTask = node.NewWaiter()
                               .Expect<SampleAggregateChangedNotification>(n => n.Id == createCmd.AggregateId && n.Value == lastValue)
                               .Create();

            node.Execute(commands.ToArray());

            return waitTask;
        }
    }
}
