namespace GridDomain.Node.Actors
{
    class CheckHealth
    {
        public string Payload {get;}

        public CheckHealth(string payload = null)
        {
            this.Payload = payload;
        }

        public static CheckHealth Instance = new CheckHealth();
    }
}