namespace HappyTravel.Edo.Data.Agents
{
    public class ApiClient
    {
        public int Id { get; set; }
        public int AgentId { get; set; }
        public int AgencyId { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; }
    }
}