using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.BatchProcessing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Edo.Data.Management;
using HappyTravel.Edo.UnitTests.Utility;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Bookings.BookingsProcessingServiceTests.Capturing
{
    public class ExecutingCapturing
    {
        [Fact]
        public async Task Capture_valid_bookings_should_succeed()
        {
            var service = CreateProcessingService();

            var (isSuccess, _, _, _) = await service.Capture(new List<int> {1, 2}, ServiceAccount);

            Assert.True(isSuccess);
        }
        
        
        [Fact]
        public async Task Capture_invalid_booking_should_fail()
        {
            var service = CreateProcessingService();

            var (_, isFailure, _, _) = await service.Capture(new List<int> {4, 5}, ServiceAccount);

            Assert.True(isFailure);
        }
        
        
        [Fact]
        public async Task All_bookings_should_be_captured()
        {
            var bookingServiceMock = new Mock<IBookingCreditCardPaymentService>();
            var service = CreateProcessingService(creditCardPaymentService: bookingServiceMock.Object);

            await service.Capture(new List<int> {1, 2, 3}, ServiceAccount);

            bookingServiceMock
                .Verify(
                    b => b.Capture(It.IsAny<Booking>(), It.IsAny<ApiCaller>()),
                    Times.Exactly(3)
                );
        }


        private BookingsProcessingService CreateProcessingService(IBookingAccountPaymentService? accountPaymentService = null,
            IBookingCreditCardPaymentService? creditCardPaymentService = null)
        {
            var context = MockEdoContextFactory.Create();
            context.Setup(c => c.Bookings)
                .Returns(DbSetMockProvider.GetDbSetMock(Bookings));
            
            var service = new BookingsProcessingService(accountPaymentService ?? Mock.Of<IBookingAccountPaymentService>(),
                creditCardPaymentService ?? Mock.Of<IBookingCreditCardPaymentService>(),
                Mock.Of<ISupplierBookingManagementService>(), 
                Mock.Of<IBookingNotificationService>(),
                Mock.Of<IBookingReportsService>(),
                context.Object,
                Mock.Of<IBookingRecordsUpdater>(),
                Mock.Of<IDateTimeProvider>());
            return service;
        }


        private static readonly ServiceAccount ServiceAccount = new ServiceAccount { ClientId = "ClientId", Id = 11};

        private static readonly Booking[] Bookings =
        {
            new Booking
            {
                Id = 1, 
                PaymentStatus = BookingPaymentStatuses.Authorized, 
                ReferenceCode = "NNN-222",
                Status = BookingStatuses.Pending,
                PaymentType = PaymentTypes.CreditCard,
                CheckInDate = new DateTime(2021, 12, 10)
            },
            new Booking
            {
                Id = 2, 
                PaymentStatus = BookingPaymentStatuses.Authorized,
                ReferenceCode = "NNN-223", 
                Status = BookingStatuses.Confirmed, 
                PaymentType = PaymentTypes.CreditCard,
                CheckInDate = new DateTime(2021, 12, 11),
                DeadlineDate = new DateTime(2021, 12, 9)
            },
            new Booking
            {
                Id = 3,
                PaymentStatus = BookingPaymentStatuses.Authorized,
                ReferenceCode = "NNN-224",
                Status = BookingStatuses.Confirmed,
                PaymentType = PaymentTypes.CreditCard,
                CheckInDate = new DateTime(2021, 12, 10),
                DeadlineDate = new DateTime(2021, 11, 9)
            },
            new Booking
            {
                Id = 4,
                PaymentStatus = BookingPaymentStatuses.Authorized,
                ReferenceCode = "NNN-224",
                Status = BookingStatuses.Cancelled,
                PaymentType = PaymentTypes.CreditCard,
                CheckInDate = new DateTime(2021, 12, 20)
            },
            new Booking
            {
                Id = 5,
                PaymentStatus = BookingPaymentStatuses.Authorized,
                ReferenceCode = "NNN-224",
                Status = BookingStatuses.Rejected,
                PaymentType = PaymentTypes.CreditCard,
                CheckInDate = new DateTime(2022, 12, 20)
            }
        };
        
        
    }
}