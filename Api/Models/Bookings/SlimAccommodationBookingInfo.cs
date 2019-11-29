﻿using System;
using HappyTravel.Edo.Api.Services.Accommodations;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.General;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Bookings
{
    public struct SlimAccommodationBookingInfo
    {
        public SlimAccommodationBookingInfo(Booking bookingInfo)
        {
            var serviceDetails = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(bookingInfo.ServiceDetails);
            var bookingDetails = JsonConvert.DeserializeObject<AccommodationBookingDetails>(bookingInfo.BookingDetails);

            Id = bookingInfo.Id;
            AccommodationName = serviceDetails.AccommodationName;
            CountryName = serviceDetails.CountryName;
            LocalityName = serviceDetails.CityName;
            DeadlineDetails = serviceDetails.DeadlineDetails;
            BoardBasisCode = serviceDetails.Agreement.BoardBasisCode;
            BoardBasis = serviceDetails.Agreement.BoardBasis;
            Price = serviceDetails.Agreement.Price;
            CheckInDate = bookingDetails.CheckInDate;
            CheckOutDate = bookingDetails.CheckOutDate;
            Status = bookingDetails.Status;
        }


        public int Id { get; }

        public BookingStatusCodes Status { get; }
        
        public Price Price { get; }

        public string BoardBasisCode { get; }

        public string BoardBasis { get; }
        
        public DateTime CheckOutDate { get; }
        
        public DateTime CheckInDate { get; }
        
        public string LocalityName { get; }
        
        public string CountryName { get; }
        
        public string AccommodationName { get; }
        
        public DeadlineDetails DeadlineDetails { get; }
    }
}