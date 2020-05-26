using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct CombinedAvailabilityDetails
    {
        [JsonConstructor]
        public CombinedAvailabilityDetails(int numberOfNights, DateTime checkInDate, DateTime checkOutDate, int numberOfProcessedResults, List<ProviderData<AvailabilityResult>> results)
        {
            CheckInDate = checkInDate;
            CheckOutDate = checkOutDate;
            NumberOfNights = numberOfNights;
            NumberOfProcessedResults = numberOfProcessedResults;
            Results = results ?? new List<ProviderData<AvailabilityResult>>(0);
        }


        public CombinedAvailabilityDetails(CombinedAvailabilityDetails details, List<ProviderData<AvailabilityResult>> results)
            : this(details.NumberOfNights, details.CheckInDate, details.CheckOutDate, details.NumberOfProcessedResults, results)
        { }

        /// <summary>
        /// Number of nights
        /// </summary>
        public int NumberOfNights { get; }
        
        /// <summary>
        /// Check-in date
        /// </summary>
        public DateTime CheckInDate { get; }
        
        /// <summary>
        /// Check-out date
        /// </summary>
        public DateTime CheckOutDate { get; }

        // TODO: Consider moving this to AvailabilitySearchState
        /// <summary>
        /// Number of all processed accommodations
        /// </summary>
        public int NumberOfProcessedResults { get; }

        /// <summary>
        /// Availability results, grouped by accommodation
        /// </summary>
        public List<ProviderData<AvailabilityResult>> Results { get; }
        
        public static CombinedAvailabilityDetails Empty => new CombinedAvailabilityDetails(default, default, default, default, default);
    }
}