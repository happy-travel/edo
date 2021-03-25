using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.Edo.Api.AdministratorServices.Models;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agencies;
using HappyTravel.Edo.Api.Models.Management.AuditEvents;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Locations;
using HappyTravel.Money.Enums;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public class CounterpartyManagementService : ICounterpartyManagementService
    {
        public CounterpartyManagementService(EdoContext context,
            IDateTimeProvider dateTimeProvider,
            IManagementAuditService managementAuditService)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _managementAuditService = managementAuditService;
        }


        public async Task<Result<CounterpartyInfo>> Get(int counterpartyId, string languageCode)
        {
            var counterpartyData = await (from cp in _context.Counterparties
                join c in _context.Countries
                    on cp.CountryCode equals c.Code
                where cp.Id == counterpartyId
                select new
                {
                    Counterparty = cp,
                    Country = c
                }).SingleOrDefaultAsync();
            if (counterpartyData == default)
                return Result.Failure<CounterpartyInfo>("Could not find counterparty with specified id");

            return ToCounterpartyInfo(counterpartyData.Counterparty, counterpartyData.Country, languageCode);
        }


        public async Task<List<CounterpartyInfo>> Get(string languageCode)
        {
            var counterparties = await (from cp in _context.Counterparties
                join c in _context.Countries on cp.CountryCode equals c.Code
                join formula in _context.DisplayMarkupFormulas on new
                {
                    Id = (int?) cp.Id,
                    AgencyId = (int?) null,
                    AgentId = (int?) null
                } equals new
                {
                    Id = formula.CounterpartyId,
                    formula.AgencyId,
                    formula.AgentId
                } into formulas
                from markupFormula in formulas.DefaultIfEmpty()
                select new
                {
                    Counterparty = cp,
                    Country = c,
                    MarkupFormula = markupFormula == null ? null : markupFormula.DisplayFormula
                }).ToListAsync();

            return counterparties.Select(c => ToCounterpartyInfo(c.Counterparty, c.Country, languageCode, c.MarkupFormula)).ToList();
        }


        public Task<List<CounterpartyPrediction>> GetCounterpartiesPredictions(string query)
            => (from c in _context.Counterparties
                    join ag in _context.Agencies on c.Id equals ag.CounterpartyId
                    join ar in _context.AgentAgencyRelations on ag.Id equals ar.AgencyId
                    join a in _context.Agents on ar.AgentId equals a.Id
                    where c.IsActive
                        && ar.IsActive
                        && ar.Type == AgentAgencyRelationTypes.Master
                        && c.State == CounterpartyStates.FullAccess
                        && !string.IsNullOrEmpty(c.Name) && c.Name.ToLower().StartsWith(query.ToLower())
                            || !string.IsNullOrEmpty(a.FirstName) && a.FirstName.ToLower().StartsWith(query.ToLower())
                            || !string.IsNullOrEmpty(a.LastName) && a.LastName.ToLower().StartsWith(query.ToLower())
                            || !string.IsNullOrEmpty(c.BillingEmail) && c.BillingEmail.ToLower().StartsWith(query.ToLower())
                            || !string.IsNullOrEmpty(a.Email) && a.Email.ToLower().StartsWith(query.ToLower())
                    select new CounterpartyPrediction(c.Id, c.Name, a.FirstName + " " + a.LastName, c.BillingEmail ?? a.Email))
                .Distinct()
                .ToListAsync();


        public Task<Result<List<AgencyInfo>>> GetAllCounterpartyAgencies(int counterpartyId)
        {
            return GetCounterparty(counterpartyId)
                .Map(counterparty => GetAgencies());


            Task<List<AgencyInfo>> GetAgencies()
                => _context.Agencies.Where(a => a.CounterpartyId == counterpartyId)
                    .Select(a => new AgencyInfo(a.Name, a.Id, a.CounterpartyId)).ToListAsync();
        }


        public Task<Result<CounterpartyInfo>> Update(CounterpartyEditRequest changedCounterpartyInfo, int counterpartyId, string languageCode)
        {
            return GetCounterparty(counterpartyId)
                .Bind(UpdateCounterparty);


            async Task<Result<CounterpartyInfo>> UpdateCounterparty(Counterparty counterpartyToUpdate)
            {
                var (_, isFailure, error) = CounterpartyValidator.Validate(changedCounterpartyInfo);
                if (isFailure)
                    return Result.Failure<CounterpartyInfo>(error);

                counterpartyToUpdate.Address = changedCounterpartyInfo.Address;
                counterpartyToUpdate.City = changedCounterpartyInfo.City;
                counterpartyToUpdate.CountryCode = changedCounterpartyInfo.CountryCode;
                counterpartyToUpdate.Fax = changedCounterpartyInfo.Fax;
                counterpartyToUpdate.Name = changedCounterpartyInfo.Name;
                counterpartyToUpdate.Phone = changedCounterpartyInfo.Phone;
                counterpartyToUpdate.Website = changedCounterpartyInfo.Website;
                counterpartyToUpdate.PostalCode = changedCounterpartyInfo.PostalCode;
                // Hardcoded because only USD is supported
                counterpartyToUpdate.PreferredCurrency = Currencies.USD;
                counterpartyToUpdate.PreferredPaymentMethod = changedCounterpartyInfo.PreferredPaymentMethod;
                counterpartyToUpdate.Updated = _dateTimeProvider.UtcNow();
                counterpartyToUpdate.VatNumber = changedCounterpartyInfo.VatNumber;
                counterpartyToUpdate.BillingEmail = changedCounterpartyInfo.BillingEmail;

                _context.Counterparties.Update(counterpartyToUpdate);
                await _context.SaveChangesAsync();

                return await Get(counterpartyId, languageCode);
            }
        }


        // This method is the same with CounterpartyService.GetCounterparty,
        // because administrator services in the future will be replaced to another application
        private async Task<Result<Counterparty>> GetCounterparty(int counterpartyId)
        {
            var counterparty = await _context.Counterparties.SingleOrDefaultAsync(c => c.Id == counterpartyId);

            if (counterparty == null)
                return Result.Failure<Counterparty>("Could not find counterparty with specified id");

            return Result.Success(counterparty);
        }


        public Task<Result> DeactivateCounterparty(int counterpartyId, string reason)
            => GetCounterparty(counterpartyId)
                .Ensure(_ => !string.IsNullOrWhiteSpace(reason), "Reason must not be empty")
                .BindWithTransaction(_context, counterparty => ChangeActivityStatus(counterparty, ActivityStatus.NotActive)
                    .Tap(() => WriteCounterpartyDeactivationToAuditLog(counterpartyId, reason)));


        public Task<Result> ActivateCounterparty(int counterpartyId, string reason)
            => GetCounterparty(counterpartyId)
                .Ensure(_ => !string.IsNullOrWhiteSpace(reason), "Reason must not be empty")
                .BindWithTransaction(_context, counterparty => ChangeActivityStatus(counterparty, ActivityStatus.Active)
                    .Tap(() => WriteCounterpartyActivationToAuditLog(counterpartyId, reason)));


        private Task<Result> ChangeActivityStatus(Counterparty counterparty, ActivityStatus status)
        {
            var convertedStatus = ConvertToDbStatus(status);
            if (convertedStatus == counterparty.IsActive)
                return Task.FromResult(Result.Success());

            return ChangeCounterpartyActivityStatus()
                .Tap(ChangeCounterpartyAccountsActivityStatus);


            async Task<Result> ChangeCounterpartyActivityStatus()
            {
                counterparty.IsActive = convertedStatus;
                counterparty.Updated = _dateTimeProvider.UtcNow();

                _context.Update(counterparty);
                await _context.SaveChangesAsync();
                return Result.Success();
            }


            async Task ChangeCounterpartyAccountsActivityStatus()
            {
                var counterpartyAccounts = await _context.CounterpartyAccounts
                    .Where(c => c.CounterpartyId == counterparty.Id)
                    .ToListAsync();

                foreach (var account in counterpartyAccounts)
                    account.IsActive = convertedStatus;

                _context.UpdateRange(counterpartyAccounts);
                await _context.SaveChangesAsync();
            }
        }


        private Task WriteCounterpartyDeactivationToAuditLog(int counterpartyId, string reason)
            => _managementAuditService.Write(ManagementEventType.CounterpartyDeactivation,
                new CounterpartyActivityStatusChangeEventData(counterpartyId, reason));


        private Task WriteCounterpartyActivationToAuditLog(int counterpartyId, string reason)
            => _managementAuditService.Write(ManagementEventType.CounterpartyActivation,
                new CounterpartyActivityStatusChangeEventData(counterpartyId, reason));


        private static CounterpartyInfo ToCounterpartyInfo(Counterparty counterparty, Country country, string languageCode, string markupFormula = null)
            => new (counterparty.Id,
                counterparty.Name,
                counterparty.Address,
                counterparty.CountryCode,
                LocalizationHelper.GetValueFromSerializedString(country.Names, languageCode),
                counterparty.City,
                counterparty.Phone,
                counterparty.Fax,
                counterparty.PostalCode,
                counterparty.PreferredCurrency,
                counterparty.PreferredPaymentMethod,
                counterparty.Website,
                counterparty.VatNumber,
                counterparty.BillingEmail,
                counterparty.IsContractUploaded,
                counterparty.State,
                counterparty.Verified,
                markupFormula);


        private bool ConvertToDbStatus(ActivityStatus status) => status == ActivityStatus.Active;
        
        private readonly IManagementAuditService _managementAuditService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly EdoContext _context;
    }
}