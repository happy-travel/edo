using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Branches;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.Management.AuditEvents;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Customers;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Customers
{
    public class CompanyService : ICompanyService
    {
        public CompanyService(EdoContext context, 
            IAccountManagementService accountManagementService,
            IAdministratorContext administratorContext,
            IDateTimeProvider dateTimeProvider,
            IManagementAuditService managementAuditService,
            ICustomerContext customerContext)
        {
            _context = context;
            _accountManagementService = accountManagementService;
            _administratorContext = administratorContext;
            _dateTimeProvider = dateTimeProvider;
            _managementAuditService = managementAuditService;
            _customerContext = customerContext;
        }

        public async Task<Result<Company>> Create(CompanyRegistrationInfo company)
        {
            var (_, isFailure, error) = Validate(company);
            if (isFailure)
                return Result.Fail<Company>(error);

            var now = _dateTimeProvider.UtcNow();
            var createdCompany = new Company
            {
                Address = company.Address,
                City = company.City,
                CountryCode = company.CountryCode,
                Fax = company.Fax,
                Name = company.Name,
                Phone = company.Phone,
                Website = company.Website,
                PostalCode = company.PostalCode,
                PreferredCurrency = company.PreferredCurrency,
                PreferredPaymentMethod = company.PreferredPaymentMethod,
                State = CompanyStates.PendingVerification,
                Created = now,
                Updated = now
            };
            
            _context.Companies.Add(createdCompany);
            await _context.SaveChangesAsync();

            return Result.Ok(createdCompany);
        }


        public Task<Result<Branch>> CreateBranch(int companyId, BranchInfo branch)
        {
            return CheckPermissions()
                .Ensure(CompanyExists, $"Could not find company with id {companyId}")
                .OnSuccess(SaveBranch);

            async Task<Result> CheckPermissions()
            {
                var (_, isFailure, customerInfo, error) = await _customerContext.GetCustomerInfo();
                if (isFailure)
                    return Result.Fail(error);

                return customerInfo.IsMaster && customerInfo.Company.Id == companyId
                    ? Result.Ok()
                    : Result.Fail("Permission denied");
            }

            Task<bool> CompanyExists()
            {
                return _context.Companies.AnyAsync(c => c.Id == companyId);
            }

            async Task<Branch> SaveBranch()
            {
                var createdBranch = new Branch {Title = branch.Title, CompanyId = companyId};
                _context.Branches.Add(createdBranch);
                await _context.SaveChangesAsync();
                
                return createdBranch;
            }
        }

        public Task<Result> SetVerified(int companyId, string verifyReason)
        {
            var now = _dateTimeProvider.UtcNow();
            return Result.Ok()
                .Ensure(HasVerifyRights, "Permission denied")
                .OnSuccess(GetCompany)
                .OnSuccessWithTransaction(_context, company => Result.Ok(company)
                    .OnSuccess(SetCompanyVerified)
                    .OnSuccess(CreatePaymentAccount)
                    .OnSuccess((WriteAuditLog)));
            
            Task<bool> HasVerifyRights()
            {
                return _administratorContext.HasPermission(AdministratorPermissions.CompanyVerification);
            }
            
            async Task<Result<Company>> GetCompany()
            {
                var company = await _context.Companies.SingleOrDefaultAsync(c => c.Id == companyId);
                return company == default
                    ? Result.Fail<Company>($"Could not find company with id {companyId}")
                    : Result.Ok(company);
            }

            Task SetCompanyVerified(Company company)
            {
                company.State = CompanyStates.Verified;
                company.VerificationReason = verifyReason;
                company.Verified = now;
                company.Updated = now;
                _context.Update(company);
                return _context.SaveChangesAsync();
            }
            
            Task<Result> CreatePaymentAccount(Company company)
            {
                return _accountManagementService
                    .Create(company, company.PreferredCurrency);
            }
            
            Task WriteAuditLog()
            {
                return _managementAuditService.Write(ManagementEventType.CompanyVerification, 
                    new CompanyVerifiedAuditEventData(companyId, verifyReason));
            }
        }

        private Result Validate(in CompanyRegistrationInfo companyRegistration)
        {
            return GenericValidator<CompanyRegistrationInfo>.Validate(v =>
            {
                v.RuleFor(c => c.Name).NotEmpty();
                v.RuleFor(c => c.Address).NotEmpty();
                v.RuleFor(c => c.City).NotEmpty();
                v.RuleFor(c => c.Phone).NotEmpty().Matches(@"^[0-9]{3,30}$");
                v.RuleFor(c => c.Fax).Matches(@"^[0-9]{3,30}$").When(i => !string.IsNullOrWhiteSpace(i.Fax));
            }, companyRegistration);
        }
        
        private readonly EdoContext _context;
        private readonly IAccountManagementService _accountManagementService;
        private readonly IAdministratorContext _administratorContext;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IManagementAuditService _managementAuditService;
        private readonly ICustomerContext _customerContext;
    }
}