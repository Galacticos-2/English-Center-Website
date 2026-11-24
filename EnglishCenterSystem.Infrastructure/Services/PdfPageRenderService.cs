using EnglishCenterSystem.Application
    .Interfaces.Services;

using PDFtoImage;

using SkiaSharp;

namespace EnglishCenterSystem.Infrastructure.Services;

public class PdfPageRenderService
    : IPdfPageRenderService
{
    public Task<MemoryStream>
        RenderAndCropPageAsync(
            byte[] pdfBytes,
            int pageNumber,
            double pdfPageWidth,
            double pdfPageHeight,
            double cropLeft,
            double cropBottom,
            double cropRight,
            double cropTop,
            int dpi = 180)
    {
        if (pdfBytes == null ||
            pdfBytes.Length == 0)
        {
            throw new Exception(
                "PDF data is required");
        }

        if (pageNumber <= 0)
        {
            throw new Exception(
                "PDF page number must be greater than 0");
        }

        if (pdfPageWidth <= 0 ||
            pdfPageHeight <= 0)
        {
            throw new Exception(
                "Invalid PDF page size");
        }

        if (cropRight <= cropLeft ||
            cropTop <= cropBottom)
        {
            throw new Exception(
                "Invalid PDF crop bounds");
        }

        var renderOptions =
            new RenderOptions(
                Dpi: dpi,
                WithAnnotations: false,
                WithFormFill: false,
                BackgroundColor:
                    SKColors.White);

        /*
         * PDFtoImage dùng page index bắt đầu từ 0.
         * PdfPig PageNumber bắt đầu từ 1.
         */
        using var renderedBitmap =
            Conversion.ToImage(
                pdfBytes,
                page: pageNumber - 1,
                options: renderOptions);

        var scaleX =
            renderedBitmap.Width /
            pdfPageWidth;

        var scaleY =
            renderedBitmap.Height /
            pdfPageHeight;

        /*
         * PDF:
         * origin ở góc dưới bên trái.
         *
         * Bitmap:
         * origin ở góc trên bên trái.
         */
        var pixelLeft =
            (int)Math.Floor(
                cropLeft * scaleX);

        var pixelRight =
            (int)Math.Ceiling(
                cropRight * scaleX);

        var pixelTop =
            (int)Math.Floor(
                (
                    pdfPageHeight -
                    cropTop
                ) * scaleY);

        var pixelBottom =
            (int)Math.Ceiling(
                (
                    pdfPageHeight -
                    cropBottom
                ) * scaleY);

        pixelLeft = Math.Clamp(
            pixelLeft,
            0,
            renderedBitmap.Width - 1);

        pixelRight = Math.Clamp(
            pixelRight,
            pixelLeft + 1,
            renderedBitmap.Width);

        pixelTop = Math.Clamp(
            pixelTop,
            0,
            renderedBitmap.Height - 1);

        pixelBottom = Math.Clamp(
            pixelBottom,
            pixelTop + 1,
            renderedBitmap.Height);

        var cropWidth =
            pixelRight - pixelLeft;

        var cropHeight =
            pixelBottom - pixelTop;

        if (cropWidth <= 0 ||
            cropHeight <= 0)
        {
            throw new Exception(
                "Calculated PDF crop area is empty");
        }

        using var croppedBitmap =
            new SKBitmap(
                cropWidth,
                cropHeight,
                renderedBitmap.ColorType,
                renderedBitmap.AlphaType);

        using (
            var canvas =
                new SKCanvas(croppedBitmap)
        )
        {
            canvas.Clear(SKColors.White);

            var sourceRectangle =
                new SKRect(
                    pixelLeft,
                    pixelTop,
                    pixelRight,
                    pixelBottom);

            var destinationRectangle =
                new SKRect(
                    0,
                    0,
                    cropWidth,
                    cropHeight);

            canvas.DrawBitmap(
                renderedBitmap,
                sourceRectangle,
                destinationRectangle);

            canvas.Flush();
        }

        using var image =
            SKImage.FromBitmap(
                croppedBitmap);

        using var encodedData =
            image.Encode(
                SKEncodedImageFormat.Webp,
                90);

        if (encodedData == null)
        {
            throw new Exception(
                "Could not encode passage image");
        }

        var outputStream =
            new MemoryStream();

        encodedData.SaveTo(
            outputStream);

        outputStream.Position = 0;

        return Task.FromResult(
            outputStream);
    }
}