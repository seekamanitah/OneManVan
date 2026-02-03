using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;

namespace OneManVan.Web.Services.Pdf;

public interface IMaterialListPdfGenerator
{
    byte[] GenerateMaterialListPdf(MaterialList materialList, CompanySettings? companySettings, bool supplyHouseFormat = false);
}

public class MaterialListPdfGenerator : IMaterialListPdfGenerator
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;
    private CompanySettings? _companySettings;
    private bool _supplyHouseFormat;

    public MaterialListPdfGenerator(IDbContextFactory<OneManVanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateMaterialListPdf(MaterialList materialList, CompanySettings? companySettings, bool supplyHouseFormat = false)
    {
        _companySettings = companySettings;
        _supplyHouseFormat = supplyHouseFormat;

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, materialList));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                var title = _supplyHouseFormat ? "MATERIAL LIST - SUPPLY ORDER" : "MATERIAL LIST";
                column.Item().Text(title)
                    .FontSize(24)
                    .Bold()
                    .FontColor(Colors.Blue.Medium);

                if (!_supplyHouseFormat)
                {
                    column.Item().PaddingTop(10).Text(_companySettings?.CompanyName ?? "OneManVan")
                        .FontSize(14)
                        .SemiBold();

                    if (!string.IsNullOrEmpty(_companySettings?.Phone))
                    {
                        column.Item().Text($"Phone: {_companySettings.Phone}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);
                    }
                }
                else
                {
                    column.Item().PaddingTop(5).Text("For Supply House Use")
                        .FontSize(10)
                        .Italic()
                        .FontColor(Colors.Grey.Darken2);
                }
            });

            row.ConstantItem(150).AlignRight().Column(column =>
            {
                column.Item().Text($"Date: {DateTime.Now:MM/dd/yyyy}")
                    .FontSize(10);
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text(t =>
            {
                t.Span("Page ");
                t.CurrentPageNumber();
                t.Span(" of ");
                t.TotalPages();
            });
        });
    }

    private void ComposeContent(IContainer container, MaterialList materialList)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(15);

            // Material List Header Info
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"List #: {materialList.ListNumber}").Bold();
                    col.Item().Text(materialList.Title);
                    
                    if (!_supplyHouseFormat && materialList.Customer != null)
                    {
                        col.Item().PaddingTop(5).Text($"Customer: {materialList.Customer.Name}");
                    }
                });

                if (!_supplyHouseFormat)
                {
                    row.ConstantItem(200).Column(col =>
                    {
                        col.Item().Text($"Created: {materialList.CreatedAt:MM/dd/yyyy}");
                        col.Item().Text($"Status: {materialList.StatusDisplay}");
                    });
                }
            });

            // Group items by category
            var groupedItems = materialList.Items
                .Where(i => i.Quantity > 0)
                .GroupBy(i => i.Category)
                .OrderBy(g => g.Key);

            foreach (var group in groupedItems)
            {
                column.Item().PaddingTop(10).Text(GetCategoryName(group.Key))
                    .Bold()
                    .FontSize(12)
                    .FontColor(Colors.Blue.Darken1);

                column.Item().Table(table =>
                {
                    if (_supplyHouseFormat)
                    {
                        // Supply house format: Item, Qty, Notes only (no prices)
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4); // Item
                            columns.RelativeColumn(1); // Qty
                            columns.RelativeColumn(3); // Notes
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Item");
                            header.Cell().Element(HeaderStyle).AlignCenter().Text("Qty");
                            header.Cell().Element(HeaderStyle).Text("Notes");
                        });

                        foreach (var item in group.OrderBy(i => i.SortOrder))
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(6).Text(item.DisplayName);
                            
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(6).AlignCenter().Text(item.Quantity.ToString());
                            
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(6).Text(item.Notes ?? "");
                        }
                    }
                    else
                    {
                        // Full format with prices
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4); // Item
                            columns.RelativeColumn(1); // Qty
                            columns.RelativeColumn(1.5f); // Unit Cost
                            columns.RelativeColumn(1.5f); // Total
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Item");
                            header.Cell().Element(HeaderStyle).AlignCenter().Text("Qty");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Unit Cost");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Total");
                        });

                        foreach (var item in group.OrderBy(i => i.SortOrder))
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(6).Text(item.DisplayName);
                            
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(6).AlignCenter().Text(item.Quantity.ToString());
                            
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(6).AlignRight().Text($"${item.UnitCost:N2}");
                            
                            var itemTotal = item.Quantity * item.UnitCost;
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(6).AlignRight().Text($"${itemTotal:N2}").Bold();
                        }
                    }
                });
            }

            // Summary section (only for non-supply house format)
            if (!_supplyHouseFormat)
            {
                column.Item().PaddingTop(20).AlignRight().Column(totals =>
                {
                    totals.Item().Row(row =>
                    {
                        row.ConstantItem(150).Text("Materials Total:");
                        row.ConstantItem(100).AlignRight().Text($"${materialList.TotalMaterialCost:N2}");
                    });

                    if (materialList.MarkupPercent > 0)
                    {
                        var markupAmount = materialList.TotalWithMarkup - materialList.TotalMaterialCost;
                        totals.Item().Row(row =>
                        {
                            row.ConstantItem(150).Text($"Markup ({materialList.MarkupPercent:N0}%):");
                            row.ConstantItem(100).AlignRight().Text($"${markupAmount:N2}");
                        });
                    }

                    if (materialList.LaborTotal > 0)
                    {
                        totals.Item().Row(row =>
                        {
                            row.ConstantItem(150).Text("Labor:");
                            row.ConstantItem(100).AlignRight().Text($"${materialList.LaborTotal:N2}");
                        });
                    }

                    totals.Item().PaddingTop(5).Row(row =>
                    {
                        row.ConstantItem(150).Text("TOTAL BID:").Bold().FontSize(12);
                        row.ConstantItem(100).AlignRight().Text($"${materialList.TotalBidPrice:N2}")
                            .Bold().FontSize(12).FontColor(Colors.Blue.Medium);
                    });
                });
            }
            else
            {
                // Supply house format: just show total item count
                var totalItems = materialList.Items.Where(i => i.Quantity > 0).Sum(i => i.Quantity);
                column.Item().PaddingTop(20).Text($"Total Items: {totalItems}")
                    .Bold()
                    .FontSize(12);
            }

            // Notes section
            if (!string.IsNullOrEmpty(materialList.Notes) && !_supplyHouseFormat)
            {
                column.Item().PaddingTop(15).Column(notes =>
                {
                    notes.Item().Text("Notes:").Bold();
                    notes.Item().Text(materialList.Notes);
                });
            }
        });
    }

    private string GetCategoryName(MaterialCategory category) => category switch
    {
        MaterialCategory.Ductwork => "Ductwork",
        MaterialCategory.Equipment => "Equipment",
        MaterialCategory.Electrical => "Electrical",
        MaterialCategory.Refrigerant => "Refrigerant/Copper",
        MaterialCategory.Drain => "Drain/Plumbing",
        MaterialCategory.Miscellaneous => "Misc Materials",
        MaterialCategory.Other => "Other",
        _ => category.ToString()
    };

    private IContainer HeaderStyle(IContainer container)
    {
        return container
            .BorderBottom(2)
            .BorderColor(Colors.Blue.Medium)
            .PaddingVertical(6)
            .Background(Colors.Blue.Lighten4);
    }
}
