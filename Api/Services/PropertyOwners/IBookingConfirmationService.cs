using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.PropertyOwners;
using HappyTravel.Edo.Data.Bookings;
using System.Threading.Tasks;

namespace HappyTravel.Edo.Api.Services.PropertyOwners
{
    public interface IBookingConfirmationService
    {
        Task<Result<SlimBookingConfirmation>> Get(string referenceCode);
        Task<Result> Update(string referenceCode, BookingConfirmation bookingConfirmation);
        Task<Result> SendConfirmationEmail(Booking booking);
    }
}