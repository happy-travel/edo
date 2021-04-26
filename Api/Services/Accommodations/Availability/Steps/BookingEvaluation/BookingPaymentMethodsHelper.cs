using System;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.AgencySettings;
using HappyTravel.Edo.Data.Agents;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation
{
    public static class BookingPaymentMethodsHelper
    {
        public static List<PaymentTypes> GetAvailablePaymentTypes(in EdoContracts.Accommodations.RoomContractSetAvailability availability,
            AccommodationBookingSettings settings, CounterpartyContractKind contractKind, DateTime date)
            => AllAvailablePaymentMethods
                .Intersect(GetAprPaymentMethods(availability, settings))
                .Intersect(GetPassedDeadlinePaymentMethods(availability, settings, date))
                .Intersect(GetContractKindPaymentMethods(contractKind))
                .ToList();


        public static PaymentTypes GetDefaultPaymentType(CounterpartyContractKind? contractKind)
            => contractKind switch
            {
                CounterpartyContractKind.CashPayments => PaymentTypes.Offline,
                CounterpartyContractKind.CreditCardPayments => PaymentTypes.CreditCard,
                CounterpartyContractKind.CreditPayments => PaymentTypes.VirtualAccount,
                _ => PaymentTypes.None
            };


        private static List<PaymentTypes> GetAprPaymentMethods(in EdoContracts.Accommodations.RoomContractSetAvailability availability,
            AccommodationBookingSettings settings)
        {
            if (!availability.RoomContractSet.IsAdvancePurchaseRate)
                return AllAvailablePaymentMethods;

            return settings.AprMode switch
            {
                AprMode.Hide => EmptyPaymentMethodsList,
                AprMode.DisplayOnly => EmptyPaymentMethodsList,
                AprMode.CardPurchasesOnly => new List<PaymentTypes> {PaymentTypes.CreditCard},
                AprMode.CardAndAccountPurchases => new List<PaymentTypes> {PaymentTypes.VirtualAccount, PaymentTypes.CreditCard},
                _ => throw new ArgumentOutOfRangeException(nameof(settings.AprMode), $"Invalid value {settings.AprMode}")
            };
        }


        private static List<PaymentTypes> GetPassedDeadlinePaymentMethods(in EdoContracts.Accommodations.RoomContractSetAvailability availability,
            AccommodationBookingSettings settings, DateTime date)
        {
            var deadlineDate = availability.RoomContractSet.Deadline.Date ?? availability.CheckInDate;
            if (date.AddDays(1) <= deadlineDate.Date)
                return AllAvailablePaymentMethods;

            return settings.PassedDeadlineOffersMode switch
            {
                PassedDeadlineOffersMode.Hide => EmptyPaymentMethodsList,
                PassedDeadlineOffersMode.DisplayOnly => EmptyPaymentMethodsList,
                PassedDeadlineOffersMode.CardPurchasesOnly => new List<PaymentTypes> {PaymentTypes.CreditCard},
                PassedDeadlineOffersMode.CardAndAccountPurchases => new List<PaymentTypes> {PaymentTypes.VirtualAccount, PaymentTypes.CreditCard},
                _ => throw new ArgumentOutOfRangeException(nameof(settings.PassedDeadlineOffersMode), $"Invalid value {settings.PassedDeadlineOffersMode}")
            };
        }


        private static List<PaymentTypes> GetContractKindPaymentMethods(CounterpartyContractKind contractKind)
        {
            return contractKind switch
            {
                CounterpartyContractKind.CashPayments => new List<PaymentTypes> { PaymentTypes.Offline, PaymentTypes.CreditCard },
                CounterpartyContractKind.CreditCardPayments => new List<PaymentTypes> { PaymentTypes.Offline, PaymentTypes.CreditCard },
                CounterpartyContractKind.CreditPayments => new List<PaymentTypes> { PaymentTypes.VirtualAccount, PaymentTypes.CreditCard },
                _ => throw new ArgumentOutOfRangeException(nameof(contractKind), $"Invalid value {contractKind}")
            };
        }


        private static readonly List<PaymentTypes> AllAvailablePaymentMethods = new()
        {
            PaymentTypes.VirtualAccount,
            PaymentTypes.CreditCard,
            PaymentTypes.Offline
        };

        private static readonly List<PaymentTypes> EmptyPaymentMethodsList = new(0);
    }
}