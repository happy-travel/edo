using System;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Data.Documents
{
    public class Invoice : IPaymentDocumentEntity
    {
        public int Id { get; set; }
        
        public string Number { get; set; }
        public ServiceTypes ServiceType { get; set; }
        public ServiceSource ServiceSource { get; set; }

        public string ParentReferenceCode { get; set; }
        public string Data { get; set; }
        public DateTime Date { get; set; }
        
        public DocumentRegistrationInfo GetRegistrationInfo() => new DocumentRegistrationInfo(Number, Date);
    }
}