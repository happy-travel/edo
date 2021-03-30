using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Data.Bookings;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management
{
    public class AgentBookingManagementService : IAgentBookingManagementService
    {
        public AgentBookingManagementService(IBookingManagementService managementService, 
            IBookingRecordManager recordManager, IBookingStatusRefreshService statusRefreshService)
        {
            _managementService = managementService;
            _recordManager = recordManager;
            _statusRefreshService = statusRefreshService;
        }


        public async Task<Result> Cancel(int bookingId, AgentContext agent)
        {
            return await GetBooking(bookingId, agent)
                .Bind(Cancel);

            
            Task<Result> Cancel(Booking booking) 
                => _managementService.Cancel(booking, agent.ToUserInfo(), new BookingChangeReason 
                { 
                    Source = Common.Enums.BookingChangeSources.Supplier,
                    Event = Common.Enums.BookingChangeEvents.Cancel,
                    Reason = "�anceled on request from agent"
                });
        }
        
        
        public async Task<Result> Cancel(string referenceCode, AgentContext agent)
        {
            return await GetBooking(referenceCode, agent)
                .Bind(Cancel);

            
            Task<Result> Cancel(Booking booking) 
                => _managementService.Cancel(booking, agent.ToUserInfo(), new BookingChangeReason 
                {
                    Source = Common.Enums.BookingChangeSources.Supplier,
                    Event = Common.Enums.BookingChangeEvents.Cancel,
                    Reason = "�anceled on request from agent"
                });
        }

        
        public async Task<Result> RefreshStatus(int bookingId, AgentContext agent)
        {
            return await GetBooking(bookingId, agent)
                .Bind(Refresh);

            
            Task<Result> Refresh(Booking booking) 
                => _statusRefreshService.RefreshStatus(booking.Id, agent.ToUserInfo());
        }


        private Task<Result<Booking>> GetBooking(int bookingId, AgentContext agent) 
            => _recordManager.Get(bookingId).CheckPermissions(agent);
        
        
        private Task<Result<Booking>> GetBooking(string referenceCode, AgentContext agent) 
            => _recordManager.Get(referenceCode).CheckPermissions(agent);

        
        private readonly IBookingManagementService _managementService;
        private readonly IBookingRecordManager _recordManager;
        private readonly IBookingStatusRefreshService _statusRefreshService;
    }
}