using HappyTravel.Edo.Api.Models.Users;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Agents
{
    public readonly struct RegisterAgentWithCounterpartyRequest
    {
        [JsonConstructor]
        public RegisterAgentWithCounterpartyRequest(UserDescriptionInfo agent, CounterpartyCreateRequest counterparty)
        {
            Agent = agent;
            Counterparty = counterparty;
        }


        /// <summary>
        ///     Agent personal information.
        /// </summary>
        public UserDescriptionInfo Agent { get; }

        /// <summary>
        ///     Agent affiliated counterparty information.
        /// </summary>
        public CounterpartyCreateRequest Counterparty { get; }
    }
}