using System.ComponentModel.DataAnnotations;
using HappyTravel.Edo.Data.Agents;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Management
{
    public readonly struct CounterpartyFullAccessVerificationRequest
    {
        [JsonConstructor]
        public CounterpartyFullAccessVerificationRequest(CounterpartyContractKind contractKind, string reason)
        {
            ContractKind = contractKind;
            Reason = reason;
        }


        /// <summary>
        /// Contract type
        /// </summary>
        [Required]
        public CounterpartyContractKind ContractKind { get; }

        /// <summary>
        /// Verify reason.
        /// </summary>
        [Required]
        public string Reason { get; }
    }
}