namespace HappyTravel.Edo.Notifications.Enums
{
    public enum NotificationTypes
    {
        None = 0,
        // Booking
        BookingVoucher = 1,
        BookingInvoice = 2,
        DeadlineApproaching = 3,
        SuccessfulPaymentReceipt = 4,
        BookingDuePaymentDate = 5,
        BookingCancelled = 6,
        BookingFinalized = 7,
        BookingStatusChanged = 8,
        // Accounts
        CreditCardPaymentReceived = 9,
        CounterpartyAccountBalanceReplenished = 10,
        // Counterparty
        AgentInvitation = 11,
        ChildAgencyInvitation = 12,
        AgentSuccessfulRegistration = 13,
        ChildAgencySuccessfulRegistration = 14,
        AgencyActivityChanged = 15,
        // Administrator
        AdministratorInvitation = 16,
        MasterAgentSuccessfulRegistration = 17,
        BookingsAdministratorSummaryNotification = 18,
        BookingAdministratorPaymentsSummary = 19,
        BookingCancelledToReservations = 20,
        BookingFinalizedToReservations = 21,
        CreditCardPaymentReceivedAdministrator = 22,
        BookingManualCorrectionNeeded = 23,
        // Other
        BookingSummaryReportForAgent = 24,
        ExternalPaymentLinks = 25,
        PaymentLinkPaidNotification = 26,
        CounterpartyActivityChanged = 27,
        CounterpartyVerificationChanged = 28,
        CounterpartyAccountBalanceSubtracted = 29,
        CounterpartyAccountBalanceIncreasedManually = 30,
        CounterpartyAccountBalanceDecreasedManually = 31,
        PropertyOwnerBookingConfirmation = 32
    }
}