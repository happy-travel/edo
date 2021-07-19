using System.Collections.Generic;

namespace HappyTravel.Edo.Api.Models.Mailing
{
    public class BookingConfirmationData : DataWithCompanyInfo
    {
        public string ReferenceCode { get; set; }
        public string AccommodationName { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public List<BookedRoomDetails> RoomDetails { get; set; }
        public string BookingConfirmationPageUrl { get; set; }

        public class BookedRoomDetails
        {
            public string Type { get; set; }
            public string MainPassengers { get; set; }
            public string PromoCode { get; set; }
            public string Price { get; set; }
            public string MealPlan { get; set; }
            public string NumberOfPassengers { get; set; }
            public string ContractDescription { get; set; }
        }
    }
}