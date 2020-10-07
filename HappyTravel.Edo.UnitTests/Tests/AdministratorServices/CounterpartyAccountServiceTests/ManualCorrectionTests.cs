using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Edo.UnitTests.Mocks;
using HappyTravel.Edo.UnitTests.Utility;
using HappyTravel.Money.Enums;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.AdministratorServices.CounterpartyAccountServiceTests
{
    public class ManualCorrectionTests
    {
        public ManualCorrectionTests(Mock<EdoContext> edoContextMock)
        {
            var entityLockerMock = new Mock<IEntityLocker>();

            entityLockerMock.Setup(l => l.Acquire<It.IsAnyType>(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(Result.Success()));

            _edoContextMock = edoContextMock;
            _mockedEdoContext = edoContextMock.Object;

            _counterpartyAccountService = new CounterpartyAccountService(_mockedEdoContext, entityLockerMock.Object, Mock.Of<IAccountBalanceAuditService>());

            var strategy = new ExecutionStrategyMock();

            var dbFacade = new Mock<DatabaseFacade>(_mockedEdoContext);
            dbFacade.Setup(d => d.CreateExecutionStrategy()).Returns(strategy);
            edoContextMock.Setup(c => c.Database).Returns(dbFacade.Object);
        }


        [Fact]
        public async Task Add_money_with_currency_mismatch_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.ManualAdd(
                1, new PaymentData(1, Currencies.EUR, "not empty reason"), _user);
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Add_money_to_not_existing_account_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.ManualAdd(
                0, new PaymentData(1, Currencies.USD, "not empty reason"), _user);
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Add_money_with_negative_amount_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.ManualAdd(
                1, new PaymentData(-1, Currencies.USD, "not empty reason"), _user);
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Add_money_to_suitable_account_should_increase_balance()
        {
            SetupInitialData();
            var affectedAccount = _mockedEdoContext.CounterpartyAccounts.Single(a => a.Id == 1);

            var (isSuccess, _, error) = await _counterpartyAccountService.ManualAdd(
                1, new PaymentData(1, Currencies.USD, "not empty reason"), _user);

            Assert.True(isSuccess);
            Assert.Equal(1001, affectedAccount.Balance);
        }


        [Fact]
        public async Task Manual_subtract_money_with_currency_mismatch_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.ManualSubtract(
                1, new PaymentData(1, Currencies.EUR, "not empty reason"), _user);
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Manual_subtract_money_to_not_existing_account_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.ManualSubtract(
                0, new PaymentData(1, Currencies.USD, "not empty reason"), _user);
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Manual_subtract_money_with_negative_amount_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.ManualSubtract(
                1, new PaymentData(-1, Currencies.USD, "not empty reason"), _user);
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Manual_subtract_money_from_suitable_account_should_decrease_balance()
        {
            SetupInitialData();
            var affectedAccount = _mockedEdoContext.CounterpartyAccounts.Single(a => a.Id == 1);

            var (isSuccess, _, error) = await _counterpartyAccountService.ManualSubtract(
                1, new PaymentData(1, Currencies.USD, "not empty reason"), _user);

            Assert.True(isSuccess);
            Assert.Equal(999, affectedAccount.Balance);
        }


        private void SetupInitialData()
        {
            _edoContextMock
                .Setup(c => c.Counterparties)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<Counterparty>
                {
                    new Counterparty
                    {
                        Id = 1
                    },
                    new Counterparty
                    {
                        Id = 2
                    },
                }));

            _edoContextMock
                .Setup(c => c.CounterpartyAccounts)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<CounterpartyAccount>
                {
                    new CounterpartyAccount
                    {
                        Id = 1,
                        Balance = 1000,
                        Currency = Currencies.USD,
                        CounterpartyId = 1,
                        IsActive = true
                    },
                    new CounterpartyAccount
                    {
                        Id = 2,
                        Balance = 1000,
                        Currency = Currencies.USD,
                        CounterpartyId = 2,
                        IsActive = true
                    }
                }));

            _edoContextMock.Setup(c => c.AccountBalanceAuditLogs).Returns(DbSetMockProvider.GetDbSetMock(new List<AccountBalanceAuditLogEntry>()));
        }


        private readonly Mock<EdoContext> _edoContextMock;
        private readonly EdoContext _mockedEdoContext;
        private readonly UserInfo _user = new UserInfo(1, UserTypes.Admin);
        private readonly ICounterpartyAccountService _counterpartyAccountService;
    }
}