﻿using System.ComponentModel.DataAnnotations;
using HappyTravel.Edo.Api.Models.Agencies;
using Newtonsoft.Json;


namespace HappyTravel.Edo.Api.Models.Agents
{
    public readonly struct CounterpartyCreateRequest
    {
        [JsonConstructor]
        public CounterpartyCreateRequest(CounterpartyInfo counterpartyInfo, AgencyInfo rootAgencyInfo)
        {
            CounterpartyInfo = counterpartyInfo;
            RootAgencyInfo = rootAgencyInfo;
        }


        /// <summary>
        /// Information to create a new counterparty.
        /// </summary>
        [Required]
        public CounterpartyInfo CounterpartyInfo { get; }


        /// <summary>
        /// Information to create a root agency for newly created counterparty.
        /// </summary>
        [Required]
        public AgencyInfo RootAgencyInfo { get; }
    }
}
