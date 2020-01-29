using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public interface IBookingService
    {
        Task<Result<BookingDetails, ProblemDetails>> Book(AccommodationBookingRequest bookingRequest, string languageCode);

        Task<Result> ProcessBookingResponse(BookingDetails bookingResponse, Booking booking = null);

        Task<Result<AccommodationBookingInfo>> Get(int bookingId);

        Task<Result<AccommodationBookingInfo>> Get(string referenceCode);

        Task<Result<List<SlimAccommodationBookingInfo>>> GetForCustomer();

        Task<Result<VoidObject, ProblemDetails>> Cancel(int bookingId);

        Task<Result<List<int>>> GetBookingsForCancellation(DateTime deadlineDate);

        Task<Result<ProcessResult>> CancelBookings(List<int> bookingIds);
    }
}