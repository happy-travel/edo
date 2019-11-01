using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.External;
using HappyTravel.Edo.Api.Services.PaymentLinks;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Common.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/external/payment-links")]
    [Produces("application/json")]
    public class PaymentLinksController : BaseController
    {
        public PaymentLinksController(IPaymentLinkService paymentLinkService,
            IPaymentLinksProcessingService paymentLinksProcessingService,
            IPayfortSignatureService signatureService)
        {
            _paymentLinkService = paymentLinkService;
            _paymentLinksProcessingService = paymentLinksProcessingService;
            _signatureService = signatureService;
        }


        /// <summary>
        ///     Gets supported desktop client versions.
        /// </summary>
        /// <returns>List of supported versions.</returns>
        [HttpGet("versions")]
        [ProducesResponseType(typeof(List<Version>), (int) HttpStatusCode.OK)]
        public IActionResult GetSupportedDesktopAppVersion() => Ok(_paymentLinkService.GetSupportedVersions());


        /// <summary>
        ///     Gets settings for payment links.
        /// </summary>
        /// <returns>Payment link settings.</returns>
        [HttpGet("settings")]
        [ProducesResponseType(typeof(ClientSettings), (int) HttpStatusCode.OK)]
        public IActionResult GetSettings() => Ok(_paymentLinkService.GetClientSettings());


        /// <summary>
        ///     Sends payment link to specified e-mail address.
        /// </summary>
        /// <param name="request">Payment link data</param>
        /// <returns></returns>
        [HttpPost("send")]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        public async Task<IActionResult> SendLink([FromBody] PaymentLinkData request)
        {
            var (isSuccess, _, error) = await _paymentLinkService.Send(request);
            return isSuccess
                ? NoContent()
                : (IActionResult) BadRequest(ProblemDetailsBuilder.Build(error));
        }
        
        /// <summary>
        ///     Generates payment link.
        /// </summary>
        /// <param name="request">Payment link data</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GenerateUrl([FromBody] PaymentLinkData request)
        {
            var (isSuccess, _, uri, error) = await _paymentLinkService.GenerateUri(request);
            return isSuccess
                ? Ok(uri)
                : (IActionResult) BadRequest(ProblemDetailsBuilder.Build(error));
        }


        [HttpGet("{code}")]
        [AllowAnonymous]
        [RequestSizeLimit(256)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(PaymentLinkData), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetPaymentData([Required] string code)
        {
            var (isSuccess, _, linkData, error) = await _paymentLinkService.Get(code);
            return isSuccess
                ? Ok(linkData)
                : (IActionResult) BadRequest(ProblemDetailsBuilder.Build(error));
        }


        /// <summary>
        ///     Calculates signature from json model
        /// </summary>
        /// <returns>signature</returns>
        [AllowAnonymous]
        [RequestSizeLimit(512)]
        [HttpGet("{code}/signatures/{type}")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CalculateSignature(string code, SignatureTypes type)
        {
            var (_, isFailure, signature, error) = await _paymentLinksProcessingService.CalculateSignature(code, type, LanguageCode);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(signature);
        }


        [HttpPost("{code}/pay")]
        [AllowAnonymous]
        [RequestSizeLimit(512)]
        [ProducesResponseType(typeof(PaymentResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Pay([Required] string code, [FromBody][Required] string token)
        {
            var (isSuccess, _, paymentResponse, error) = await _paymentLinksProcessingService.Pay(code,
                token,
                GetClientIp(),
                LanguageCode);

            return isSuccess
                ? Ok(paymentResponse)
                : (IActionResult) BadRequest(ProblemDetailsBuilder.Build(error));
        }


        [HttpPost("{code}/callback")]
        [AllowAnonymous]
        [RequestSizeLimit(1024)]
        [ProducesResponseType(typeof(PaymentResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> PaymentCallback([Required] string code, [FromBody] JObject value)
        {
            var (isSuccess, _, paymentResponse, error) = await _paymentLinksProcessingService.ProcessPaymentResponse(code, value);
            return isSuccess
                ? Ok(paymentResponse)
                : (IActionResult) BadRequest(ProblemDetailsBuilder.Build(error));
        }


        private readonly IPaymentLinkService _paymentLinkService;
        private readonly IPaymentLinksProcessingService _paymentLinksProcessingService;
        private readonly IPayfortSignatureService _signatureService;
    }
}