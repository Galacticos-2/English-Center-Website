namespace EnglishCenterSystem.Application
    .Interfaces.Services;

public interface IPdfPageRenderService
{
    Task<MemoryStream> RenderAndCropPageAsync(
        byte[] pdfBytes,
        int pageNumber,
        double pdfPageWidth,
        double pdfPageHeight,
        double cropLeft,
        double cropBottom,
        double cropRight,
        double cropTop,
        int dpi = 180);
}