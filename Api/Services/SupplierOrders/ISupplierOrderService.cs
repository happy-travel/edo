using System.Threading.Tasks;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.SupplierOrders
{
    public interface ISupplierOrderService
    {
        Task Add(string referenceCode, ServiceTypes serviceType, decimal supplierPrice, Suppliers supplier);

        Task Cancel(string referenceCode);
    }
}