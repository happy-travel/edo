using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.AuditEvents;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Money.Models;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Payments.Accounts
{
    public class AccountPaymentProcessingService : IAccountPaymentProcessingService
    {
        public AccountPaymentProcessingService(EdoContext context,
            IEntityLocker locker,
            IAccountBalanceAuditService auditService)
        {
            _context = context;
            _locker = locker;
            _auditService = auditService;
        }


        public async Task<Result> AddMoney(int accountId, PaymentData paymentData, UserInfo user)
        {
            return await GetAccount(accountId)
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                .BindWithLock(_locker, a => Result.Success(a)
                    .BindWithTransaction(_context, account => Result.Success(account)
                        .Map(AddMoney)
                        .Map(WriteAuditLog)
                    ));


            bool IsReasonProvided(AgencyAccount account) => !string.IsNullOrEmpty(paymentData.Reason);


            async Task<AgencyAccount> AddMoney(AgencyAccount account)
            {
                account.Balance += paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            async Task<AgencyAccount> WriteAuditLog(AgencyAccount account)
            {
                var eventData = new AccountBalanceLogEventData(paymentData.Reason, account.Balance, account.AuthorizedBalance);
                await _auditService.Write(AccountEventType.Add,
                    account.Id,
                    paymentData.Amount,
                    user,
                    eventData,
                    null);

                return account;
            }
        }


        public async Task<Result> ChargeMoney(int accountId, PaymentData paymentData, UserInfo user)
        {
            return await GetAccount(accountId)
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                .BindWithLock(_locker, a => Result.Success(a)
                    .Ensure(IsBalanceSufficient, "Could not charge money, insufficient balance")
                    .BindWithTransaction(_context, account => Result.Success(account)
                        .Map(ChargeMoney)
                        .Map(WriteAuditLog)));

            bool IsReasonProvided(AgencyAccount account) => !string.IsNullOrEmpty(paymentData.Reason);

            bool IsBalanceSufficient(AgencyAccount account) => this.IsBalanceSufficient(account, paymentData.Amount);


            async Task<AgencyAccount> ChargeMoney(AgencyAccount account)
            {
                account.Balance -= paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            async Task<AgencyAccount> WriteAuditLog(AgencyAccount account)
            {
                var eventData = new AccountBalanceLogEventData(paymentData.Reason, account.Balance, account.AuthorizedBalance);
                await _auditService.Write(AccountEventType.Charge,
                    account.Id,
                    paymentData.Amount,
                    user,
                    eventData,
                    null);

                return account;
            }
        }


        public async Task<Result> AuthorizeMoney(int accountId, AuthorizedMoneyData paymentData, UserInfo user)
        {
            return await GetAccount(accountId)
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                .BindWithLock(_locker, a => Result.Success(a)
                    .Ensure(IsBalancePositive, "Could not charge money, insufficient balance")
                    .BindWithTransaction(_context, account => Result.Success(account)
                        .Map(AuthorizeMoney)
                        .Map(WriteAuditLog)));

            bool IsReasonProvided(AgencyAccount account) => !string.IsNullOrEmpty(paymentData.Reason);

            bool IsBalancePositive(AgencyAccount account) => (account.Balance).IsGreaterThan(decimal.Zero);


            async Task<AgencyAccount> AuthorizeMoney(AgencyAccount account)
            {
                account.AuthorizedBalance += paymentData.Amount;
                account.Balance -= paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            async Task<AgencyAccount> WriteAuditLog(AgencyAccount account)
            {
                var eventData = new AccountBalanceLogEventData(paymentData.Reason, account.Balance, account.AuthorizedBalance);
                await _auditService.Write(AccountEventType.Authorize,
                    account.Id,
                    paymentData.Amount,
                    user,
                    eventData,
                    paymentData.ReferenceCode);

                return account;
            }
        }


        public async Task<Result> CaptureMoney(int accountId, AuthorizedMoneyData paymentData, UserInfo user)
        {
            return await GetAccount(accountId)
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                .BindWithLock(_locker, a => Result.Success(a)
                    .Ensure(IsAuthorizedSufficient, "Could not capture money, insufficient authorized balance")
                    .BindWithTransaction(_context, account => Result.Success(account)
                        .Map(CaptureMoney)
                        .Map(WriteAuditLog)));

            bool IsReasonProvided(AgencyAccount account) => !string.IsNullOrEmpty(paymentData.Reason);

            bool IsAuthorizedSufficient(AgencyAccount account) => this.IsAuthorizedSufficient(account, paymentData.Amount);


            async Task<AgencyAccount> CaptureMoney(AgencyAccount account)
            {
                account.AuthorizedBalance -= paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            Task<AgencyAccount> WriteAuditLog(AgencyAccount account) => WriteAuditLogWithReferenceCode(account, paymentData, AccountEventType.Capture, user);
        }


        public async Task<Result> VoidMoney(int accountId, AuthorizedMoneyData paymentData, UserInfo user)
        {
            return await GetAccount(accountId)
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                .BindWithLock(_locker, a => Result.Success(a)
                    .Ensure(IsAuthorizedSufficient, "Could not void money, insufficient authorized balance")
                    .BindWithTransaction(_context, account => Result.Success(account)
                        .Map(VoidMoney)
                        .Map(WriteAuditLog)));

            bool IsReasonProvided(AgencyAccount account) => !string.IsNullOrEmpty(paymentData.Reason);

            bool IsAuthorizedSufficient(AgencyAccount account) => this.IsAuthorizedSufficient(account, paymentData.Amount);


            async Task<AgencyAccount> VoidMoney(AgencyAccount account)
            {
                account.AuthorizedBalance -= paymentData.Amount;
                account.Balance += paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            Task<AgencyAccount> WriteAuditLog(AgencyAccount account) => WriteAuditLogWithReferenceCode(account, paymentData, AccountEventType.Void, user);
        }


        public async Task<Result> TransferToChildAgency(int payerAccountId, int recipientAccountId, MoneyAmount amount, AgentContext agent)
        {
            var user = agent.ToUserInfo();

            return await Result.Success()
                .Ensure(IsAmountPositive, "Payment amount must be a positive number")
                .Bind(GetPayerAccount)
                .Ensure(IsAgentUsingHisAgencyAccount, "You can only transfer money from an agency you are currently using")
                .Bind(GetRecipientAccount)
                .Ensure(IsRecipientAgencyChildOfPayerAgency, "Transfers are only possible to accounts of child agencies")
                .Ensure(AreAccountsCurrenciesMatch, "Currencies of specified accounts mismatch")
                .Ensure(IsAmountCurrencyMatch, "Currency of specified amount mismatch")
                .BindWithLock(_locker, a => Result.Success(a)
                    .Ensure(IsBalanceSufficient, "Could not charge money, insufficient balance")
                    .BindWithTransaction(_context, accounts => Result.Success(accounts)
                        .Map(TransferMoney)
                        .Tap(WriteAuditLog)));


            async Task<Result<AgencyAccount>> GetPayerAccount()
            {
                var (isSuccess, _, recipientAccount, _) = await GetAccount(payerAccountId);
                return isSuccess
                    ? recipientAccount
                    : Result.Failure<AgencyAccount>("Could not find payer account");
            }


            bool IsAgentUsingHisAgencyAccount(AgencyAccount payerAccount) => agent.IsUsingAgency(payerAccount.AgencyId);


            async Task<Result<(AgencyAccount, AgencyAccount)>> GetRecipientAccount(AgencyAccount payerAccount)
            {
                var (isSuccess, _, recipientAccount, _) = await GetAccount(recipientAccountId);
                return isSuccess
                    ? (payerAccount, recipientAccount)
                    : Result.Failure<(AgencyAccount, AgencyAccount)>("Could not find recipient account");
            }


            bool IsAmountPositive() => amount.Amount.IsGreaterThan(decimal.Zero);


            async Task<bool> IsRecipientAgencyChildOfPayerAgency((AgencyAccount payerAccount, AgencyAccount recipientAccount) accounts)
            {
                var recipientAgency = await _context.Agencies.Where(a => a.Id == accounts.recipientAccount.AgencyId).SingleOrDefaultAsync();
                return recipientAgency.ParentId == accounts.payerAccount.AgencyId;
            }


            bool AreAccountsCurrenciesMatch((AgencyAccount payerAccount, AgencyAccount recipientAccount) accounts)
                => accounts.payerAccount.Currency == accounts.recipientAccount.Currency;


            bool IsAmountCurrencyMatch((AgencyAccount payerAccount, AgencyAccount recipientAccount) accounts)
                => accounts.payerAccount.Currency == amount.Currency;


            bool IsBalanceSufficient((AgencyAccount payerAccount, AgencyAccount recipientAccount) accounts)
                => accounts.payerAccount.Balance.IsGreaterOrEqualThan(amount.Amount);


            async Task<(AgencyAccount, AgencyAccount)> TransferMoney(
                (AgencyAccount payerAccount, AgencyAccount recipientAccount) accounts)
            {
                accounts.payerAccount.Balance -= amount.Amount;
                _context.Update(accounts.payerAccount);

                accounts.recipientAccount.Balance += amount.Amount;
                _context.Update(accounts.recipientAccount);

                await _context.SaveChangesAsync();

                return accounts;
            }


            async Task WriteAuditLog((AgencyAccount payerAccount, AgencyAccount recipientAccount) accounts)
            {
                var counterpartyEventData = new AccountBalanceLogEventData(null, accounts.payerAccount.Balance,
                    accounts.payerAccount.AuthorizedBalance);

                await _auditService.Write(AccountEventType.AgencyTransferToAgency, accounts.payerAccount.Id,
                    amount.Amount, user, counterpartyEventData, null);

                var agencyEventData = new AccountBalanceLogEventData(null, accounts.recipientAccount.Balance,
                    accounts.recipientAccount.AuthorizedBalance);

                await _auditService.Write(AccountEventType.AgencyTransferToAgency, accounts.recipientAccount.Id,
                    amount.Amount, user, agencyEventData, null);
            }
        }


        private bool IsBalanceSufficient(AgencyAccount account, decimal amount) => (account.Balance).IsGreaterOrEqualThan(amount);


        private bool IsAuthorizedSufficient(AgencyAccount account, decimal amount) => account.AuthorizedBalance.IsGreaterOrEqualThan(amount);


        private bool AreCurrenciesMatch(AgencyAccount account, PaymentData paymentData) => account.Currency == paymentData.Currency;

        private bool AreCurrenciesMatch(AgencyAccount account, AuthorizedMoneyData paymentData) => account.Currency == paymentData.Currency;


        private async Task<Result<AgencyAccount>> GetAccount(int accountId)
        {
            var account = await _context.AgencyAccounts.SingleOrDefaultAsync(p => p.IsActive && p.Id == accountId);
            return account == default
                ? Result.Failure<AgencyAccount>("Could not find account")
                : Result.Success(account);
        }


        private async Task<AgencyAccount> WriteAuditLogWithReferenceCode(AgencyAccount account, AuthorizedMoneyData paymentData, AccountEventType eventType,
            UserInfo user)
        {
            var eventData = new AccountBalanceLogEventData(paymentData.Reason, account.Balance, account.AuthorizedBalance);
            await _auditService.Write(eventType,
                account.Id,
                paymentData.Amount,
                user,
                eventData,
                paymentData.ReferenceCode);

            return account;
        }


        private readonly IAccountBalanceAuditService _auditService;
        private readonly EdoContext _context;
        private readonly IEntityLocker _locker;
    }
}