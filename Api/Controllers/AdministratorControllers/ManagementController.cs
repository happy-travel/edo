using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Management;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Services.Documents;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Common.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin/management")]
    [Produces("application/json")]
    public class ManagementController : BaseController
    {
        public ManagementController(IAdministratorInvitationService invitationService,
            IAdministratorRegistrationService registrationService,
            ITokenInfoAccessor tokenInfoAccessor,
            IDirectConnectivityReportService directConnectivity)
        {
            _invitationService = invitationService;
            _registrationService = registrationService;
            _tokenInfoAccessor = tokenInfoAccessor;
            _directConnectivity = directConnectivity;
        }


        /// <summary>
        ///     Send invitation to administrator.
        /// </summary>
        /// <param name="invitationInfo">Administrator invitation info.</param>
        /// <returns></returns>
        [HttpPost("invite")]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [AdministratorPermissions(AdministratorPermissions.AdministratorInvitation)]
        public async Task<IActionResult> InviteAdministrator([FromBody] AdministratorInvitationInfo invitationInfo)
        {
            var (_, isFailure, error) = await _invitationService.SendInvitation(invitationInfo);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Register current user as administrator by invitation code.
        /// </summary>
        /// <param name="invitationCode">Invitation code.</param>
        /// <returns></returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        public async Task<IActionResult> RegisterAdministrator([FromBody] string invitationCode)
        {
            var identity = _tokenInfoAccessor.GetIdentity();
            if (string.IsNullOrWhiteSpace(identity))
                return BadRequest(ProblemDetailsBuilder.Build("Could not get user's identity"));

            var (_, isFailure, error) = await _registrationService.RegisterByInvitation(invitationCode, identity);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }
        
        
        /// <summary>
        ///     Disable invitation.
        /// </summary>
        /// <param name="code">Invitation code.</param>
        [HttpPost("invitations/{code}/disable")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> DisableInvitation(string code)
        {
            var (_, isFailure, error) = await _invitationService.Disable(code);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok();
        }


        /// <summary>
        ///     Returns supplier wise direct connectivity report
        /// </summary>
        [HttpGet("reports/direct-connectivity-report/supplier-wise")]
        [ProducesResponseType(typeof(FileStream), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.DirectConnectivityReport)]
        public async Task<IActionResult> GetSupplerWiseDirectConnectivityReport(Suppliers supplier, DateTime from, DateTime end)
        {
            var (_, isFailure, stream, error) = await _directConnectivity.GetSupplierWiseReport(supplier, from, end);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return new FileStreamResult(stream, new MediaTypeHeaderValue("text/csv"))
            {
                FileDownloadName = $"supplier-wise-report.csv-{from:g}-{end:g}.csv"
            };
        }
        
        
        private readonly IAdministratorInvitationService _invitationService;
        private readonly IAdministratorRegistrationService _registrationService;
        private readonly ITokenInfoAccessor _tokenInfoAccessor;
        private readonly IDirectConnectivityReportService _directConnectivity;
    }
}