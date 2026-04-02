using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Meditrans.Client.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Meditrans.Client.Models.Pdf
{
    public class AviataDocument : IDocument
    {
        private readonly List<IGrouping<string, ProductionReportRowDto>> _data;

        public AviataDocument(List<IGrouping<string, ProductionReportRowDto>> data) => _data = data;

        public void Compose(IDocumentContainer container)
        {
            foreach (var clientGroup in _data)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                    page.Header().Element(h => ComposeHeader(h, clientGroup.First()));
                    page.Content().Element(c => ComposeContent(c, clientGroup));
                    page.Footer().PaddingTop(5).Element(f => {
                        f.AlignRight().Text(x => {
                            x.Span("Page ").FontSize(7);
                            x.CurrentPageNumber().FontSize(7);
                        });
                    });
                });
            }
        }

        private void ComposeHeader(IContainer container, ProductionReportRowDto first)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Client Name / DOB").Bold().Underline();
                    col.Item().Text($"{first.Patient} / {first.DOB?.ToString("MM-dd-yyyy")}").Bold();
                    col.Item().Text(first.PatientAddress);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("Funding Source / Insurance Id").Bold().Underline();
                    col.Item().Text(first.FundingSource ?? "N/A").Bold();
                });
            });
        }

        private void ComposeContent(IContainer container, IGrouping<string, ProductionReportRowDto> group)
        {
            container.PaddingTop(10).Column(col => {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(50); // Date
                        columns.ConstantColumn(50); // Run
                        columns.RelativeColumn(2);  // Pickup
                        columns.RelativeColumn(2);  // Dropoff
                        columns.ConstantColumn(40); // Num Spaces
                        columns.ConstantColumn(40); // Miles
                        columns.RelativeColumn(1.5f); // Charge Name
                        columns.ConstantColumn(40); // Quantity
                        columns.ConstantColumn(40); // Rate
                        columns.ConstantColumn(50); // Ext Amount
                        columns.RelativeColumn(1.5f); // Auth
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("Date");
                        header.Cell().Element(HeaderStyle).Text("Run");
                        header.Cell().Element(HeaderStyle).Text("Pickup Address");
                        header.Cell().Element(HeaderStyle).Text("Dropoff Address");
                        header.Cell().Element(HeaderStyle).Text("Num Spaces");
                        header.Cell().Element(HeaderStyle).Text("Miles");
                        header.Cell().Element(HeaderStyle).Text("Charge Name");
                        header.Cell().Element(HeaderStyle).Text("Quantity");
                        header.Cell().Element(HeaderStyle).Text("$ Rate");
                        header.Cell().Element(HeaderStyle).Text("$ Ext Amount");
                        header.Cell().Element(HeaderStyle).Text("Authorization / Passenger Id");
                    });

                    foreach (var trip in group)
                    {
                        var bgColor = trip.Canceled ? "#FFB38A" : "#FFFFFF"; // Naranja claro para cancelados

                        table.Cell().Background(bgColor).Element(CellStyle).Text(trip.Date.ToString("MM/dd/yy"));
                        table.Cell().Background(bgColor).Element(CellStyle).Text(trip.Run);
                        table.Cell().Background(bgColor).Element(CellStyle).Text(trip.PickupAddress).FontSize(7);
                        table.Cell().Background(bgColor).Element(CellStyle).Text(trip.DropoffAddress).FontSize(7);
                        table.Cell().Background(bgColor).Element(CellStyle).AlignCenter().Text("1");
                        table.Cell().Background(bgColor).Element(CellStyle).AlignCenter().Text((trip.Distance ?? 0.0).ToString("N2"));

                        // Sub-columna para cargos detallados
                        table.Cell().Background(bgColor).Element(CellStyle).Column(c => {
                            foreach (var line in trip.BillableLines) c.Item().Text(line.ChargeName);
                        });
                        table.Cell().Background(bgColor).Element(CellStyle).Column(c => {
                            foreach (var line in trip.BillableLines) c.Item().AlignCenter().Text(line.Quantity.ToString("N1"));
                        });
                        table.Cell().Background(bgColor).Element(CellStyle).Column(c => {
                            foreach (var line in trip.BillableLines) c.Item().Text(line.Rate.ToString("C2"));
                        });
                        table.Cell().Background(bgColor).Element(CellStyle).Column(c => {
                            foreach (var line in trip.BillableLines) c.Item().Text(line.Amount.ToString("C2"));
                        });

                        table.Cell().Background(bgColor).Element(CellStyle).Text(trip.Authorization);
                    }
                });

                // Totales al final de la tabla del cliente
                col.Item().Table(table => {
                    table.ColumnsDefinition(columns => {
                        columns.ConstantColumn(230); // Espacio hasta Miles
                        columns.ConstantColumn(40);  // Total Miles
                        columns.RelativeColumn();    // Espacio hasta Amount
                        columns.ConstantColumn(80);  // Total Amount
                    });

                    table.Cell().Background("#E0E0E0").Border(0.5f).Padding(2).Text("TOTALS:").Bold();
                    table.Cell().Background("#D3D3D3").Border(0.5f).Padding(2).AlignCenter().Text((group.Sum(x => x.Distance) ?? 0.0).ToString("N2")).Bold();
                    table.Cell().Border(0).Text("");
                    table.Cell().Background("#D3D3D3").Border(0.5f).Padding(2).AlignRight().Text(group.Sum(x => x.TotalTripAmount).ToString("C2")).Bold();
                });
            });
        }

        static IContainer HeaderStyle(IContainer container) => container.Background("#D3D3D3").Border(0.5f).Padding(2).AlignCenter().AlignMiddle().DefaultTextStyle(x => x.Bold().FontSize(7));
        static IContainer CellStyle(IContainer container) => container.Border(0.5f).Padding(2).AlignMiddle();
    }
}