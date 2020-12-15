﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using FloxDc.CacheFlow;
using HappyTravel.AmazonS3Client.Extensions;
using HappyTravel.Edo.Api.Filters.Authorization;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Filters.Authorization.AgentExistingFilters;
using HappyTravel.Edo.Api.Filters.Authorization.CounterpartyStatesFilters;
using HappyTravel.Edo.Api.Filters.Authorization.InAgencyPermissionFilters;
using HappyTravel.Edo.Api.Filters.Authorization.ServiceAccountFilters;
using HappyTravel.Edo.Api.Infrastructure.Constants;
using HappyTravel.Edo.Api.Infrastructure.Converters;
using HappyTravel.Edo.Api.Infrastructure.Environments;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Payments.External.PaymentLinks;
using HappyTravel.Edo.Api.Services.Accommodations;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Infrastructure.SupplierConnectors;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.RoomSelection;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch;
using HappyTravel.Edo.Api.Services.Accommodations.Mappings;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.CodeProcessors;
using HappyTravel.Edo.Api.Services.Company;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Api.Services.CurrencyConversion;
using HappyTravel.Edo.Api.Services.Documents;
using HappyTravel.Edo.Api.Services.Locations;
using HappyTravel.Edo.Api.Services.Mailing;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.Markups;
using HappyTravel.Edo.Api.Services.Markups.Templates;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Api.Services.Payments.CreditCards;
using HappyTravel.Edo.Api.Services.Payments.External;
using HappyTravel.Edo.Api.Services.Payments.External.PaymentLinks;
using HappyTravel.Edo.Api.Services.Payments.Offline;
using HappyTravel.Edo.Api.Services.Payments.Payfort;
using HappyTravel.Edo.Api.Services.ProviderResponses;
using HappyTravel.Edo.Api.Services.SupplierOrders;
using HappyTravel.Edo.Api.Services.Users;
using HappyTravel.Edo.Api.Services.Versioning;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Geography;
using HappyTravel.MailSender;
using HappyTravel.MailSender.Infrastructure;
using HappyTravel.MailSender.Models;
using HappyTravel.Money.Enums;
using HappyTravel.VaultClient;
using IdentityModel;
using IdentityServer4.AccessTokenValidation;
using LocationNameNormalizer.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetTopologySuite;
using Newtonsoft.Json;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;
using Amazon;
using Amazon.S3;
using Elasticsearch.Net;
using HappyTravel.CurrencyConverter.Extensions;
using HappyTravel.CurrencyConverter.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Analytics;
using HappyTravel.Edo.Api.Services.Files;
using Prometheus;

namespace HappyTravel.Edo.Api.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration,
            IWebHostEnvironment environment, IVaultClient vaultClient)
        {
            var (apiName, authorityUrl) = GetApiNameAndAuthority(configuration, environment, vaultClient);

            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = authorityUrl;
                    options.ApiName = apiName;
                    options.RequireHttpsMetadata = true;
                    options.SupportedTokens = SupportedTokens.Jwt;
                });

            return services;
        }


        public static IServiceCollection ConfigureHttpClients(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment,
            IVaultClient vaultClient)
        {
            var (_, authorityUrl) = GetApiNameAndAuthority(configuration, environment, vaultClient);
            services.AddHttpClient(HttpClientNames.Identity, client => client.BaseAddress = new Uri(authorityUrl));

            services.AddHttpClient(HttpClientNames.GoogleMaps, c => { c.BaseAddress = new Uri(configuration["Edo:Google:Endpoint"]); })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetDefaultRetryPolicy());

            services.AddHttpClient(SendGridMailSender.HttpClientName)
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetDefaultRetryPolicy());

            services.AddHttpClient(HttpClientNames.Payfort)
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetDefaultRetryPolicy());

            services.AddHttpClient(HttpClientNames.CurrencyService)
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetDefaultRetryPolicy());

            services.AddHttpClient(HttpClientNames.Connectors)
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .UseHttpClientMetrics();

            return services;
        }


        public static IServiceCollection ConfigureServiceOptions(this IServiceCollection services, IConfiguration configuration,
            IWebHostEnvironment environment, VaultClient.VaultClient vaultClient)
        {
            #region mailing setting

            var mailSettings = vaultClient.Get(configuration["Edo:Email:Options"]).GetAwaiter().GetResult();
            var edoPublicUrl = mailSettings[configuration["Edo:Email:EdoPublicUrl"]];

            var sendGridApiKey = mailSettings[configuration["Edo:Email:ApiKey"]];
            var senderAddress = mailSettings[configuration["Edo:Email:SenderAddress"]];
            services.Configure<SenderOptions>(options =>
            {
                options.ApiKey = sendGridApiKey;
                options.BaseUrl = new Uri(edoPublicUrl);
                options.SenderAddress = new EmailAddress(senderAddress);
            });

            var agentInvitationTemplateId = mailSettings[configuration["Edo:Email:AgentInvitationTemplateId"]];
            services.Configure<AgentInvitationOptions>(options =>
            {
                options.MailTemplateId = agentInvitationTemplateId;
                options.EdoPublicUrl = edoPublicUrl;
            });

            var administratorInvitationTemplateId = mailSettings[configuration["Edo:Email:AdministratorInvitationTemplateId"]];
            services.Configure<AdministratorInvitationOptions>(options =>
            {
                options.MailTemplateId = administratorInvitationTemplateId;
                options.EdoPublicUrl = edoPublicUrl;
            });
            services.Configure<UserInvitationOptions>(options =>
                options.InvitationExpirationPeriod = TimeSpan.FromDays(7));

            var administrators = JsonConvert.DeserializeObject<List<string>>(mailSettings[configuration["Edo:Email:Administrators"]]);
            var masterAgentRegistrationMailTemplateId = mailSettings[configuration["Edo:Email:MasterAgentRegistrationTemplateId"]];
            var regularAgentRegistrationMailTemplateId = mailSettings[configuration["Edo:Email:RegularAgentRegistrationTemplateId"]];
            services.Configure<AgentRegistrationNotificationOptions>(options =>
            {
                options.AdministratorsEmails = administrators;
                options.MasterAgentMailTemplateId = masterAgentRegistrationMailTemplateId;
                options.RegularAgentMailTemplateId = regularAgentRegistrationMailTemplateId;
            });

            var bookingVoucherTemplateId = mailSettings[configuration["Edo:Email:BookingVoucherTemplateId"]];
            var bookingInvoiceTemplateId = mailSettings[configuration["Edo:Email:BookingInvoiceTemplateId"]];
            var bookingCancelledTemplateId = mailSettings[configuration["Edo:Email:BookingCancelledTemplateId"]];
            var bookingFinalizedTemplateId = mailSettings[configuration["Edo:Email:BookingFinalizedTemplateId"]];
            var bookingDeadlineNotificationTemplateId = mailSettings[configuration["Edo:Email:BookingDeadlineNotificationTemplateId"]];
            var reservationsBookingFinalizedTemplateId = mailSettings[configuration["Edo:Email:ReservationsBookingFinalizedTemplateId"]];
            var reservationsBookingCancelledTemplateId = mailSettings[configuration["Edo:Email:ReservationsBookingCancelledTemplateId"]];
            var bookingSummaryTemplateId = mailSettings[configuration["Edo:Email:BookingSummaryTemplateId"]];
            var bookingAdministratorSummaryTemplateId = mailSettings[configuration["Edo:Email:BookingAdministratorSummaryTemplateId"]];
            var bookingPaymentsSummaryTemplateId = mailSettings[configuration["Edo:Email:BookingAdministratorPaymentsSummaryTemplateId"]];
            var ccNotificationAddresses = JsonConvert.DeserializeObject<List<string>>(mailSettings[configuration["Edo:Email:CcNotificationAddresses"]]);
            var adminCreditCardPaymentConfirmationTemplateId = mailSettings[configuration["Edo:Email:AdminCreditCardPaymentConfirmationTemplateId"]];
            var agentCreditCardPaymentConfirmationTemplateId = mailSettings[configuration["Edo:Email:AgentCreditCardPaymentConfirmationTemplateId"]];
            services.Configure<BookingMailingOptions>(options =>
            {
                options.VoucherTemplateId = bookingVoucherTemplateId;
                options.InvoiceTemplateId = bookingInvoiceTemplateId;
                options.BookingCancelledTemplateId = bookingCancelledTemplateId;
                options.BookingFinalizedTemplateId = bookingFinalizedTemplateId;
                options.DeadlineNotificationTemplateId = bookingDeadlineNotificationTemplateId;
                options.ReservationsBookingFinalizedTemplateId = reservationsBookingFinalizedTemplateId;
                options.ReservationsBookingCancelledTemplateId = reservationsBookingCancelledTemplateId;
                options.CcNotificationAddresses = ccNotificationAddresses;
                options.BookingSummaryTemplateId = bookingSummaryTemplateId;
                options.BookingAdministratorPaymentsSummaryTemplateId = bookingPaymentsSummaryTemplateId;
                options.BookingAdministratorSummaryTemplateId = bookingAdministratorSummaryTemplateId;
                options.AdminCreditCardPaymentConfirmationTemplateId = adminCreditCardPaymentConfirmationTemplateId;
                options.AgentCreditCardPaymentConfirmationTemplateId = agentCreditCardPaymentConfirmationTemplateId;
            });

            var receiptTemplateId = mailSettings[configuration["Edo:Email:KnownCustomerReceiptTemplateId"]];
            services.Configure<PaymentNotificationOptions>(po => { po.ReceiptTemplateId = receiptTemplateId; });

            #endregion

            var databaseOptions = vaultClient.Get(configuration["Edo:Database:Options"]).GetAwaiter().GetResult();
            services.AddEntityFrameworkNpgsql().AddDbContextPool<EdoContext>(options =>
            {
                var host = databaseOptions["host"];
                var port = databaseOptions["port"];
                var password = databaseOptions["password"];
                var userId = databaseOptions["userId"];

                var connectionString = configuration.GetConnectionString("Edo");
                options.UseNpgsql(string.Format(connectionString, host, port, userId, password), builder =>
                {
                    builder.UseNetTopologySuite();
                    builder.EnableRetryOnFailure();
                });
                options.EnableSensitiveDataLogging(false);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }, 16);

            var currencyConverterOptions = vaultClient.Get(configuration["CurrencyConverter:Options"]).GetAwaiter().GetResult();
            services.Configure<CurrencyRateServiceOptions>(o =>
            {
                var url = environment.IsLocal()
                    ? configuration["CurrencyConverter:Url"]
                    : currencyConverterOptions["url"];

                o.ServiceUrl = new Uri(url);

                var cacheLifeTimeMinutes = environment.IsLocal()
                    ? configuration["CurrencyConverter:CacheLifetimeInMinutes"]
                    : currencyConverterOptions["cacheLifetimeMinutes"];

                o.CacheLifeTime = TimeSpan.FromMinutes(int.Parse(cacheLifeTimeMinutes));
            });

            var supplierOptions = vaultClient.Get(configuration["Suppliers:Options"]).GetAwaiter().GetResult();
            services.Configure<SupplierOptions>(options =>
            {
                options.Netstorming = environment.IsLocal()
                    ? configuration["Suppliers:Netstorming"]
                    : supplierOptions["netstormingConnector"];

                options.Illusions = environment.IsLocal()
                    ? configuration["Suppliers:Illusions"]
                    : supplierOptions["illusions"];

                options.Etg = environment.IsLocal()
                    ? configuration["Suppliers:Etg"]
                    : supplierOptions["etg"];

                options.DirectContracts = environment.IsLocal()
                    ? configuration["Suppliers:DirectContracts"]
                    : supplierOptions["directContracts"];
                
                options.Rakuten = environment.IsLocal()
                    ? configuration["Suppliers:Rakuten"]
                    : supplierOptions["rakuten"];
                
                var enabledConnectors = environment.IsLocal()
                    ? configuration["Suppliers:EnabledConnectors"]
                    : supplierOptions["enabledConnectors"];

                options.EnabledSuppliers = enabledConnectors
                    .Split(';')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(Enum.Parse<Suppliers>)
                    .ToList();
            });

            var googleOptions = vaultClient.Get(configuration["Edo:Google:Options"]).GetAwaiter().GetResult();
            services.Configure<GoogleOptions>(options => { options.ApiKey = googleOptions["apiKey"]; })
                .Configure<FlowOptions>(options =>
                {
                    options.CacheKeyDelimiter = "::";
                    options.CacheKeyPrefix = "HappyTravel::Edo::Api";
                })
                .Configure<RequestLocalizationOptions>(options =>
                {
                    options.DefaultRequestCulture = new RequestCulture("en");
                    options.SupportedCultures = new[]
                    {
                        new CultureInfo("en"),
                        new CultureInfo("ar"),
                        new CultureInfo("ru")
                    };

                    options.RequestCultureProviders.Insert(0, new RouteDataRequestCultureProvider { Options = options });
                });

            services.Configure<LocationServiceOptions>(o =>
            {
                o.IsGoogleGeoCoderDisabled = bool.TryParse(googleOptions["disabled"], out var disabled) && disabled;
            });

            var paymentLinksOptions = vaultClient.Get(configuration["PaymentLinks:Options"]).GetAwaiter().GetResult();
            var externalPaymentLinksMailTemplateId = mailSettings[configuration["Edo:Email:ExternalPaymentsTemplateId"]];
            var externalPaymentLinksConfirmationMailTemplateId = mailSettings[configuration["Edo:Email:PaymentLinkPaymentConfirmation"]];
            services.Configure<PaymentLinkOptions>(options =>
            {
                options.ClientSettings = new ClientSettings
                {
                    Currencies = configuration.GetSection("PaymentLinks:Currencies")
                        .Get<List<Currencies>>(),
                    ServiceTypes = configuration.GetSection("PaymentLinks:ServiceTypes")
                        .Get<Dictionary<ServiceTypes, string>>()
                };
                options.LinkMailTemplateId = externalPaymentLinksMailTemplateId;
                options.PaymentConfirmationMailTemplateId = externalPaymentLinksConfirmationMailTemplateId;
                options.SupportedVersions = new List<Version> { new Version(0, 2) };
                options.PaymentUrlPrefix = new Uri(paymentLinksOptions["endpoint"]);
            });

            var payfortOptions = vaultClient.Get(configuration["Edo:Payfort:Options"]).GetAwaiter().GetResult();
            var payfortUrlsOptions = vaultClient.Get(configuration["Edo:Payfort:Urls"]).GetAwaiter().GetResult();
            services.Configure<PayfortOptions>(options =>
            {
                options.AccessCode = payfortOptions["access-code"];
                options.Identifier = payfortOptions["merchant-identifier"];
                options.ShaRequestPhrase = payfortOptions["request-phrase"];
                options.ShaResponsePhrase = payfortOptions["response-phrase"];
                options.PaymentUrl = payfortUrlsOptions["payment"];
                options.TokenizationUrl = payfortUrlsOptions["tokenization"];
                options.ReturnUrl = payfortUrlsOptions["return"];
                options.ResultUrl = payfortUrlsOptions["result"];
            });

            var clientOptions = vaultClient.Get(configuration["Edo:Client:Options"]).GetAwaiter().GetResult();
            var (_, authorityUrl) = GetApiNameAndAuthority(configuration, environment, vaultClient);

            services.Configure<TokenRequestOptions>(options =>
            {
                var uri = new Uri(new Uri(authorityUrl), "/connect/token");
                options.Address = uri.ToString();
                options.ClientId = clientOptions["clientId"];
                options.ClientSecret = clientOptions["clientSecret"];
                options.Scope = clientOptions["scope"];
                options.GrantType = OidcConstants.GrantTypes.ClientCredentials;
            });

            var commonBankDetails = vaultClient.Get(configuration["Edo:BankDetails:Options"]).GetAwaiter().GetResult();
            var aedAccountDetails = vaultClient.Get(configuration["Edo:BankDetails:AccountDetails:AED"]).GetAwaiter().GetResult();
            var eurAccountDetails = vaultClient.Get(configuration["Edo:BankDetails:AccountDetails:EUR"]).GetAwaiter().GetResult();
            var usdAccountDetails = vaultClient.Get(configuration["Edo:BankDetails:AccountDetails:USD"]).GetAwaiter().GetResult();

            services.Configure<BankDetails>(options =>
            {
                options.BankAddress = commonBankDetails["bankAddress"];
                options.BankName = commonBankDetails["bankName"];
                options.CompanyName = commonBankDetails["companyName"];
                options.RoutingCode = commonBankDetails["routingCode"];
                options.SwiftCode = commonBankDetails["swiftCode"];

                options.AccountDetails = new Dictionary<Currencies, BankDetails.CurrencySpecificData>
                {
                    {
                        Currencies.AED, new BankDetails.CurrencySpecificData
                        {
                            Iban = aedAccountDetails["iban"],
                            AccountNumber = aedAccountDetails["accountNumber"]
                        }
                    },
                    {
                        Currencies.EUR, new BankDetails.CurrencySpecificData
                        {
                            Iban = eurAccountDetails["iban"],
                            AccountNumber = eurAccountDetails["accountNumber"]
                        }
                    },
                    {
                        Currencies.USD, new BankDetails.CurrencySpecificData
                        {
                            Iban = usdAccountDetails["iban"],
                            AccountNumber = usdAccountDetails["accountNumber"]
                        }
                    },
                };
            });

            var amazonS3DocumentsOptions = vaultClient.Get(configuration["AmazonS3:Options"]).GetAwaiter().GetResult();
            var contractsS3FolderName = configuration["AmazonS3:ContractsS3FolderName"];
            var imagesS3FolderName = configuration["AmazonS3:ImagesS3FolderName"];

            services.AddAmazonS3Client(options =>
            {
                options.AccessKeyId = amazonS3DocumentsOptions["accessKeyId"];
                options.AccessKey = amazonS3DocumentsOptions["accessKey"];
                options.AmazonS3Config = new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(amazonS3DocumentsOptions["regionEndpoint"])
                };
            });

            services.Configure<ContractFileServiceOptions>(options =>
            {
                options.Bucket = amazonS3DocumentsOptions["bucket"];
                options.S3FolderName = contractsS3FolderName;
            });

            services.Configure<ImageFileServiceOptions>(options =>
            {
                options.Bucket = amazonS3DocumentsOptions["bucket"];
                options.S3FolderName = imagesS3FolderName;
            });

            return services;
        }


        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddSingleton(NtsGeometryServices.Instance.CreateGeometryFactory(GeoConstants.SpatialReferenceId));

            services.AddTransient<IConnectorClient, ConnectorClient>();
            services.AddSingleton<IConnectorSecurityTokenManager, ConnectorSecurityTokenManager>();
            services.AddTransient<ICountryService, CountryService>();
            services.AddTransient<IGeoCoder, GoogleGeoCoder>();
            services.AddTransient<IGeoCoder, InteriorGeoCoder>();

            services.AddSingleton<IVersionService, VersionService>();

            services.AddTransient<ILocationService, LocationService>();
            services.AddTransient<ICounterpartyService, CounterpartyService>();
            services.AddTransient<ICounterpartyManagementService, CounterpartyManagementService>();
            services.AddTransient<IAgentService, AgentService>();
            services.AddTransient<IAgentRegistrationService, AgentRegistrationService>();
            services.AddTransient<IAccountPaymentService, AccountPaymentService>();
            services.AddTransient<ICounterpartyAccountService, CounterpartyAccountService>();
            services.AddTransient<IAgencyAccountService, AgencyAccountService>();
            services.AddTransient<IPaymentSettingsService, PaymentSettingsService>();
            services.AddTransient<IBookingPaymentService, BookingPaymentService>();
            services.AddTransient<IAccommodationService, AccommodationService>();
            services.AddScoped<IAgentContextService, HttpBasedAgentContextService>();
            services.AddScoped<IAgentContextInternal, HttpBasedAgentContextService>();
            services.AddHttpContextAccessor();
            services.AddSingleton<IDateTimeProvider, DefaultDateTimeProvider>();
            services.AddTransient<IBookingRecordsManager, BookingRecordsManager>();
            services.AddTransient<ITagProcessor, TagProcessor>();

            services.AddTransient<IAgentInvitationService, AgentInvitationService>();
            services.AddSingleton<IMailSender, SendGridMailSender>();
            services.AddSingleton<ITokenInfoAccessor, TokenInfoAccessor>();
            services.AddTransient<IAccountBalanceAuditService, AccountBalanceAuditService>();
            services.AddTransient<ICreditCardAuditService, CreditCardAuditService>();
            services.AddTransient<IOfflinePaymentAuditService, OfflinePaymentAuditService>();

            services.AddTransient<IAccountManagementService, AccountManagementService>();
            services.AddScoped<IAdministratorContext, HttpBasedAdministratorContext>();
            services.AddScoped<IServiceAccountContext, HttpBasedServiceAccountContext>();

            services.AddTransient<IUserInvitationService, UserInvitationService>();
            services.AddTransient<IAdministratorInvitationService, AdministratorInvitationService>();
            services.AddTransient<IExternalAdminContext, ExternalAdminContext>();

            services.AddTransient<IAdministratorRegistrationService, AdministratorRegistrationService>();
            services.AddScoped<IManagementAuditService, ManagementAuditService>();

            services.AddScoped<IEntityLocker, EntityLocker>();
            services.AddTransient<IAccountPaymentProcessingService, AccountPaymentProcessingService>();

            services.AddTransient<IPayfortService, PayfortService>();
            services.AddTransient<ICreditCardsManagementService, CreditCardsManagementService>();
            services.AddTransient<IPayfortSignatureService, PayfortSignatureService>();

            services.AddTransient<IMarkupPolicyService, MarkupPolicyService>();
            services.AddTransient<IMarkupService, MarkupService>();
            services.AddTransient<IDisplayedMarkupFormulaService, DisplayedMarkupFormulaService>();

            services.AddSingleton<IMarkupPolicyTemplateService, MarkupPolicyTemplateService>();
            services.AddScoped<IAgentMarkupPolicyManager, AgentMarkupPolicyManager>();
            services.AddScoped<IAdminMarkupPolicyManager, AdminMarkupPolicyManager>();

            services.AddScoped<ICurrencyRateService, CurrencyRateService>();
            services.AddScoped<ICurrencyConverterService, CurrencyConverterService>();

            services.AddTransient<ISupplierOrderService, SupplierOrderService>();

            services.AddSingleton<IJsonSerializer, NewtonsoftJsonSerializer>();
            services.AddTransient<IAgentSettingsManager, AgentSettingsManager>();
            services.AddTransient<IAgentStatusManagementService, AgentStatusManagementService>();

            services.AddTransient<IPaymentLinkService, PaymentLinkService>();
            services.AddTransient<IPaymentLinksProcessingService, PaymentLinksProcessingService>();
            services.AddTransient<IPaymentLinksStorage, PaymentLinksStorage>();
            services.AddTransient<IPaymentCallbackDispatcher, PaymentCallbackDispatcher>();
            services.AddTransient<IAgentPermissionManagementService, AgentPermissionManagementService>();
            services.AddTransient<IPermissionChecker, PermissionChecker>();
            services.AddTransient<IPaymentNotificationService, PaymentNotificationService>();
            services.AddTransient<IBookingMailingService, BookingMailingService>();
            services.AddTransient<IPaymentHistoryService, PaymentHistoryService>();
            services.AddTransient<IBookingDocumentsService, BookingDocumentsService>();
            services.AddTransient<IBookingAuditLogService, BookingAuditLogService>();
            services.AddTransient<ISupplierConnectorManager, SupplierConnectorManager>();
            services.AddTransient<IWideAvailabilitySearchService, WideAvailabilitySearchService>();
            services.AddTransient<IRoomSelectionService, RoomSelectionService>();
            services.AddTransient<IBookingEvaluationService, BookingEvaluationService>();
            services.AddTransient<IBookingManagementService, BookingManagementService>();
            services.AddTransient<IBookingRegistrationService, BookingRegistrationService>();
            services.AddTransient<IBookingRequestStorage, BookingRequestStorage>();
            services.AddTransient<IBookingChangesProcessor, BookingChangesProcessor>();
            services.AddTransient<IBookingResponseProcessor, BookingResponseProcessor>();
            services.AddTransient<IBookingsProcessingService, BookingsProcessingService>();
            services.AddTransient<IDeadlineService, DeadlineService>();
            services.AddTransient<IAppliedBookingMarkupRecordsManager, AppliedBookingMarkupRecordsManager>();

            services.AddSingleton<IAuthorizationPolicyProvider, CustomAuthorizationPolicyProvider>();
            services.AddTransient<IAuthorizationHandler, InAgencyPermissionAuthorizationHandler>();
            services.AddTransient<IAuthorizationHandler, MinCounterpartyStateAuthorizationHandler>();
            services.AddTransient<IAuthorizationHandler, AdministratorPermissionsAuthorizationHandler>();
            services.AddTransient<IAuthorizationHandler, AgentRequiredAuthorizationHandler>();
            services.AddTransient<IAuthorizationHandler, ServiceAccountRequiredAuthorizationHandler>();

            services.AddTransient<ICreditCardPaymentProcessingService, CreditCardPaymentProcessingService>();
            services.AddTransient<ICreditCardMoneyAuthorizationService, CreditCardMoneyAuthorizationService>();
            services.AddTransient<ICreditCardMoneyCaptureService, CreditCardMoneyCaptureService>();
            services.AddTransient<ICreditCardMoneyRefundService, CreditCardMoneyRefundService>();
            services.AddTransient<IPayfortResponseParser, PayfortResponseParser>();
            services.AddTransient<ICreditCardPaymentConfirmationService, CreditCardPaymentConfirmationService>();

            services.AddTransient<ICompanyService, CompanyService>();
            services.AddTransient<MailSenderWithCompanyInfo>();

            // Default behaviour allows not authenticated requests to be checked by authorization policies.
            // Special wrapper returns Forbid result for them.
            // More information: https://github.com/dotnet/aspnetcore/issues/4656
            services.AddTransient<IPolicyEvaluator, ForbidUnauthenticatedPolicyEvaluator>();
            // Default policy evaluator needs to be registered as dependency of ForbidUnauthenticatedPolicyEvaluator.
            services.AddTransient<PolicyEvaluator>();

            services.AddTransient<NetstormingResponseService>();
            services.AddTransient<WebhookResponseService>();

            services.AddNameNormalizationServices();
            services.AddScoped<ILocationNormalizer, LocationNormalizer>();

            services.AddTransient<IMultiProviderAvailabilityStorage, MultiProviderAvailabilityStorage>();
            services.AddTransient<IWideAvailabilityStorage, WideAvailabilityStorage>();
            services.AddTransient<IRoomSelectionStorage, RoomSelectionStorage>();

            services.AddTransient<IBookingEvaluationStorage, BookingEvaluationStorage>();

            services.AddTransient<IPriceProcessor, PriceProcessor>();

            services.AddTransient<IInvoiceService, InvoiceService>();
            services.AddTransient<IReceiptService, ReceiptService>();
            services.AddTransient<IPaymentDocumentsStorage, PaymentDocumentsStorage>();
            services.AddTransient<IPaymentLinkNotificationService, PaymentLinkNotificationService>();

            services.AddTransient<IAccommodationDuplicatesService, AccommodationDuplicatesService>();
            services.AddTransient<IAccommodationDuplicateReportsManagementService, AccommodationDuplicateReportsManagementService>();

            services.AddTransient<IAgentSystemSettingsService, AgentSystemSettingsService>();
            services.AddTransient<IAgencySystemSettingsService, AgencySystemSettingsService>();

            services.AddTransient<IAgentSystemSettingsManagementService, AgentSystemSettingsManagementService>();
            services.AddTransient<IAgencySystemSettingsManagementService, AgencySystemSettingsManagementService>();
            
            services.AddTransient<IAccommodationBookingSettingsService, AccommodationBookingSettingsService>();

            services.AddTransient<IContractFileManagementService, ContractFileManagementService>();
            services.AddTransient<IContractFileService, ContractFileService>();
            services.AddTransient<IImageFileService, ImageFileService>();

            services.AddTransient<IAnalyticsService, ElasticAnalyticsService>();
            services.AddTransient<AvailabilityAnalyticsService>();

            //TODO: move to Consul when it will be ready
            services.AddCurrencyConversionFactory(new List<BufferPair>
            {
                new BufferPair
                {
                    BufferValue = decimal.Zero,
                    SourceCurrency = Currencies.AED,
                    TargetCurrency = Currencies.USD
                },
                new BufferPair
                {
                    BufferValue = decimal.Zero,
                    SourceCurrency = Currencies.USD,
                    TargetCurrency = Currencies.AED
                }
            });
            
            return services;
        }


        public static IServiceCollection AddTracing(this IServiceCollection services, IWebHostEnvironment environment, IConfiguration configuration)
        {
            string agentHost;
            int agentPort;
            if (environment.IsLocal())
            {
                agentHost = configuration["Jaeger:AgentHost"];
                agentPort = int.Parse(configuration["Jaeger:AgentPort"]);
            }
            else
            {
                agentHost = EnvironmentVariableHelper.Get("Jaeger:AgentHost", configuration);
                agentPort = int.Parse(EnvironmentVariableHelper.Get("Jaeger:AgentPort", configuration));
            }

            var connection = ConnectionMultiplexer.Connect(EnvironmentVariableHelper.Get("Redis:Endpoint", configuration));
            var serviceName = $"{environment.ApplicationName}-{environment.EnvironmentName}";

            services.AddOpenTelemetryTracing(builder =>
            {
                builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRedisInstrumentation(connection)
                    .AddJaegerExporter(options =>
                    {
                        options.AgentHost = agentHost;
                        options.AgentPort = agentPort;
                    })
                    .SetSampler(new AlwaysOnSampler());
            });

            return services;
        }


        public static IServiceCollection AddUserEventLogging(this IServiceCollection services, IConfiguration configuration,
            VaultClient.VaultClient vaultClient)
        {
            var elasticOptions = vaultClient.Get(configuration["UserEvents:Elastic"]).GetAwaiter().GetResult();
            return services.AddSingleton<IElasticLowLevelClient>(provider =>
            {
                var settings = new ConnectionConfiguration(new Uri(elasticOptions["url"]));
                var client = new ElasticLowLevelClient(settings);

                return client;
            });
        }


        private static (string apiName, string authorityUrl) GetApiNameAndAuthority(IConfiguration configuration, IWebHostEnvironment environment,
            IVaultClient vaultClient)
        {
            var authorityOptions = vaultClient.Get(configuration["Authority:Options"]).GetAwaiter().GetResult();

            var apiName = configuration["Authority:ApiName"];
            var authorityUrl = configuration["Authority:Endpoint"];
            if (environment.IsDevelopment() || environment.IsLocal())
                return (apiName, authorityUrl);

            apiName = authorityOptions["apiName"];
            authorityUrl = authorityOptions["authorityUrl"];

            return (apiName, authorityUrl);
        }


        private static IAsyncPolicy<HttpResponseMessage> GetDefaultRetryPolicy()
        {
            var jitter = new Random();

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, attempt
                    => TimeSpan.FromSeconds(Math.Pow(1.5, attempt)) + TimeSpan.FromMilliseconds(jitter.Next(0, 100)));
        }
    }
}