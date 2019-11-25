﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}")]
    [Produces("application/json")]
    public class AccommodationsController : BaseController
    {
        public AccommodationsController(IAccommodationService service)
        {
            _service = service;
        }


        /// <summary>
        ///     Returns the full set of accommodation details.
        /// </summary>
        /// <param name="accommodationId">Accommodation ID, obtained from an availability query.</param>
        /// <returns></returns>
        [HttpGet("accommodations/{accommodationId}")]
        [ProducesResponseType(typeof(RichAccommodationDetails), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async ValueTask<IActionResult> Get([FromRoute] string accommodationId)
        {
            if (string.IsNullOrWhiteSpace(accommodationId))
                return BadRequest(ProblemDetailsBuilder.Build("No accommodation IDs was provided."));

            var (_, isFailure, response, error) = await _service.Get(accommodationId, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(response);
        }


        /// <summary>
        ///     Returns accommodations available for a booking.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("availabilities/accommodations")]
        [ProducesResponseType(typeof(AvailabilityResponse), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetAvailability([FromBody] AvailabilityRequest request)
        {
            var (_, isFailure, response, error) = await _service.GetAvailable(request, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(response);
        }


        /// <summary>
        ///     Book an accommodation.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("bookings/accommodations")]
        [ProducesResponseType(typeof(AccommodationBookingDetails), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Book([FromBody] AccommodationBookingRequest request)
        {
            var (_, isFailure, bookingDetails, error) = await _service.Book(request, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(bookingDetails);
        }


        /// <summary>
        ///     Cancel accommodation booking.
        /// </summary>
        /// <param name="bookingId">Id of booking to cancel</param>
        /// <returns></returns>
        [HttpPost("bookings/accommodations/{bookingId}/cancel")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var (_, isFailure, _, error) = await _service.CancelBooking(bookingId);
            if (isFailure)
                return BadRequest(error);

            return NoContent();
        }


        /// <summary>
        /// The method gets booking data by a booking Id for a current customer.
        /// </summary>
        /// <returns>Full booking data.</returns>
        [HttpGet("bookings/accommodations/{bookingId}")]
        [ProducesResponseType(typeof(List<AccommodationBookingInfo>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetBookingById(int bookingId)
        {
            var (_, isFailure, bookingData, error) = await _service.GetBooking(bookingId);
            if (isFailure)
                return BadRequest(error);

            return Ok(bookingData);
        }


        /// <summary>
        /// The method gets all booking data for a current customer.
        /// </summary>
        /// <returns>Slim booking data.</returns>
        [ProducesResponseType(typeof(List<AccommodationBookingInfoSlim>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [HttpGet("bookings/accommodations")]
        public async Task<IActionResult> GetAllBookings()
        {
            var (_, isFailure, bookingData, error) = await _service.GetAllBookings();
            if (isFailure)
                return BadRequest(error);

            return Ok(bookingData);
        }


        /// <summary>
        ///     The route returns the availability information from the cache before the booking request.
        /// </summary>
        /// <param name="availabilityId">Cached availability id.</param>
        /// <param name="agreementId">Cached agreement id, e.g. 0f8fad5b-d9cb-469f-a165-70867728950e.</param>
        /// <returns></returns>
        [HttpGet("availabilities/{availabilityId}/{agreementId}")]
        [ProducesResponseType(typeof(BookingAvailabilityInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetBookingAvailability([FromRoute] int availabilityId, [FromRoute] Guid agreementId)
        {
            var (_, isFailure, availabilityInfo, error) = await _service.GetBookingAvailability(availabilityId, agreementId, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(availabilityInfo);
        }


        private readonly IAccommodationService _service;
    }
}