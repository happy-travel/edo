﻿using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Invitations;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Infrastructure.Invitations
{
    public class InvitationRecordService : IInvitationRecordService
    {
        public InvitationRecordService(
            IDateTimeProvider dateTimeProvider,
            EdoContext context)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }


        public async Task<Result> Revoke(string codeHash)
        {
            return await GetActiveInvitationByHash(codeHash)
                .Tap(SaveRevoked);


            Task SaveRevoked(UserInvitation invitation)
            {
                invitation.InvitationStatus = UserInvitationStatuses.Revoked;
                _context.Update(invitation);
                return _context.SaveChangesAsync();
            }
        }


        public async Task<Result> SetToResent(string codeHash)
        {
            return await GetActiveInvitationByHash(codeHash)
                .Tap(SaveResent);


            Task SaveResent(UserInvitation oldInvitation)
            {
                oldInvitation.InvitationStatus = UserInvitationStatuses.Resent;
                _context.Update(oldInvitation);
                return _context.SaveChangesAsync();
            }
        }


        public async Task<Result> SetAccepted(string code)
        {
            return await GetActiveInvitationByCode(code)
                .Tap(SaveAccepted);


            Task SaveAccepted(UserInvitation invitation)
            {
                invitation.InvitationStatus = UserInvitationStatuses.Accepted;
                _context.Update(invitation);
                return _context.SaveChangesAsync();
            }
        }


        public Task<Result<UserInvitation>> GetActiveInvitationByHash(string codeHash)
        {
            return GetInvitation()
                .Ensure(InvitationIsActual, "Invitation expired");


            async Task<Result<UserInvitation>> GetInvitation()
            {
                var invitation = await _context.UserInvitations
                    .SingleOrDefaultAsync(i
                        => i.CodeHash == codeHash
                        && i.InvitationStatus == UserInvitationStatuses.Active);

                return invitation ?? Result.Failure<UserInvitation>("Invitation with specified code either does not exist, or is not active.");
            }


            bool InvitationIsActual(UserInvitation invitation)
                => !invitation.IsExpired(_dateTimeProvider.UtcNow());
        }


        public Task<Result<UserInvitation>> GetActiveInvitationByCode(string code)
            => GetActiveInvitationByHash(HashGenerator.ComputeSha256(code));


        public UserInvitationData GetInvitationData(UserInvitation invitation)
            => JsonConvert.DeserializeObject<UserInvitationData>(invitation.Data);


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}
