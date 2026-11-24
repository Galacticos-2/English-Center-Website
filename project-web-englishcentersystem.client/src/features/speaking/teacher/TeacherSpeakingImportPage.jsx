import {
    useState
}
from "react";

import {
    importSpeakingExerciseApi
}
from "../api/speakingApi";

function TeacherSpeakingImportPage() {

    const [pdf,
        setPdf] =
        useState(null);

    const [title,
        setTitle] =
        useState("");

    const handleImport =
        async () => {

            const form =
                new FormData();

            form.append(
                "pdfFile",
                pdf);

            form.append(
                "title",
                title);

            await importSpeakingExerciseApi(
                form);

            alert("Imported");
        };

    return (
        <div>

            <h2>
                Import Speaking
            </h2>

            <input
                value={title}
                onChange={(e)=>
                    setTitle(
                        e.target.value)}
            />

            <input
                type="file"
                accept=".pdf"
                onChange={(e)=>
                    setPdf(
                        e.target.files[0]
                    )}
            />

            <button
                onClick={handleImport}
            >
                Import
            </button>

        </div>
    );
}

export default TeacherSpeakingImportPage;