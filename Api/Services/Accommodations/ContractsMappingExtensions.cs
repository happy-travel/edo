using System;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public static class ContractsMappingExtensions
    {
        public static RoomContractSet ToRoomContractSet(this in EdoContracts.Accommodations.Internals.RoomContractSet roomContractSet, Suppliers? supplier, List<string> tags)
        {
            var roomContractList = roomContractSet.RoomContracts.ToRoomContractList();
            
            return new RoomContractSet(roomContractSet.Id,
                roomContractSet.Rate.ToRate(),
                roomContractList.ToRoomContractSetDeadline(),
                roomContractList,
                roomContractSet.IsAdvancePurchaseRate,
                supplier,
                tags,
                roomContractSet.IsDirectContract);
        }

        
        public static Deadline ToDeadline(this in EdoContracts.Accommodations.Deadline deadline)
        {
            return new (deadline.Date, deadline.Policies.ToPolicyList(), deadline.Remarks, deadline.IsFinal);
        }


        private static List<CancellationPolicy> ToPolicyList(this IEnumerable<EdoContracts.Accommodations.Internals.CancellationPolicy> policies)
        {
            return policies
                .Select(ToCancellationPolicy)
                .ToList();
        }
        

        private static RoomContract ToRoomContract(this EdoContracts.Accommodations.Internals.RoomContract roomContract)
        {
            return new RoomContract(roomContract.BoardBasis, roomContract.MealPlan,
                roomContract.ContractTypeCode, roomContract.IsAvailableImmediately, roomContract.IsDynamic,
                roomContract.ContractDescription, roomContract.Remarks, roomContract.DailyRoomRates.ToDailyRateList(), roomContract.Rate.ToRate(),
                roomContract.AdultsNumber, roomContract.ChildrenAges, roomContract.Type, roomContract.IsExtraBedNeeded,
                roomContract.Deadline.ToDeadline(), roomContract.IsAdvancePurchaseRate);
        }
        
        
        private static Rate ToRate(this EdoContracts.General.Rate rate)
        {
            return new (rate.FinalPrice, rate.Gross, rate.Discounts, rate.Type, rate.Description);
        }


        private static CancellationPolicy ToCancellationPolicy(EdoContracts.Accommodations.Internals.CancellationPolicy policy)
        {
            return new (policy.FromDate, policy.Percentage);
        }
        
        
        private static List<DailyRate> ToDailyRateList(this IEnumerable<EdoContracts.General.DailyRate> rates)
        {
            return rates
                .Select(r => new DailyRate(r.FromDate,
                    r.ToDate,
                    r.FinalPrice,
                    r.Gross,
                    r.Type,
                    r.Description))
                .ToList();
        }
        
        
        private static List<RoomContract> ToRoomContractList(
            this IEnumerable<EdoContracts.Accommodations.Internals.RoomContract> roomContractSets)
        {
            return roomContractSets
                .Select(ToRoomContract)
                .ToList();
        }


        private static Deadline ToRoomContractSetDeadline(this IReadOnlyCollection<RoomContract> roomContracts)
        {
            var contractsWithDeadline = roomContracts
                .Where(contract => contract.Deadline.Date.HasValue)
                .ToList();
            
            if (!contractsWithDeadline.Any())
                return default;
            
            var totalAmount = Convert.ToDouble(roomContracts.Sum(r => r.Rate.FinalPrice.Amount));
            var deadlineDate = contractsWithDeadline
                .Select(contract => contract.Deadline.Date.Value)
                .OrderByDescending(d => d)
                .FirstOrDefault();

            var policies = contractsWithDeadline
                .SelectMany(c => c.Deadline.Policies.Select(p => p.FromDate.Date))
                .Distinct()
                .OrderBy(d => d)
                .Select(date =>
                {
                    var amount = contractsWithDeadline.Sum(contract 
                        => contract.Deadline.Policies
                            .Where(p => p.FromDate <= date)
                            .OrderByDescending(p => p.FromDate)
                            .Select(p => p.Percentage * Convert.ToDouble(contract.Rate.FinalPrice.Amount))
                            .FirstOrDefault()
                        );

                    return new CancellationPolicy(date, CalculatePercent(amount));
                })
                .ToList();


            double CalculatePercent(double amount) 
                => amount / totalAmount;

            return new Deadline(deadlineDate, policies, new List<string>(), true);
        }
    }
}