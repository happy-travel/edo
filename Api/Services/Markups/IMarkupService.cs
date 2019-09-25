using System.Threading.Tasks;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Common.Enums.Markup;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public interface IMarkupService
    {
        Task<Markup> GetMarkup(CustomerData customerData, MarkupPolicyTarget policyTarget);
    }
}