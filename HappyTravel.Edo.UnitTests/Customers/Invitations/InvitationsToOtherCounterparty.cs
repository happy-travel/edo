using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Api.Services.Users;
using HappyTravel.Edo.UnitTests.Infrastructure;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Customers.Invitations
{
    public class InvitationsToOtherCounterparty
    {
        public InvitationsToOtherCounterparty()
        {
            var customer = CustomerInfoFactory.CreateByWithCounterpartyAndAgency(It.IsAny<int>(), CustomerCounterpartyId, It.IsAny<int>());
            var customerContext = new Mock<ICustomerContext>();
            customerContext
                .Setup(c => c.GetCustomer())
                .ReturnsAsync(customer);
            
            _invitationService = new CustomerInvitationService(customerContext.Object,
                Mock.Of<IOptions<CustomerInvitationOptions>>(),
                Mock.Of<IUserInvitationService>(),
                Mock.Of<ICounterpartyService>());
        }
        
        [Fact]
        public async Task Sending_invitation_to_other_counterparty_should_be_permitted()
        {
            var invitationInfoWithOtherCounterparty = new CustomerInvitationInfo(It.IsAny<CustomerEditableInfo>(),
                OtherCounterpartyId, It.IsAny<string>());
            
            var (_, isFailure, _) = await _invitationService.Send(invitationInfoWithOtherCounterparty);
            
            Assert.True(isFailure);
        }
        
        [Fact]
        public async Task Creating_invitation_to_other_counterparty_should_be_permitted()
        {
            var invitationInfoWithOtherCounterparty = new CustomerInvitationInfo(It.IsAny<CustomerEditableInfo>(),
                OtherCounterpartyId, It.IsAny<string>());
            
            var (_, isFailure, _, _) = await _invitationService.Create(invitationInfoWithOtherCounterparty);
            
            Assert.True(isFailure);
        }
        
        private readonly CustomerInvitationService _invitationService;
        private const int CustomerCounterpartyId = 123;
        private const int OtherCounterpartyId = 122;
    }
}