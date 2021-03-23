using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Management;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public interface ICounterpartyAccountService
    {
        Task<Result<CounterpartyBalanceInfo>> GetBalance(int counterpartyId, Currencies currency);

        Task<Result> AddMoney(int counterpartyAccountId, PaymentData paymentData, UserInfo user);

        Task<Result> SubtractMoney(int counterpartyAccountId, PaymentCancellationData data, UserInfo user);

        Task<Result> TransferToDefaultAgency(int counterpartyAccountId, MoneyAmount amount, UserInfo user);

        Task<Result> IncreaseManually(int counterpartyAccountId, PaymentData data, UserInfo user);

        Task<Result> DecreaseManually(int counterpartyAccountId, PaymentData data, UserInfo user);
        
        Task<List<CounterpartyAccountInfo>> GetAccounts(int counterpartyId);
    }
}