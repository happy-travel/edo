using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Customers
{
    public class PermissionChecker : IPermissionChecker
    {
        public PermissionChecker(EdoContext context, IMemoryFlow flow, IAdministratorContext administratorContext)
        {
            _administratorContext = administratorContext;
            _context = context;
            _flow = flow;
        }


        public ValueTask<Result> CheckInCompanyPermission(CustomerInfo customer, InCompanyPermissions permission) 
            => CheckPermission(customer, permission, new List<CompanyStates>(1) {CompanyStates.FullAccess});


        public ValueTask<Result> CheckInCompanyReadOnlyPermission(CustomerInfo customer, InCompanyPermissions permission) 
            => CheckPermission(customer, permission, new List<CompanyStates>(2) {CompanyStates.ReadOnly, CompanyStates.FullAccess});


        private async ValueTask<Result> CheckPermission(CustomerInfo customer, InCompanyPermissions permission, List<CompanyStates> states)
        {
            if (await _administratorContext.HasPermission(AdministratorPermissions.CompanyVerification))
                return Result.Ok();

            var isCompanyVerified = await IsCompanyHasState(customer.CompanyId, states);
            if (!isCompanyVerified)
                return Result.Fail($"The action is available only for companies with verification levels: {states}");
            
            var storedPermissions = await _context.CustomerCompanyRelations
                .Where(r => r.CustomerId == customer.CustomerId)
                .Where(r => r.CompanyId == customer.CompanyId)
                .Where(r => r.BranchId == customer.BranchId)
                .Select(r => r.InCompanyPermissions)
                .SingleOrDefaultAsync();

            if (Equals(storedPermissions, default))
                return Result.Fail("The customer isn't affiliated with the company");

            return !storedPermissions.HasFlag(permission) 
                ? Result.Fail($"Customer does not have permission '{permission}'") 
                : Result.Ok();


            ValueTask<bool> IsCompanyHasState(int companyId, List<CompanyStates> companyStates)
            {
                var cacheKey = _flow.BuildKey(nameof(PermissionChecker), nameof(IsCompanyHasState), companyId.ToString());
                return _flow.GetOrSetAsync(cacheKey, ()
                        => _context.Companies
                            .Where(c => c.Id == companyId)
                            .AnyAsync(c => companyStates.Contains(c.State)), 
                    CompanyStateCacheTtl);
            }
        }


        private static readonly TimeSpan CompanyStateCacheTtl = TimeSpan.FromMinutes(5);

        private readonly EdoContext _context;
        private readonly IMemoryFlow _flow;
        private readonly IAdministratorContext _administratorContext;
    }
}