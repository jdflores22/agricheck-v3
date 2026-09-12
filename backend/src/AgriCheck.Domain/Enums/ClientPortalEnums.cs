namespace AgriCheck.Domain.Enums;

public enum EntryStatus
{
    Draft,
    Submitted,
    UnderReview,
    ForCompliance,
    Approved,
    DaIssueBilling,
    ForInspection,
    ReadyForTransport,
    AwaitingTransport,
    PartiallyConfirmed,
    InTransit,
    Rejected,
    Cancelled
}

public enum EntryType
{
    Import,
    Export
}

public enum PaymentStatus
{
    Unpaid,
    Pending,
    Paid,
    Failed
}

public enum AccreditationSubmissionStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected,
    RevisionRequired
}

public enum CertificateStatus
{
    Active,
    Revoked,
    Expired
}

public enum WarehouseBookingStatus
{
    Pending,
    PaymentPending,
    Confirmed,
    Cancelled,
    Completed
}

public enum ClientBillStatus
{
    Unpaid,
    Pending,
    Paid,
    Overdue,
    Cancelled
}

public enum EntryMavDocumentStatus
{
    NotProvided,
    PendingReview,
    Approved,
    Rejected,
    RevisionRequired
}
