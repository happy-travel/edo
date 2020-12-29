using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Mailing;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Data.Documents;
using HappyTravel.Formatters;
using Microsoft.Extensions.Options;
using MoneyFormatter = HappyTravel.Formatters.MoneyFormatter;
using EnumFormatters = HappyTravel.Formatters.EnumFormatters;
    
namespace HappyTravel.Edo.Api.Services.Payments
{
    public class PaymentNotificationService : IPaymentNotificationService
    {
        public PaymentNotificationService(MailSenderWithCompanyInfo mailSender, IOptions<PaymentNotificationOptions> options)
        {
            _mailSender = mailSender;
            _options = options.Value;
        }


        public Task<Result> SendReceiptToCustomer((DocumentRegistrationInfo RegistrationInfo, PaymentReceipt Data) receipt, string email)
        {
            var (registrationInfo, paymentReceipt) = receipt;

            var payload = new PaymentReceiptData
            {
                Date = FormatDate(registrationInfo.Date),
                Number = registrationInfo.Number,
                CustomerName = paymentReceipt.CustomerName,
                Amount = MoneyFormatter.ToCurrencyString(paymentReceipt.Amount, paymentReceipt.Currency),
                Method = EnumFormatters.FromDescription(paymentReceipt.Method),
                InvoiceNumber = paymentReceipt.InvoiceInfo.Number,
                InvoiceDate = FormatDate(paymentReceipt.InvoiceInfo.Date),
                ReferenceCode = paymentReceipt.ReferenceCode,
                AccommodationName = paymentReceipt.AccommodationName,
                RoomTypes = paymentReceipt.ReceiptItems.Select(r => new PaymentReceiptData.RoomDetails
                {
                    DeadlineDate = r.DeadlineDate,
                    RoomType = r.RoomType
                }).ToList(),
                CheckInDate = DateTimeFormatters.ToDateString(paymentReceipt.CheckInDate),
                CheckOutDate = DateTimeFormatters.ToDateString(paymentReceipt.CheckOutDate),
                BuyerInformation = new PaymentReceiptData.Buyer
                {
                    Address = paymentReceipt.BuyerDetails.Address,
                    ContactPhone = paymentReceipt.BuyerDetails.ContactPhone,
                    Email = paymentReceipt.BuyerDetails.Email,
                    Name = paymentReceipt.BuyerDetails.Name
                },
                DeadlineDate = DateTimeFormatters.ToDateString(paymentReceipt.DeadlineDate)
            };

            return _mailSender.Send(_options.ReceiptTemplateId, email, payload);
        }

        private static string FormatDate(DateTime date) => date.ToString("dd-MMM-yy");

        private readonly MailSenderWithCompanyInfo _mailSender;
        private readonly PaymentNotificationOptions _options;
    }
}