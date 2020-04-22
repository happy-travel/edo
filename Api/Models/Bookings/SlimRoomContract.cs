﻿using HappyTravel.EdoContracts.Accommodations.Internals;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Bookings
{
    public readonly struct SlimRoomContract
    {
        [JsonConstructor]
        public SlimRoomContract(RoomContract roomContract)
        {
            MealPlan = roomContract.MealPlan;
            ContractType = roomContract.ContractDescription;
            BoardBasis = roomContract.BoardBasis.ToString();
        }


        public string MealPlan { get; }
        public string ContractType { get; }
        public string BoardBasis { get; }
    }
}