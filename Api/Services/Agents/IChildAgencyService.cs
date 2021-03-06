﻿using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agencies;
using HappyTravel.Edo.Api.Models.Agents;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public interface IChildAgencyService
    {
        Task<Result<ChildAgencyInfo>> Get(int agencyId, AgentContext agent);

        Task<List<SlimChildAgencyInfo>> Get(AgentContext agent);
    }
}
