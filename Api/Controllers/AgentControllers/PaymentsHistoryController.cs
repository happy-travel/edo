﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.AgentExistingFilters;
using HappyTravel.Edo.Api.Filters.Authorization.CounterpartyStatesFilters;
using HappyTravel.Edo.Api.Filters.Authorization.InAgencyPermissionFilters;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Common.Enums;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AgentControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/payments")]
    [Produces("application/json")]
    public class PaymentsHistoryController : ControllerBase
    {
        public PaymentsHistoryController(IPaymentHistoryService paymentHistoryService, IAgentContextService agentContextService)
        {
            _paymentHistoryService = paymentHistoryService;
            _agentContextService = agentContextService;
        }


        /// <summary>
        ///     Gets payment history for current agent.
        /// </summary>
        /// <param name="historyRequest"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(List<PaymentHistoryData>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AgentRequired]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [HttpPost("history/agent")]
        [EnableQuery]
        public async Task<IActionResult> GetAgentHistory([FromBody] PaymentHistoryRequest historyRequest)
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, response, error) = _paymentHistoryService.GetAgentHistory(historyRequest, agent);
            if (isFailure)
                return BadRequest(error);

            return Ok(response);
        }


        /// <summary>
        ///     Gets payment history for an agency.
        /// </summary>
        /// <param name="historyRequest"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(List<PaymentHistoryData>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [HttpPost("history/agency")]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions(InAgencyPermissions.ObservePaymentHistory)]
        public async Task<IActionResult> GetAgencyHistory([FromBody] PaymentHistoryRequest historyRequest)
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, response, error) = _paymentHistoryService.GetAgencyHistory(historyRequest, agent);
            if (isFailure)
                return BadRequest(error);

            return Ok(response);
        }


        private readonly IPaymentHistoryService _paymentHistoryService;
        private readonly IAgentContextService _agentContextService;
    }
}