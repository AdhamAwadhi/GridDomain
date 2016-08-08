using GridDomain.Tools;

namespace GridDomain.Tests.Tools
{
    public static class TestRepository
    {
        public static IRepository NewInMemory()
        {
            return new Repository(TestEventRepository.NewInMemory());
        }

        public static IRepository NewPersistent()
        {
            return new Repository(TestEventRepository.NewPersistent());
        }
    }
}