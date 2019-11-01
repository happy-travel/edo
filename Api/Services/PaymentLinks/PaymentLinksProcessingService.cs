using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.External;
using HappyTravel.Edo.Api.Models.Payments.Payfort;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Common.Enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Services.PaymentLinks
{
    public class PaymentLinksProcessingService : IPaymentLinksProcessingService
    {
        public PaymentLinksProcessingService(IPayfortService payfortService,
            IPaymentLinkService linkService,
            IPayfortSignatureService signatureService,
            IOptions<PayfortOptions> payfortOptions)
        {
            _payfortService = payfortService;
            _linkService = linkService;
            _signatureService = signatureService;
            _payfortOptions = payfortOptions.Value;
        }


        public Task<Result<PaymentResponse>> Pay(string code, string token, string ip, string languageCode)
        {
            return GetLink(code)
                .OnSuccess(Pay)
                .Map(ToPaymentResponse);

            Task<Result<CreditCardPaymentResult>> Pay(PaymentLinkData link)
            {
                return _payfortService.Pay(new CreditCardPaymentRequest(
                    amount: link.Amount,
                    currency: link.Currency,
                    token: new PaymentTokenInfo(token, PaymentTokenTypes.OneTime), 
                    // TODO get customer name anywhere else
                    customerName: new MailAddress(link.Email).User, 
                    customerEmail: link.Email,
                    customerIp: ip,
                    referenceCode: link.ReferenceCode,
                    languageCode: languageCode,
                    isNewCard: true,
                    // Is not needed for new card
                    securityCode: null));
            }

            PaymentResponse ToPaymentResponse(CreditCardPaymentResult cr) => new PaymentResponse(cr.Secure3d, cr.Status, cr.Message);
        }


        public Task<Result<PaymentResponse>> ProcessPaymentResponse(string code, JObject response)
        {
            return GetLinkToPay()
                .OnSuccess(ProcessCardResponse)
                .OnSuccess(StorePaymentResult);

            async Task<Result<PaymentLinkData>> GetLinkToPay()
            {
                var (_, isFailure, link, error) = await GetLink(code);
                if (isFailure)
                    return Result.Fail<PaymentLinkData>(error);

                return link.PaymentStatus == PaymentStatuses.Success
                    ? Result.Fail<PaymentLinkData>("Link is already paid")
                    : Result.Ok(link);
            }


            Result<PaymentResponse> ProcessCardResponse(PaymentLinkData link)
            {
                var (_, isFailure, cr, error) = _payfortService.ProcessPaymentResponse(response);
                if (isFailure)
                    return Result.Fail<PaymentResponse>(error);

                return Result.Ok(new PaymentResponse(cr.Secure3d, cr.Status, cr.Message));
            }


            async Task<PaymentResponse> StorePaymentResult(PaymentResponse paymentResponse)
            {
                await _linkService.UpdatePaymentStatus(code, paymentResponse);
                return paymentResponse;
            }
        }


        public Task<Result<string>> CalculateSignature(string code, string languageCode)
        {
            return GetLink(code)
                .OnSuccess(GetSignature);

            Result<string> GetSignature(PaymentLinkData paymentLinkData)
            {
                var signingData = new Dictionary<string, string>
                {
                    { "service_command", "TOKENIZATION" },
                    { "access_code", _payfortOptions.AccessCode },
                    { "merchant_identifier", _payfortOptions.Identifier },
                    { "merchant_reference", paymentLinkData.ReferenceCode },
                    { "language", languageCode },
                    { "return_url", _payfortOptions.ReturnUrl },
                    { "signature", string.Empty }
                };
                return _signatureService.Calculate(signingData, SignatureTypes.Request);
            }
        }


        private Task<Result<PaymentLinkData>> GetLink(string code) => _linkService.Get(code);
        
        private readonly IPayfortService _payfortService;
        private readonly IPaymentLinkService _linkService;
        private readonly IPayfortSignatureService _signatureService;
        private readonly PayfortOptions _payfortOptions;
    }
}