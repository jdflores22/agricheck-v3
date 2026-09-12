using AgriCheck.Application.MavPortal.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AgriCheck.Infrastructure.Services;

public static class MavReportPdfGenerator
{
    static MavReportPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(MavReportDetailDto detail, int? mavYear)
    {
        var yearLabel = mavYear?.ToString() ?? "All Years";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("MAV Report Summary").FontSize(20).Bold().FontColor(Colors.Green.Darken2);
                    col.Item().Text($"MAV Year: {yearLabel}").FontSize(12).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Item().Text("Overview").FontSize(14).Bold();
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                        });

                        AddSummaryRow(table, "Total Applications", detail.Summary.TotalApplications);
                        AddSummaryRow(table, "Approved Applications", detail.Summary.ApprovedApplications);
                        AddSummaryRow(table, "Rejected Applications", detail.Summary.RejectedApplications);
                        AddSummaryRow(table, "Active Licenses", detail.Summary.ActiveLicenses);
                        AddSummaryRow(table, "Issued MICs", detail.Summary.IssuedMics);
                        AddSummaryRow(table, "Total Awarded (MT)", detail.Summary.TotalAwardedVolume);
                        AddSummaryRow(table, "Total Utilized (MT)", detail.Summary.TotalUtilizedVolume);
                    });

                    if (detail.ByCommodity.Count > 0)
                    {
                        col.Item().PaddingTop(20).Text("By Commodity").FontSize(14).Bold();
                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("HS").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Commodity").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Apps").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Licenses").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Awarded").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Utilized").Bold();
                            });

                            foreach (var row in detail.ByCommodity)
                            {
                                table.Cell().Padding(4).Text(row.HsCode);
                                table.Cell().Padding(4).Text(row.CommodityName);
                                table.Cell().Padding(4).Text(row.ApplicationCount.ToString());
                                table.Cell().Padding(4).Text(row.ActiveLicenses.ToString());
                                table.Cell().Padding(4).Text(row.AwardedVolume.ToString("0.###"));
                                table.Cell().Padding(4).Text(row.UtilizedVolume.ToString("0.###"));
                            }
                        });
                    }

                    if (detail.ByPoolType.Count > 0)
                    {
                        col.Item().PaddingTop(20).Text("By Pool Type").FontSize(14).Bold();
                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Pool").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Apps").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Licenses").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Awarded").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Utilized").Bold();
                            });

                            foreach (var row in detail.ByPoolType)
                            {
                                table.Cell().Padding(4).Text(row.PoolType);
                                table.Cell().Padding(4).Text(row.ApplicationCount.ToString());
                                table.Cell().Padding(4).Text(row.ActiveLicenses.ToString());
                                table.Cell().Padding(4).Text(row.AwardedVolume.ToString("0.###"));
                                table.Cell().Padding(4).Text(row.UtilizedVolume.ToString("0.###"));
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated by AgriCheck V3 · ");
                    text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC")).FontSize(9);
                });
            });
        }).GeneratePdf();
    }

    private static void AddSummaryRow(TableDescriptor table, string label, object value)
    {
        table.Cell().Padding(4).Text(label);
        table.Cell().Padding(4).Text(value.ToString() ?? string.Empty);
    }
}
