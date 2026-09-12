namespace AgriCheck.Domain.Enums;

public static class EntryStatusRules
{
    public static readonly EntryStatus[] OperationalPipelineStatuses =
    {
        EntryStatus.Approved,
        EntryStatus.DaIssueBilling,
        EntryStatus.ForInspection,
        EntryStatus.ReadyForTransport,
        EntryStatus.AwaitingTransport,
        EntryStatus.PartiallyConfirmed,
        EntryStatus.InTransit
    };

    public static bool IsInOperationalPipeline(EntryStatus status) =>
        status is EntryStatus.Approved
            or EntryStatus.DaIssueBilling
            or EntryStatus.ForInspection
            or EntryStatus.ReadyForTransport
            or EntryStatus.AwaitingTransport
            or EntryStatus.PartiallyConfirmed
            or EntryStatus.InTransit;

    public static bool CanUploadInspectionPhotos(EntryStatus status) =>
        status == EntryStatus.ForInspection;

    public static bool CanIssueCertificate(EntryStatus status) =>
        status is EntryStatus.Approved
            or EntryStatus.DaIssueBilling
            or EntryStatus.ForInspection
            or EntryStatus.ReadyForTransport
            or EntryStatus.AwaitingTransport
            or EntryStatus.PartiallyConfirmed
            or EntryStatus.InTransit;

    public static bool CanScheduleAgencyInspection(EntryStatus status) =>
        status is EntryStatus.ForInspection
            or EntryStatus.ReadyForTransport
            or EntryStatus.AwaitingTransport
            or EntryStatus.PartiallyConfirmed
            or EntryStatus.InTransit;
}
