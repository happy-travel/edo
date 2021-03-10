﻿using System;
using System.Collections.Generic;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation;
using HappyTravel.Edo.Common.Enums.AgencySettings;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General.Enums;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Availability.Steps.BookingEvaluation
{
    public class BookingPaymentMethodsHelperTests
    {
        [Fact]
        public void Hidden_apr_should_return_no_payment_methods()
        {
            var availability = CreateAvailability(
                isApr: true,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.Hide);
            
            var availablePaymentMethods = BookingPaymentMethodsHelper.GetAvailablePaymentMethods(availability, settingsWithHiddenApr, new DateTime(2020, 11 ,11));
            
            Assert.Equal(new List<PaymentMethods>(), availablePaymentMethods);
        }

        
        [Fact]
        public void Display_only_apr_should_return_no_payment_methods()
        {
            var availability = CreateAvailability(
                isApr: true,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.DisplayOnly);
            
            var availablePaymentMethods = BookingPaymentMethodsHelper.GetAvailablePaymentMethods(availability, settingsWithHiddenApr, new DateTime(2020, 11 ,11));
            
            Assert.Equal(new List<PaymentMethods>(), availablePaymentMethods);
        }
        
        
        [Fact]
        public void Card_only_apr_should_return_card_payment_method_when_deadline_is_not_reached()
        {
            var availability = CreateAvailability(
                isApr: true,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.CardPurchasesOnly);
            
            var availablePaymentMethods = BookingPaymentMethodsHelper
                .GetAvailablePaymentMethods(availability, settingsWithHiddenApr, new DateTime(2020, 11 ,15));
            
            Assert.Equal(new List<PaymentMethods> {PaymentMethods.CreditCard}, availablePaymentMethods);
        }
        
        
        [Fact]
        public void Card_only_apr_should_return_no_payment_methods_when_deadline_is_reached()
        {
            var availability = CreateAvailability(
                isApr: true,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.CardPurchasesOnly);
            
            var availablePaymentMethods = BookingPaymentMethodsHelper
                .GetAvailablePaymentMethods(availability, settingsWithHiddenApr, new DateTime(2020, 11 ,23));
            
            Assert.Equal(new List<PaymentMethods>(), availablePaymentMethods);
        }
        
        
        [Fact]
        public void Card_and_account_apr_should_return_no_payment_methods_when_deadline_is_reached()
        {
            var availability = CreateAvailability(
                isApr: true,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.CardAndAccountPurchases);
            
            var availablePaymentMethods = BookingPaymentMethodsHelper
                .GetAvailablePaymentMethods(availability, settingsWithHiddenApr, new DateTime(2020, 11 ,23));
            
            Assert.Equal(new List<PaymentMethods>(), availablePaymentMethods);
        }
        
        
        [Fact]
        public void Not_apr_should_return_all_payment_methods_when_deadline_is_not_reached()
        {
            var availability = CreateAvailability(
                isApr: false,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.CardAndAccountPurchases);
            
            var availablePaymentMethods = BookingPaymentMethodsHelper
                .GetAvailablePaymentMethods(availability, settingsWithHiddenApr, new DateTime(2020, 11 ,15));
            
            Assert.Equal(new List<PaymentMethods> {PaymentMethods.BankTransfer, PaymentMethods.CreditCard}, availablePaymentMethods);
        }
        
        
        [Fact]
        public void Not_apr_should_return_no_payment_methods_when_deadline_is_not_reached_and_hidden()
        {
            var availability = CreateAvailability(
                isApr: false,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.Hide, deadlineOffersMode: PassedDeadlineOffersMode.Hide);
            
            var availablePaymentMethods = BookingPaymentMethodsHelper
                .GetAvailablePaymentMethods(availability, settingsWithHiddenApr, new DateTime(2020, 11 ,22));
            
            Assert.Equal(new List<PaymentMethods>(), availablePaymentMethods);
        }
        
        
        [Fact]
        public void Not_apr_should_return_credit_card_when_deadline_is_reached_and_allowed_credit_card()
        {
            var availability = CreateAvailability(
                isApr: false,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.Hide, deadlineOffersMode: PassedDeadlineOffersMode.CardPurchasesOnly);
            
            var availablePaymentMethods = BookingPaymentMethodsHelper
                .GetAvailablePaymentMethods(availability, settingsWithHiddenApr, new DateTime(2020, 11 ,22));
            
            Assert.Equal(new List<PaymentMethods> {PaymentMethods.CreditCard}, availablePaymentMethods);
        }
        
        
        [Fact]
        public void Allowed_apr_should_return_all_payment_methods_when_deadline_is_reached_and_allowed()
        {
            var availability = CreateAvailability(
                isApr: true,
                deadlineDate: new DateTime(2020, 11, 22),
                checkInDate: new DateTime(2020, 11, 25));
            var settingsWithHiddenApr = CreateSettings(aprMode: AprMode.CardAndAccountPurchases, deadlineOffersMode: PassedDeadlineOffersMode.CardAndAccountPurchases);
            
            var availablePaymentMethods = BookingPaymentMethodsHelper
                .GetAvailablePaymentMethods(availability, settingsWithHiddenApr, new DateTime(2020, 11 ,22));
            
            Assert.Equal(new List<PaymentMethods> {PaymentMethods.BankTransfer, PaymentMethods.CreditCard}, availablePaymentMethods);
        }
        

        private static AccommodationBookingSettings CreateSettings(AprMode aprMode = default, PassedDeadlineOffersMode deadlineOffersMode = default)
            => new(default, aprMode,
                deadlineOffersMode, default, default, default, default);


        private static RoomContractSetAvailability CreateAvailability(bool isApr = false, DateTime? checkInDate = null, DateTime? deadlineDate = null)
        {
            var deadline = new Deadline(deadlineDate);
            var roomContractSetWithApr = new RoomContractSet(default, default, deadline, default,
                default, isAdvancePurchaseRate: isApr);

            return new RoomContractSetAvailability(default, checkInDate ?? default,
                default, default, default, roomContractSetWithApr);
        }
    }
}