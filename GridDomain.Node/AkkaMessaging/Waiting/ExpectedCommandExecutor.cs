using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.CQRS;

namespace GridDomain.Node.AkkaMessaging.Waiting
{
    class ExpectedCommandExecutor: IExpectedCommandExecutor
    {
        private readonly AkkaCommandLocalWaiter _waiter;
        private readonly bool _failOnFaults;

        public ICommandExecutor Executor { get; }

        public ExpectedCommandExecutor(ICommandExecutor executor, 
                                       AkkaCommandLocalWaiter waiter,
                                       bool failOnFaults)
        {
            _failOnFaults = failOnFaults;
            Executor = executor;
            _waiter = waiter;
        }

        public async Task<IWaitResults> Execute(params ICommand[] commands)
        {

            foreach (var command in commands)
                _waiter.ExpectBuilder.Or(MessageMetadataEnvelop.GenericForType(Fault.TypeFor(command)),
                                         f => (((f as IMessageMetadataEnvelop)?.Message as IFault)?.Message as ICommand)?.Id == command.Id);

            var task = _waiter.Start();

            foreach (var command in commands)
                Executor.Execute(command);

            var res = await task;

            if (!_failOnFaults) return res;

            var faults = new List<IFault>();
            foreach (var m in res.All)
            {
                var fault = m as IFault;
                if(fault != null)
                    faults.Add(fault);

                var envelopedFault = m as IMessageMetadataEnvelop<IFault>;
                if (envelopedFault != null)
                    faults.Add(envelopedFault.Message);
            }

            if (faults.Any())
                throw new AggregateException(faults.Select(f => f.Exception));

            return res;
        }

        public async Task<IWaitResults> Execute<T>(T command, IMessageMetadata metadata) where T : ICommand
        {

             _waiter.ExpectBuilder.Or<IFault<T>>(f => f.Message.Id == command.Id);
             _waiter.ExpectBuilder.Or<Fault<T>>(f => f.Message.Id == command.Id);
             _waiter.ExpectBuilder.Or<IMessageMetadataEnvelop<IFault<T>>>(f => f.Message.Message.Id == command.Id);
             _waiter.ExpectBuilder.Or<IMessageMetadataEnvelop<Fault<T>>>(f => f.Message.Message.Id == command.Id);

            var task = _waiter.Start();

            Executor.Execute(command, metadata);

            var res = await task;

            if (!_failOnFaults) return res;
            var faults = res.All.OfType<IMessageMetadataEnvelop<IFault>>().ToArray();
            if (faults.Any())
                throw new AggregateException(faults.Select(f => f.Message.Exception));

            return res;
        }
    }
}