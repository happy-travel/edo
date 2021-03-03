using System;
using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.EdoContracts.General.Enums;

namespace HappyTravel.Edo.Api.Models.Reports.DirectConnectivityReports
{
    public readonly struct AgencyWiseRecordProjection
    {
        public DateTime Date { get; init; }
        public string ReferenceCode { get; init; }
        public string InvoiceNumber { get; init; }
        public string AgencyName { get; init; }
        public PaymentMethods PaymentMethod { get; init; }
        public string GuestName { get; init; }
        public string AccommodationName { get; init; }
        public List<BookedRoom> Rooms { get; init; }
        public DateTime ArrivalDate { get; init; }
        public DateTime DepartureDate { get; init; }
        public decimal TotalAmount { get; init; }
        public string ConfirmationNumber { get; init; }
        public BookingPaymentStatuses PaymentStatus { get; init; }
    }
}