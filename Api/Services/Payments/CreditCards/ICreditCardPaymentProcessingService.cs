using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Payments;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Services.Payments.CreditCards
{
    public interface ICreditCardPaymentProcessingService
    {
        Task<Result<PaymentResponse>> Authorize(NewCreditCardPaymentRequest request, 
            string languageCode, string ipAddress, IPaymentsService paymentsService);


        Task<Result<PaymentResponse>> Authorize(SavedCreditCardPaymentRequest request, string languageCode, 
            string ipAddress, IPaymentsService paymentsService);


        Task<Result<PaymentResponse>> ProcessPaymentResponse(JObject rawResponse, IPaymentsService paymentsService);

        Task<Result<string>> CaptureMoney(string referenceCode, IPaymentsService paymentsService);

        Task<Result> VoidMoney(string referenceCode, IPaymentsService paymentsService);
    }
}