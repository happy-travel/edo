using System.ComponentModel.DataAnnotations;
using HappyTravel.Edo.Common.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Customers
{
    public readonly struct CompanyRegistrationInfo
    {
        [JsonConstructor]
        public CompanyRegistrationInfo(string name, string address, string countryCode, string city, string phone,
            string fax, string postalCode, Currency preferredCurrency,
            PaymentMethod preferredPaymentMethod, string website)
        {
            Name = name;
            Address = address;
            CountryCode = countryCode;
            City = city;
            Phone = phone;
            Fax = fax;
            PostalCode = postalCode;
            PreferredCurrency = preferredCurrency;
            PreferredPaymentMethod = preferredPaymentMethod;
            Website = website;
        }

        /// <summary>
        ///     Company name.
        /// </summary>
        [Required]
        public string Name { get; }

        /// <summary>
        ///     Company address.
        /// </summary>
        [Required]
        public string Address { get; }

        /// <summary>
        ///     Two-letter international country code.
        /// </summary>
        [Required]
        public string CountryCode { get; }

        /// <summary>
        ///     City name.
        /// </summary>
        [Required]
        public string City { get; }

        /// <summary>
        ///     Phone number. Only digits, length between 3 and 30.
        /// </summary>
        [Required]
        [Phone]
        [RegularExpression(@"^[0-9]{3,30}$")]
        public string Phone { get; }

        /// <summary>
        ///     Fax number. Only digits, length between 3 and 30.
        /// </summary>
        [Phone]
        [RegularExpression(@"^[0-9]{3,30}$")]
        public string Fax { get; }

        /// <summary>
        ///     Postal code.
        /// </summary>
        [DataType(DataType.PostalCode)]
        public string PostalCode { get; }

        /// <summary>
        ///     Preferable payments currency.
        /// </summary>
        [Required]
        public Currency PreferredCurrency { get; }

        /// <summary>
        ///     Preferable way to do payments.
        /// </summary>
        [Required]
        public PaymentMethod PreferredPaymentMethod { get; }

        /// <summary>
        ///     Company site url.
        /// </summary>
        [Url]
        public string Website { get; }
    }
}