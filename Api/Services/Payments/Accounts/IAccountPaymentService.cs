using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.Services.Payments.Accounts
{
    public interface IAccountPaymentService
    {
        Task<bool> CanPayWithAccount(AgentContext agentContext);
        Task<Result<AccountBalanceInfo>> GetAccountBalance(Currencies currency, AgentContext agent);
        Task<Result<AccountBalanceInfo>> GetAccountBalance(Currencies currency, int agencyId);
        Task<Result<PaymentResponse>> Charge(Booking booking, UserInfo user, int agencyId, string clientIp);
        Task<Result> Refund(Booking booking, UserInfo user);
        Task<Result> TransferToChildAgency(int payerAccountId, int recipientAccountId, MoneyAmount amount, AgentContext agent);
    }
}