using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Customers;

namespace HappyTravel.Edo.Api.Services.Customers
{
    public interface ICustomerService
    {
        Task<Result<Customer>> Add(CustomerRegistrationInfo customerRegistration, string externalIdentity, string email);

        Task<Result<Customer>> GetMasterCustomer(int companyId);

        Task<Result<List<CustomerInfoSlim>>> GetCustomers(int companyId, int branchId = default);

        Task<Result<CustomerInfo>> GetCustomer(int companyId, int branchId, int customerId);
        
        Task<Result<List<InCompanyPermissions>>> UpdateCustomerPermissions(int companyId, int branchId, int customerId,
            List<InCompanyPermissions> permissions);
    }
}