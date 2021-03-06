using System;
using System.Linq;
using GridDomain.EventSourcing;
using GridDomain.Node.Configuration.Akka.Hocon;
using GridDomain.Tools.Persistence.SqlPersistence;
using GridDomain.Tools.Repositories.RawDataRepositories;

//using Wire.Extensions;

namespace GridDomain.Tools.Repositories.EventRepositories
{
    public class DomainEventsRepository : IRepository<DomainEvent>
    {
        private readonly IRepository<JournalItem> _rawDataRepo;
        private readonly WireJsonSerializer _serializer = new WireJsonSerializer();

        public void Dispose()
        {
        }

        public DomainEventsRepository(IRepository<JournalItem> rawDataRepo)
        {
            _rawDataRepo = rawDataRepo;
        }

        //Event order matter!!
        public void Save(string id, params DomainEvent[] messages)
        {
            long counter=0;

            var journalEntries = messages.Select(m => new JournalItem(id,
                                                                      ++counter,
                                                                      false,
                                                                      m.GetType().AssemblyQualifiedShortName(),
                                                                      m.CreatedTime,
                                                                      "",
                                                                      _serializer.ToBinary(m))
                                                                     )
                                         .ToArray();

            _rawDataRepo.Save(id, journalEntries);
        }

        public DomainEvent[] Load(string id)
        {
            return
                _rawDataRepo.Load(id)
                            .Select(d =>
                            {
                                try
                                {
                                    return _serializer.FromBinary(d.Payload,Type.GetType(d.Manifest));
                                }
                                catch (NullReferenceException ex)
                                {
                                    throw new PersistanceFailureException(d, ex);
                                }
                            })
                            .Cast<DomainEvent>()
                            .ToArray();
        }

        public static DomainEventsRepository New(string connectionString)
        {
            return new DomainEventsRepository(new RawJournalRepository(connectionString));
        }
    }
}