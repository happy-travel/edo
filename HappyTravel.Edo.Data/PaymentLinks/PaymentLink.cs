using System;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Data.PaymentLinks
{
    public class PaymentLink
    {
        public string Code { get; set; }
        public string Email { get; set; }
        public Currencies Currency { get; set; }
        public ServiceTypes ServiceType { get; set; }
        public decimal Price { get; set; }
        public string Comment { get; set; }
        public string ReferenceCode { get; set; }
        public DateTime Created { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public string LastPaymentResponse { get; set; }
    }
}