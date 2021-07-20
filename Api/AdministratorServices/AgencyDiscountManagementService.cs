using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices.Models;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Management.AuditEvents;
using HappyTravel.Edo.Api.Services;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.Markups.Templates;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Markup;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public class AgencyDiscountManagementService : IAgencyDiscountManagementService
    {
        public AgencyDiscountManagementService(EdoContext context,
            IManagementAuditService managementAuditService,
            IMarkupPolicyTemplateService templateService
            )
        {
            _context = context;
            _managementAuditService = managementAuditService;
            _templateService = templateService;
        }


        public async Task<List<DiscountInfo>> Get(int agencyId)
        {
            var query = from discount in _context.Discounts
                join markupPolicy in _context.MarkupPolicies on discount.TargetPolicyId equals markupPolicy.Id
                where discount.TargetAgencyId == agencyId
                select new DiscountInfo
                {
                    Id = discount.Id,
                    DiscountPercent = discount.DiscountPercent,
                    Description = discount.Description,
                    TargetMarkupId = discount.TargetPolicyId,
                    TargetPolicyDescription = markupPolicy.Description,
                    IsActive = discount.IsActive
                };

            return await query.ToListAsync();
        }


        public Task<Result> Start(int agencyId, int discountId)
            => ChangeActivityState(agencyId, discountId, true);


        public Task<Result> Stop(int agencyId, int discountId)
            => ChangeActivityState(agencyId, discountId, false);


        public Task<Result> Add(int agencyId, CreateDiscountRequest createDiscountRequest)
        {
            return ValidatePercent(createDiscountRequest.DiscountPercent)
                .Bind(ValidateTargetMarkup)
                .Bind(ValidateAgency)
                .Bind(DiscountDoesntExceedMarkups)
                .BindWithTransaction(_context, () => Result.Success()
                    .Tap(UpdateDiscount)
                    .Bind(WriteAuditLog));


            async Task<Result> ValidateTargetMarkup()
            {
                var targetMarkup = await _context.MarkupPolicies
                    .SingleOrDefaultAsync(p => p.Id == createDiscountRequest.TargetMarkupId);

                if (targetMarkup is null)
                    return Result.Failure($"Could not find markup policy with id {createDiscountRequest.TargetMarkupId}");
                
                if (targetMarkup.ScopeType != MarkupPolicyScopeType.Global)
                    return Result.Failure("Cannot apply discount to non-global markup policy");

                return Result.Success();
            }


            async Task<Result> ValidateAgency()
                => await _context.Agencies.AnyAsync(a => a.Id == agencyId)
                    ? Result.Success()
                    : Result.Failure($"Could not find an agency with id {agencyId}");


            async Task<Result> DiscountDoesntExceedMarkups()
            {
                var markupPolicy = await _context.MarkupPolicies.SingleOrDefaultAsync(x => x.Id == createDiscountRequest.TargetMarkupId);
                var markupFunction = _templateService.CreateFunction(markupPolicy.Id, markupPolicy.TemplateSettings);
                
                var allDiscounts = await _context.Discounts
                    .Where(x => x.TargetPolicyId == markupPolicy.Id)
                    .Where(d => d.TargetAgencyId == agencyId)
                    .Where(d => d.IsActive)
                    .Select(d => d.DiscountPercent)
                    .ToListAsync();
                
                allDiscounts.Add(createDiscountRequest.DiscountPercent);

                return DiscountsValidator.DiscountsDontExceedMarkups(allDiscounts, markupFunction);
            }


            Task UpdateDiscount()
            {
                _context.Discounts.Add(new Discount
                {
                    DiscountPercent = createDiscountRequest.DiscountPercent,
                    Description = createDiscountRequest.Description,
                    TargetPolicyId = createDiscountRequest.TargetMarkupId,
                    IsActive = true,
                    TargetAgencyId = agencyId
                });
                return _context.SaveChangesAsync();
            }


            Task<Result> WriteAuditLog()
                => _managementAuditService.Write(ManagementEventType.DiscountCreate, new DiscountCreateEventData(agencyId, createDiscountRequest));
        }


        public async Task<Result> Update(int agencyId, int discountId, EditDiscountRequest editDiscountRequest)
        {
            return await Get(agencyId, discountId)
                .Check(_ => ValidatePercent(editDiscountRequest.DiscountPercent))
                .Check(DiscountDoesntExceedMarkups)
                .BindWithTransaction(_context, discount => Result.Success(discount)
                    .Tap(Update)
                    .Bind(WriteAuditLog));


            async Task<Result> DiscountDoesntExceedMarkups(Discount discount)
            {
                var markupPolicy = await _context.MarkupPolicies.SingleOrDefaultAsync(x => x.Id == discount.TargetPolicyId);
                var markupFunction = _templateService.CreateFunction(markupPolicy.Id, markupPolicy.TemplateSettings);
                
                var allDiscounts = await _context.Discounts
                    .Where(x => x.TargetPolicyId == markupPolicy.Id)
                    .Where(d => d.TargetAgencyId == agencyId)
                    .Where(d => d.IsActive)
                    .Where(d => d.Id != discountId) // excluding discount we want to edit
                    .Select(d => d.DiscountPercent)
                    .ToListAsync();
                
                allDiscounts.Add(editDiscountRequest.DiscountPercent);

                return DiscountsValidator.DiscountsDontExceedMarkups(allDiscounts, markupFunction);
            }
            

            Task Update(Discount discount)
                => this.Update(discount, d =>
                {
                    d.DiscountPercent = editDiscountRequest.DiscountPercent;
                    d.Description = editDiscountRequest.Description;
                });


            Task<Result> WriteAuditLog(Discount _)
                => _managementAuditService.Write(ManagementEventType.DiscountEdit, new DiscountEditEventData(agencyId, editDiscountRequest));
        }


        public async Task<Result> ChangeActivityState(int agencyId, int discountId, bool newActivityState)
        {
            return await Get(agencyId, discountId)
                .BindWithTransaction(_context, discount => Result.Success(discount)
                    .Tap(Update)
                    .Check(WriteAuditLog));


            Task Update(Discount discount)
                => this.Update(discount, d => d.IsActive = newActivityState);


            Task<Result> WriteAuditLog(Discount _)
                => _managementAuditService.Write(ManagementEventType.DiscountEdit, new DiscountActivityStateEventData(agencyId, newActivityState));
        }


        public async Task<Result> Delete(int agencyId, int discountId)
        {
            return await Get(agencyId, discountId)
                .BindWithTransaction(_context, discount => Result.Success(discount)
                    .Tap(Delete)
                    .Check(WriteAuditLog));


            async Task Delete(Discount discount)
            {
                _context.Remove(discount);
                await _context.SaveChangesAsync();
            }


            Task<Result> WriteAuditLog(Discount discount)
                => _managementAuditService.Write(ManagementEventType.DiscountDelete, new DiscountDeleteEventData(agencyId, GetDiscountInfo(discount)));


            static DiscountInfo GetDiscountInfo(Discount discount)
                => new DiscountInfo
                {
                    Id = discount.Id,
                    IsActive = discount.IsActive,
                    Description = discount.Description,
                    DiscountPercent = discount.DiscountPercent,
                    TargetMarkupId = discount.TargetPolicyId
                };
        }


        private async Task<Result<Discount>> Get(int agencyId, int discountId)
            => await _context.Discounts.SingleOrDefaultAsync(d => d.Id == discountId && d.TargetAgencyId == agencyId) ??
                Result.Failure<Discount>($"Could not find discount with id {discountId}");


        private async Task Update(Discount discount, Action<Discount> updateAction)
        {
            updateAction(discount);
            _context.Update(discount);
            await _context.SaveChangesAsync();
        }


        private Result ValidatePercent(decimal discountPercent)
            => discountPercent <= MaxDiscountPercent
                ? Result.Success()
                : Result.Failure($"Could not set discount percent with value more than {MaxDiscountPercent}");

        private const decimal MaxDiscountPercent = 5;
        
        private readonly EdoContext _context;
        private readonly IManagementAuditService _managementAuditService;
        private readonly IMarkupPolicyTemplateService _templateService;
    }
}