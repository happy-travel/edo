using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Data.Customers;

namespace HappyTravel.Edo.Api.Services.Customers
{
    public interface ICustomerService
    {
        Task<Result<Customer>> Create(CustomerRegistrationInfo customerRegistration, string externalIdentity);
        Task<Result<Customer>> GetByIdentityId(string userToken);
        Task<Result<Customer>> GetByClientId(string clientIdClaim);
    }
}