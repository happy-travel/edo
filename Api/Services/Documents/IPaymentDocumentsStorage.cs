using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Documents;

namespace HappyTravel.Edo.Api.Services.Documents
{
    public interface IPaymentDocumentsStorage
    {
        Task<DocumentRegistrationInfo> Register<TPaymentDocumentEntity>(TPaymentDocumentEntity documentEntity)
            where TPaymentDocumentEntity : class, IPaymentDocumentEntity;


        public Task<List<TPaymentDocumentEntity>> Get<TPaymentDocumentEntity>(ServiceTypes serviceType,
            ServiceSource serviceSource, string referenceCode)
            where TPaymentDocumentEntity : class, IPaymentDocumentEntity;


        Task<Result<TPaymentDocumentEntity>> Get<TPaymentDocumentEntity>(int id)
            where TPaymentDocumentEntity : class, IPaymentDocumentEntity;
    }
}