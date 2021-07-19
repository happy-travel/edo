using HappyTravel.Edo.Notifications.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Edo.Data.Migrations
{
    public partial class AddPropertyOwnerNotification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData("DefaultNotificationOptions", new string[] { "Type", "EnabledProtocols", "IsMandatory", "EnabledReceivers" },
                new object[,]
                {
                    { (int)NotificationTypes.PropertyOwnerBookingConfirmation, (int)(ProtocolTypes.Email), true, (int)ReceiverTypes.None }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
