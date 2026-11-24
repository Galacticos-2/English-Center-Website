import {
    useEffect,
    useRef,
    useState
} from "react";

import {
    Document,
    Page,
    pdfjs
} from "react-pdf";

import "react-pdf/dist/Page/TextLayer.css";
import "react-pdf/dist/Page/AnnotationLayer.css";

pdfjs.GlobalWorkerOptions.workerSrc =
    new URL(
        "pdfjs-dist/build/pdf.worker.min.mjs",
        import.meta.url
    ).toString();

function PdfViewer({
    file,
    maxWidth = 1100,
    renderTextLayer = true,
    renderAnnotationLayer = false
}) {
    const containerRef = useRef(null);

    const [numPages, setNumPages] =
        useState(0);

    const [containerWidth, setContainerWidth] =
        useState(maxWidth);

    useEffect(() => {
        const container =
            containerRef.current;

        if (!container) {
            return;
        }

        const updateWidth = () => {
            const nextWidth =
                container.clientWidth;

            if (nextWidth > 0) {
                setContainerWidth(
                    Math.min(
                        nextWidth,
                        maxWidth
                    )
                );
            }
        };

        updateWidth();

        const observer =
            new ResizeObserver(updateWidth);

        observer.observe(container);

        return () => {
            observer.disconnect();
        };
    }, [maxWidth]);

    if (!file) {
        return (
            <div className="pdf-empty">
                No PDF file available
            </div>
        );
    }

    return (
        <div
            ref={containerRef}
            className="pdf-viewer"
        >
            <Document
                file={file}
                loading={
                    <div className="pdf-loading">
                        Loading PDF document...
                    </div>
                }
                error={
                    <div className="pdf-error">
                        Unable to load the PDF document.
                    </div>
                }
                onLoadSuccess={({ numPages }) => {
                    setNumPages(numPages);
                }}
                onLoadError={(error) => {
                    console.error(
                        "PDF load error:",
                        error
                    );

                    console.log(
                        "PDF URL:",
                        file
                    );
                }}
            >
                {Array.from(
                    {
                        length: numPages
                    },
                    (_, index) => {
                        const pageNumber =
                            index + 1;

                        return (
                            <div
                                key={pageNumber}
                                className="pdf-page-wrap"
                            >
                                <Page
                                    pageNumber={
                                        pageNumber
                                    }
                                    width={
                                        containerWidth
                                    }
                                    renderTextLayer={
                                        renderTextLayer
                                    }
                                    renderAnnotationLayer={
                                        renderAnnotationLayer
                                    }
                                />
                            </div>
                        );
                    }
                )}
            </Document>
        </div>
    );
}

export default PdfViewer;
