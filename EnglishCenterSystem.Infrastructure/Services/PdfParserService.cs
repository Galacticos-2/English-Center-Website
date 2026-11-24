using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnglishCenterSystem.Application.Interfaces.Services;
using UglyToad.PdfPig;

namespace EnglishCenterSystem.Infrastructure
.Services;

public class PdfParserService : IPdfParserService
{
    public string ParseToHtml(
        string pdfPath)
    {
        using var document =
            PdfDocument.Open(pdfPath);

        var html = "";

        foreach (var page
            in document.GetPages())
        {
            html +=
                $"<p>{page.Text}</p>";
        }

        return html;
    }
}