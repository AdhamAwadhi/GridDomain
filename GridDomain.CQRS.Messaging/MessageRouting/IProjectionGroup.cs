namespace GridDomain.CQRS.Messaging.MessageRouting
{
    public interface IProjectionGroup
    {
        void Project(object message);
    }

}