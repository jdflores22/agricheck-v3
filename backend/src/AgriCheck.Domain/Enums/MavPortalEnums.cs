namespace AgriCheck.Domain.Enums;

public enum MavPoolType
{
    BYP,
    MYP
}

public enum MavApplicationPeriodStatus
{
    Upcoming,
    Open,
    Closed
}

public enum MavApplicationStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected
}

public enum MavLicenseStatus
{
    Active,
    Expired,
    FullyUtilized,
    Suspended,
    Revoked
}

public enum MavImportCertificateStatus
{
    Active,
    PartiallyUtilized,
    FullyUtilized,
    Expired
}

public enum MavAccountTransactionType
{
    MicIssuance,
    MicUtilization,
    Adjustment
}
