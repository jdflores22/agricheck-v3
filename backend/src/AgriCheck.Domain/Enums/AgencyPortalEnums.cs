namespace AgriCheck.Domain.Enums;

public enum EvaluationDecision
{
    Pending,
    Approved,
    Rejected,
    RevisionRequired
}

public enum AssignmentStatus
{
    Pending,
    Active,
    Completed
}

public enum InspectionStatus
{
    Scheduled,
    InProgress,
    Completed,
    Failed
}

public enum ComplianceResultStatus
{
    Pending,
    Passed,
    Failed,
    Na
}

public enum AgencyBillingStatus
{
    Draft,
    Issued,
    PaymentPending,
    Paid,
    Cancelled
}

public enum ContainerInspectionPhotoType
{
    ActualItem,
    LabelImage,
    XrayImage,
    ExaminationImage,
    ReportImage,
    RequestForInspection
}

public enum DoctorInspectionStatus
{
    Pending,
    InProgress,
    Approved,
    Rejected
}
