using System.ComponentModel.DataAnnotations;
using HappyTravel.Edo.Common.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Payments.External
{
    public readonly struct PaymentLinkData
    {
        [JsonConstructor]
        public PaymentLinkData(decimal amount, string email, ServiceTypes serviceType, 
            Currencies currency, string comment, string referenceCode, PaymentStatuses paymentStatus)
        {
            Amount = amount;
            Email = email;
            ServiceType = serviceType;
            Currency = currency;
            Comment = comment;
            ReferenceCode = referenceCode;
            PaymentStatus = paymentStatus;
        }


        /// <summary>
        ///     Payment price.
        /// </summary>
        [Required]
        public decimal Amount { get; }


        /// <summary>
        /// Customer e-mail.
        /// </summary>
        [Required]
        public string Email { get; }

        /// <summary>
        /// Service type to pay for.
        /// </summary>
        public ServiceTypes ServiceType { get; }


        /// <summary>
        ///     Payment currency.
        /// </summary>
        [Required]
        public Currencies Currency { get; }

        /// <summary>
        ///     Optional payment comment.
        /// </summary>
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string Comment { get; }

        /// <summary>
        /// Link reference code.
        /// </summary>
        public string ReferenceCode { get; }

        /// <summary>
        /// Payment status.
        /// </summary>
        public PaymentStatuses PaymentStatus { get; }
    }
}