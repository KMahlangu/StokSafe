using StokSafe.Models.ViewModels;

namespace StokSafe.Services.Interfaces;

public interface IPdfService
{
    byte[] GenerateStatementPdf(StatementViewModel model);
}