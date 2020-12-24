using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Money.Models;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Services.Payments.CreditCards
{
    public interface ICreditCardPaymentProcessingService
    {
        Task<Result<PaymentResponse>> Authorize(NewCreditCardPaymentRequest request, 
            string languageCode, string ipAddress, IPaymentsService paymentsService, AgentContext agent);


        Task<Result<PaymentResponse>> Authorize(SavedCreditCardPaymentRequest request, string languageCode, 
            string ipAddress, IPaymentsService paymentsService, AgentContext agent);


        Task<Result<PaymentResponse>> ProcessPaymentResponse(JObject rawResponse, IPaymentsService paymentsService);

        Task<Result<string>> CaptureMoney(string referenceCode, UserInfo user, IPaymentsService paymentsService);

        Task<Result> VoidMoney(string referenceCode, UserInfo user, IPaymentsService paymentsService);

        Task<Result> RefundMoney(string referenceCode, MoneyAmount refundingAmount, UserInfo user, IPaymentsService paymentsService);
    }
}