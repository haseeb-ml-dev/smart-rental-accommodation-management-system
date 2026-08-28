using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Smart_Rental___Accomodation_Management_System.Extensions;
using Smart_Rental___Accomodation_Management_System.ViewModels;

namespace Smart_Rental___Accomodation_Management_System.Services
{
    public class ReportExportService
    {
        public byte[] BuildExcel(LandlordReportData data)
        {
            using var workbook = new XLWorkbook();

            var summary = workbook.Worksheets.Add("Summary");
            var summaryRows = new (string Label, object Value)[]
            {
                ("Landlord", data.LandlordName),
                ("Generated", data.GeneratedAt),
                ("Total properties", data.TotalProperties),
                ("Total units", data.TotalUnits),
                ("Occupied units", data.OccupiedUnits),
                ("Vacant units", data.VacantUnits),
                ("Active tenants", data.ActiveTenants),
                ("Collected this month", data.CollectedThisMonth),
                ("Outstanding this month", data.OutstandingThisMonth),
                ("Overdue invoices", data.OverdueInvoiceCount),
                ("Outstanding utilities", data.OutstandingUtilities)
            };

            for (var i = 0; i < summaryRows.Length; i++)
            {
                summary.Cell(i + 1, 1).Value = summaryRows[i].Label;
                summary.Cell(i + 1, 1).Style.Font.Bold = true;
                summary.Cell(i + 1, 2).Value = XLCellValue.FromObject(summaryRows[i].Value);
            }
            summary.Columns().AdjustToContents();

            var occupancy = workbook.Worksheets.Add("Occupancy by property");
            occupancy.Cell(1, 1).Value = "Property";
            occupancy.Cell(1, 2).Value = "Total units";
            occupancy.Cell(1, 3).Value = "Occupied units";
            occupancy.Cell(1, 4).Value = "Occupancy rate";
            occupancy.Row(1).Style.Font.Bold = true;

            var occRow = 2;
            foreach (var p in data.PropertyOccupancy)
            {
                occupancy.Cell(occRow, 1).Value = p.PropertyName;
                occupancy.Cell(occRow, 2).Value = p.TotalUnits;
                occupancy.Cell(occRow, 3).Value = p.OccupiedUnits;
                occupancy.Cell(occRow, 4).Value = p.OccupancyRate / 100;
                occupancy.Cell(occRow, 4).Style.NumberFormat.Format = "0.0%";
                occRow++;
            }
            occupancy.Columns().AdjustToContents();

            var trend = workbook.Worksheets.Add("Monthly collection");
            trend.Cell(1, 1).Value = "Month";
            trend.Cell(1, 2).Value = "Collected";
            trend.Cell(1, 3).Value = "Outstanding";
            trend.Row(1).Style.Font.Bold = true;

            var trendRow = 2;
            foreach (var m in data.MonthlyCollection)
            {
                trend.Cell(trendRow, 1).Value = m.Label;
                trend.Cell(trendRow, 2).Value = m.Collected;
                trend.Cell(trendRow, 3).Value = m.Outstanding;
                trendRow++;
            }
            trend.Columns().AdjustToContents();

            var invoices = workbook.Worksheets.Add("Invoices");
            string[] invoiceHeaders = { "Property", "Unit", "Tenant", "Period", "Amount", "Due date", "Status", "Paid date" };
            for (var c = 0; c < invoiceHeaders.Length; c++)
            {
                invoices.Cell(1, c + 1).Value = invoiceHeaders[c];
            }
            invoices.Row(1).Style.Font.Bold = true;

            var invRow = 2;
            foreach (var inv in data.Invoices)
            {
                invoices.Cell(invRow, 1).Value = inv.PropertyName;
                invoices.Cell(invRow, 2).Value = inv.UnitName;
                invoices.Cell(invRow, 3).Value = inv.TenantName;
                invoices.Cell(invRow, 4).Value = $"{inv.PeriodMonth}/{inv.PeriodYear}";
                invoices.Cell(invRow, 5).Value = inv.Amount;
                invoices.Cell(invRow, 6).Value = inv.DueDate;
                invoices.Cell(invRow, 7).Value = inv.Status.Humanize();
                if (inv.PaidDate.HasValue)
                {
                    invoices.Cell(invRow, 8).Value = inv.PaidDate.Value;
                }
                invRow++;
            }
            invoices.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] BuildPdf(LandlordReportData data)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text($"Landlord Report — {data.LandlordName}").FontSize(18).Bold();
                        col.Item().Text($"Generated {data.GeneratedAt:MMM d, yyyy h:mm tt} UTC").FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Spacing(15);

                        col.Item().Element(c => SummarySection(c, data));
                        col.Item().Element(c => OccupancySection(c, data));
                        col.Item().Element(c => TrendSection(c, data));
                        col.Item().Element(c => InvoicesSection(c, data));
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static void SummarySection(IContainer container, LandlordReportData data)
        {
            container.Column(col =>
            {
                col.Item().Text("Summary").FontSize(13).Bold();
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    AddStat(table, "Properties / Units", $"{data.TotalProperties} / {data.TotalUnits}");
                    AddStat(table, "Occupied / Vacant", $"{data.OccupiedUnits} / {data.VacantUnits}");
                    AddStat(table, "Active tenants", data.ActiveTenants.ToString());
                    AddStat(table, "Overdue invoices", data.OverdueInvoiceCount.ToString());
                    AddStat(table, "Collected this month", data.CollectedThisMonth.ToString("C"));
                    AddStat(table, "Outstanding this month", data.OutstandingThisMonth.ToString("C"));
                    AddStat(table, "Outstanding utilities", data.OutstandingUtilities.ToString("C"));
                });
            });
        }

        private static void AddStat(TableDescriptor table, string label, string value)
        {
            table.Cell().Padding(3).Column(c =>
            {
                c.Item().Text(label).FontColor(Colors.Grey.Darken1).FontSize(8);
                c.Item().Text(value).Bold();
            });
        }

        private static void OccupancySection(IContainer container, LandlordReportData data)
        {
            container.Column(col =>
            {
                col.Item().Text("Occupancy by property").FontSize(13).Bold();
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Property").Bold();
                        header.Cell().Text("Total units").Bold();
                        header.Cell().Text("Occupied").Bold();
                        header.Cell().Text("Rate").Bold();
                    });

                    foreach (var p in data.PropertyOccupancy)
                    {
                        table.Cell().Text(p.PropertyName);
                        table.Cell().Text(p.TotalUnits.ToString());
                        table.Cell().Text(p.OccupiedUnits.ToString());
                        table.Cell().Text($"{p.OccupancyRate:0.0}%");
                    }

                    if (data.PropertyOccupancy.Count == 0)
                    {
                        table.Cell().ColumnSpan(4).Text("No properties yet.").FontColor(Colors.Grey.Darken1);
                    }
                });
            });
        }

        private static void TrendSection(IContainer container, LandlordReportData data)
        {
            container.Column(col =>
            {
                col.Item().Text("Monthly collection (last 6 months)").FontSize(13).Bold();
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Month").Bold();
                        header.Cell().Text("Collected").Bold();
                        header.Cell().Text("Outstanding").Bold();
                    });

                    foreach (var m in data.MonthlyCollection)
                    {
                        table.Cell().Text(m.Label);
                        table.Cell().Text(m.Collected.ToString("C"));
                        table.Cell().Text(m.Outstanding.ToString("C"));
                    }
                });
            });
        }

        private static void InvoicesSection(IContainer container, LandlordReportData data)
        {
            container.Column(col =>
            {
                col.Item().Text($"Invoices ({data.Invoices.Count})").FontSize(13).Bold();
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Property").Bold();
                        header.Cell().Text("Unit").Bold();
                        header.Cell().Text("Tenant").Bold();
                        header.Cell().Text("Period").Bold();
                        header.Cell().Text("Amount").Bold();
                        header.Cell().Text("Due").Bold();
                        header.Cell().Text("Status").Bold();
                    });

                    foreach (var inv in data.Invoices)
                    {
                        table.Cell().Text(inv.PropertyName);
                        table.Cell().Text(inv.UnitName);
                        table.Cell().Text(inv.TenantName);
                        table.Cell().Text($"{inv.PeriodMonth}/{inv.PeriodYear}");
                        table.Cell().Text(inv.Amount.ToString("C"));
                        table.Cell().Text(inv.DueDate.ToString("MMM d, yyyy"));
                        table.Cell().Text(inv.Status.Humanize());
                    }

                    if (data.Invoices.Count == 0)
                    {
                        table.Cell().ColumnSpan(7).Text("No invoices yet.").FontColor(Colors.Grey.Darken1);
                    }
                });
            });
        }
    }
}
