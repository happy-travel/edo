using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.CurrencyConversion;
using HappyTravel.Edo.Api.Services.Markups;
using HappyTravel.Edo.Api.Services.Markups.Templates;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Markup;
using HappyTravel.Edo.UnitTests.Mocks;
using HappyTravel.Edo.UnitTests.Utility;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Markups.MarkupServiceTests
{
    public class MarkupDisable
    {
        [Theory]
        [InlineData(100, Currencies.EUR, 14000000)]
        [InlineData(240.5, Currencies.USD, 33670000)]
        [InlineData(0.13, Currencies.USD, 18200)]
        public async Task Markups_should_be_applied_if_enabled(decimal supplierPrice, Currencies currency, decimal expectedResultPrice)
        {
            var accommodationBookingsSettingsMock = new Mock<IAccommodationBookingSettingsService>();
            accommodationBookingsSettingsMock
                .Setup(s => s.Get(It.IsAny<AgentContext>()))
                .ReturnsAsync(new AccommodationBookingSettings(default, default, default, isMarkupDisabled: false, isSupplierVisible: default, default, areTagsVisible: default, default));
            var markupService = CreateMarkupService(accommodationBookingsSettingsMock.Object);
            var classUnderMarkup = new TestStructureUnderMarkup {Price = new MoneyAmount(supplierPrice, currency)};
            
            var processed = await markupService.ApplyMarkups(AgentContext, classUnderMarkup, TestStructureUnderMarkup.Apply);
            
            Assert.Equal(expectedResultPrice, processed.Price.Amount);
        }
        
        
        [Theory]
        [InlineData(100, Currencies.EUR, 100)]
        [InlineData(240.5, Currencies.USD, 240.5)]
        [InlineData(0.13, Currencies.USD, 0.13)]
        public async Task Markups_should_not_be_applied_if_disabled(decimal supplierPrice, Currencies currency, decimal expectedResultPrice)
        {
            var accommodationBookingSettingsServiceMock = new Mock<IAccommodationBookingSettingsService>();
            accommodationBookingSettingsServiceMock
                .Setup(s => s.Get(It.IsAny<AgentContext>()))
                .ReturnsAsync(new AccommodationBookingSettings(default, default, default, isMarkupDisabled: true, isSupplierVisible: false, default, areTagsVisible: default, default));
            var markupService = CreateMarkupService(accommodationBookingSettingsServiceMock.Object);
            var classUnderMarkup = new TestStructureUnderMarkup {Price = new MoneyAmount(supplierPrice, currency)};
            
            var processed = await markupService.ApplyMarkups(AgentContext, classUnderMarkup, TestStructureUnderMarkup.Apply);
            
            Assert.Equal(expectedResultPrice, processed.Price.Amount);
        }

        
        private IMarkupService CreateMarkupService(IAccommodationBookingSettingsService accommodationBookingSettingsService)
        {
            var edoContextMock = MockEdoContextFactory.Create();
            var flow = new FakeDoubleFlow();
            
            edoContextMock.Setup(c => c.MarkupPolicies)
                .Returns(DbSetMockProvider.GetDbSetMock(_policies));

            edoContextMock.Setup(c => c.Agencies)
                .Returns(DbSetMockProvider.GetDbSetMock(_agencies));
            
            var currencyRateServiceMock = new Mock<ICurrencyRateService>();
            currencyRateServiceMock
                .Setup(c => c.Get(It.IsAny<Currencies>(), It.IsAny<Currencies>()))
                .Returns(new ValueTask<Result<decimal>>(Result.Success((decimal)1)));

            var agentSettingsMock = new Mock<IAgentSettingsManager>();
            
            agentSettingsMock
                .Setup(s => s.GetUserSettings(It.IsAny<AgentContext>()))
                .Returns(Task.FromResult(new AgentUserSettings(true, It.IsAny<Currencies>(), It.IsAny<Currencies>(), It.IsAny<int>())));
            
            var functionService = new MarkupPolicyService(edoContextMock.Object,
                flow,
                agentSettingsMock.Object,
                accommodationBookingSettingsService);
            
            return new MarkupService(functionService, new MarkupPolicyTemplateService(),
                currencyRateServiceMock.Object, new FakeMemoryFlow());
        }
        
        
        private readonly IEnumerable<MarkupPolicy> _policies = new[]
        {
            new MarkupPolicy
            {
                Id = 6,
                Order = 1,
                Target = MarkupPolicyTarget.AccommodationAvailability,
                ScopeType = MarkupPolicyScopeType.Global,
                TemplateId = 1,
                TemplateSettings = new Dictionary<string, decimal> {{"factor", 100}},
            },
            new MarkupPolicy
            {
                Id = 7,
                Order = 1,
                AgencyId = AgentContext.AgencyId,
                Target = MarkupPolicyTarget.AccommodationAvailability,
                ScopeType = MarkupPolicyScopeType.Agency,
                TemplateId = 1,
                TemplateSettings = new Dictionary<string, decimal> {{"factor", 100}},
            },
            new MarkupPolicy
            {
                Id = 9,
                Order = 14,
                AgentId = AgentContext.AgentId,
                Target = MarkupPolicyTarget.AccommodationAvailability,
                ScopeType = MarkupPolicyScopeType.EndClient,
                TemplateId = 1,
                TemplateSettings = new Dictionary<string, decimal> {{"factor", 14}},
            }
        };


        private readonly IEnumerable<Agency> _agencies = new[]
        {
            new Agency
            {
                Id = AgentContext.AgencyId,
                Name = "Child agency",
                Ancestors = new List<int> {2000, 1000}
            },
            new Agency
            {
                Id = 1000,
                Name = "Parent agency",
                Ancestors = new List<int>{2000}
            },
            new Agency
            {
                Id = 2000,
                Name = "Root agency",
                Ancestors = new()
            }
        };
        
        
        private static readonly AgentContext AgentContext = AgentContextFactory.CreateWithCounterpartyAndAgency(1, 1, 1);
    }
}