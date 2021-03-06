﻿namespace HappyTravel.Edo.Api.Services.PropertyOwners
{
    public interface IUrlGenerationService
    {
        string Generate(string referenceCode);
        string ReadReferenceCode(string encryptedReferenceCode);
    }
}