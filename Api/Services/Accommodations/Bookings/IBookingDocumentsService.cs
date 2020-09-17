using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Documents;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public interface IBookingDocumentsService
    {
        Task<Result<BookingVoucherData>> GenerateVoucher(int bookingId, AgentContext agent, string languageCode);

        Task<Result<(DocumentRegistrationInfo RegistrationInfo, BookingInvoiceData Data)>> GetActualInvoice(int bookingId, AgentContext agent);

        Task<Result> GenerateInvoice(string referenceCode, AgentContext agent);

        Task<Result<(DocumentRegistrationInfo RegistrationInfo, PaymentReceipt Data)>> GenerateReceipt(int bookingId, AgentContext agent);

        Task<Result<(DocumentRegistrationInfo RegistrationInfo, PaymentReceipt Data)>> GenerateReceipt(int bookingId, Agent agent);
    }
}