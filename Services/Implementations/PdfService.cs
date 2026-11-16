using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StokSafe.Models.ViewModels;
using StokSafe.Services.Interfaces;

namespace StokSafe.Services.Implementations;

public class PdfService : IPdfService
{
    public PdfService()
    {
        // Free community license for QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateStatementPdf(StatementViewModel model)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(ComposeHeader);
                page.Content().Element(x => ComposeContent(x, model));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });
        });

        return document.GeneratePdf();
    }

    void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("StokSafe Statement").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}");
            });
        });
    }

    void ComposeContent(IContainer container, StatementViewModel model)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(10);

            // Member Info
            column.Item().Text($"Member: {model.User.DisplayName}").FontSize(14).SemiBold();
            column.Item().Text($"Period: {model.PeriodStart:dd MMM yyyy} - {model.PeriodEnd:dd MMM yyyy}");

            // Summary Table
            column.Item().PaddingTop(15).Text("Summary").FontSize(16).SemiBold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell().BorderBottom(1).Padding(5).Text("Contributions Paid");
                table.Cell().BorderBottom(1).Padding(5).AlignRight().Text($"R {model.ContributionsPaid:N2}").FontColor(Colors.Green.Darken2);

                table.Cell().BorderBottom(1).Padding(5).Text("Fines Paid");
                table.Cell().BorderBottom(1).Padding(5).AlignRight().Text($"R {model.FinesPaid:N2}");

                table.Cell().BorderBottom(1).Padding(5).Text("Loans Repaid");
                table.Cell().BorderBottom(1).Padding(5).AlignRight().Text($"R {model.LoansRepaid:N2}");
            });

            // Contributions Detail
            if (model.Contributions.Any())
            {
                column.Item().PaddingTop(15).Text("Contributions").FontSize(16).SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Date");
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Type");
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Amount");
                    });

                    foreach (var c in model.Contributions)
                    {
                        table.Cell().BorderBottom(1).Padding(5).Text(c.ContributionDate.ToString("yyyy-MM-dd"));
                        table.Cell().BorderBottom(1).Padding(5).Text(c.PaymentType.ToString());
                        table.Cell().BorderBottom(1).Padding(5).AlignRight().Text($"R {c.Amount:N2}");
                    }
                });
            }
        });
    }
}