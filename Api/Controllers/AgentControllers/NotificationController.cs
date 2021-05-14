﻿using HappyTravel.Edo.Api.Filters.Authorization.AgentExistingFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.NotificationCenter.Models;
using HappyTravel.Edo.Api.NotificationCenter.Services;
using HappyTravel.Edo.Api.Services.Agents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace HappyTravel.Edo.Api.Controllers.AgentControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/notifications")]
    [Produces("application/json")]
    public class NotificationController : BaseController
    {
        public NotificationController(IAgentContextService agentContextService, INotificationService notificationService)
        {
            _agentContextService = agentContextService;
            _notificationService = notificationService;
        }


        /// <summary>
        ///     Gets the notification history of the current agent
        /// </summary>
        /// <param name="skip">Skip</param>
        /// <param name="top">Top</param>
        /// <returns>List of notifications</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<SlimNotification>), (int)HttpStatusCode.OK)]
        [AgentRequired]
        public async Task<IActionResult> GetNotifications([FromQuery] int skip = 0, [FromQuery] int top = 1000)
        {
            var agent = await _agentContextService.GetAgent();

            return Ok(await _notificationService.Get(new Models.Agents.SlimAgentContext(agent.AgentId, agent.AgencyId), skip, top));
        }


        private readonly IAgentContextService _agentContextService;
        private readonly INotificationService _notificationService;
    }
}
