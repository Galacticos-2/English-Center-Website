using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface IPdfParserService
{
    string ParseToHtml(string pdfPath);
}