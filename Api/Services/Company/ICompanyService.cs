using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Company;

namespace HappyTravel.Edo.Api.Services.Company
{
    public interface ICompanyService
    {
        Task<Result<CompanyInfo>> Get();
    }
}