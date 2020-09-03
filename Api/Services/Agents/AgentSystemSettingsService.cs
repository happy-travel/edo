using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public class AgentSystemSettingsService : IAgentSystemSettingsService
    {
        public AgentSystemSettingsService(EdoContext context)
        {
            _context = context;
        }
        
        public async Task<AvailabilitySearchSettings?> GetAvailabilitySearchSettings(AgentContext agent)
        {
            var settings = await _context
                .AgentSystemSettings
                .SingleOrDefaultAsync(s => s.AgentId == agent.AgentId && s.AgencyId == agent.AgencyId);

            return settings?.AvailabilitySearchSettings;
        }
        
        private readonly EdoContext _context;
    }
}