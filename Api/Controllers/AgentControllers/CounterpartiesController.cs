using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.InAgencyPermissionFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Files;
using HappyTravel.Edo.Common.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AgentControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/counterparty")]
    [Produces("application/json")]
    public class CounterpartiesController : BaseController
    {
        public CounterpartiesController(ICounterpartyService counterpartyService,
            IAgentContextService agentContextService,
            IContractFileService contractFileService)
        {
            _counterpartyService = counterpartyService;
            _agentContextService = agentContextService;
            _contractFileService = contractFileService;
        }


        ///// <summary>
        /////     Creates agency for counterparty.
        ///// </summary>
        ///// <param name="counterpartyId">Counterparty Id.</param>
        ///// <param name="agencyInfo">Agency information.</param>
        ///// <returns></returns>
        //[HttpPost("{counterpartyId}/agencies")]
        //[ProducesResponseType((int) HttpStatusCode.NoContent)]
        //[ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        //[AgentRequired]
        //public async Task<IActionResult> AddAgency(int counterpartyId, [FromBody] AgencyInfo agencyInfo)
        //{
        //    var (isSuccess, _, _, error) = await _counterpartyService.AddAgency(counterpartyId, agencyInfo);

        //    return isSuccess
        //        ? (IActionResult) NoContent()
        //        : BadRequest(ProblemDetailsBuilder.Build(error));
        //}


        /// <summary>
        ///     Gets counterparty information.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(CounterpartyInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetCounterparty()
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, counterpartyInfo, error) = await _counterpartyService.Get(agent.CounterpartyId);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(counterpartyInfo);
        }


        /// <summary>
        /// Downloads a contract pdf file of the counterparty agent is currently using.
        /// </summary>
        [HttpGet("contract-file")]
        [ProducesResponseType(typeof(FileStreamResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.ObserveCounterpartyContract)]
        public async Task<IActionResult> GetContractFile()
        {
            var agent = await _agentContextService.GetAgent();

            var (_, isFailure, (stream, contentType), error) = await _contractFileService.Get(agent);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return File(stream, contentType);
        }


        private readonly ICounterpartyService _counterpartyService;
        private readonly IAgentContextService _agentContextService;
        private readonly IContractFileService _contractFileService;
    }
}