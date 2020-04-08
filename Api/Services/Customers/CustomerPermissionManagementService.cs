using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Customers;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Customers
{
    public class CustomerPermissionManagementService : ICustomerPermissionManagementService
    {
        public CustomerPermissionManagementService(EdoContext context,
            ICustomerContext customerContext, IPermissionChecker permissionChecker)
        {
            _context = context;
            _customerContext = customerContext;
            _permissionChecker = permissionChecker;
        }


        public Task<Result<List<InCounterpartyPermissions>>> SetInCounterpartyPermissions(int counterpartyId, int agencyId, int customerId,
            List<InCounterpartyPermissions> permissionsList) =>
            SetInCounterpartyPermissions(counterpartyId, agencyId, customerId, permissionsList.Aggregate((p1, p2) => p1 | p2));


        public async Task<Result<List<InCounterpartyPermissions>>> SetInCounterpartyPermissions(int counterpartyId, int agencyId, int customerId,
            InCounterpartyPermissions permissions)
        {
            var customer = await _customerContext.GetCustomer();

            return await CheckPermission()
                .OnSuccess(CheckCounterpartyAndAgency)
                .OnSuccess(GetRelation)
                .Ensure(IsPermissionManagementRightNotLost, "Cannot revoke last permission management rights")
                .OnSuccess(UpdatePermissions);

            Result CheckPermission()
            {
                if (!customer.InCounterpartyPermissions.HasFlag(InCounterpartyPermissions.PermissionManagementInAgency)
                    && !customer.InCounterpartyPermissions.HasFlag(InCounterpartyPermissions.PermissionManagementInCounterparty))
                    return Result.Fail("You have no acceptance to manage customers permissions");

                return Result.Ok();
            }

            Result CheckCounterpartyAndAgency()
            {
                if (customer.CounterpartyId != counterpartyId)
                {
                    return Result.Fail("The customer isn't affiliated with the counterparty");
                }

                // TODO When agency system gets ierarchic, this needs to be changed so that customer can see customers/markups of his own agency and its subagencies
                if (!customer.InCounterpartyPermissions.HasFlag(InCounterpartyPermissions.PermissionManagementInCounterparty)
                    && customer.AgencyId != agencyId)
                {
                    return Result.Fail("The customer isn't affiliated with the agency");
                }
                
                return Result.Ok();
            }

            async Task<Result<CustomerCounterpartyRelation>> GetRelation()
            {
                var relation = await _context.CustomerCounterpartyRelations
                    .SingleOrDefaultAsync(r => r.CustomerId == customerId && r.CounterpartyId == counterpartyId && r.AgencyId == agencyId);

                return relation is null
                    ? Result.Fail<CustomerCounterpartyRelation>(
                        $"Could not find relation between the customer {customerId} and the counterparty {counterpartyId}")
                    : Result.Ok(relation);
            }


            async Task<bool> IsPermissionManagementRightNotLost(CustomerCounterpartyRelation relation)
            {
                if (permissions.HasFlag(InCounterpartyPermissions.PermissionManagementInCounterparty))
                    return true;

                return (await _context.CustomerCounterpartyRelations
                        .Where(r => r.CounterpartyId == relation.CounterpartyId && r.CustomerId != relation.CustomerId)
                        .ToListAsync())
                    .Any(c => c.InCounterpartyPermissions.HasFlag(InCounterpartyPermissions.PermissionManagementInCounterparty));
            }


            async Task<List<InCounterpartyPermissions>> UpdatePermissions(CustomerCounterpartyRelation relation)
            {
                relation.InCounterpartyPermissions = permissions;

                _context.CustomerCounterpartyRelations.Update(relation);
                await _context.SaveChangesAsync();

                return relation.InCounterpartyPermissions.ToList();
            }
        }


        private readonly EdoContext _context;
        private readonly ICustomerContext _customerContext;
        private readonly IPermissionChecker _permissionChecker;
    }
}