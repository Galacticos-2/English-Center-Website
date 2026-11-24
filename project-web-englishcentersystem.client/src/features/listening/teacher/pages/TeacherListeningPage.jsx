import { useEffect, useState } from "react";

import {
    getListeningTasksApi,
    createListeningTaskApi,
    deleteListeningTaskApi
}
    from "../api/listeningApi";

function TeacherListeningPage() {

    const [tasks, setTasks] =
        useState([]);

    const [title, setTitle] =
        useState("");

    const [transcript,
        setTranscript] =
        useState("");

    const [audioFile,
        setAudioFile] =
        useState(null);
    const loadData =
        async () => {

            const data =
                await getListeningTasksApi();

            setTasks(data);
        };
    useEffect(() => {

        loadData();

    }, []);

    

    const handleCreate =
        async () => {

            const formData =
                new FormData();

            formData.append(
                "title",
                title
            );

            formData.append(
                "transcript",
                transcript
            );

            formData.append(
                "audioFile",
                audioFile
            );

            await createListeningTaskApi(
                formData
            );

            loadData();
        };

    return (
        <div>

            <h2>
                Listening Library
            </h2>

            <input
                placeholder="Title"
                value={title}
                onChange={(e) =>
                    setTitle(
                        e.target.value
                    )
                }
            />

            <textarea
                placeholder="Transcript"
                value={transcript}
                onChange={(e) =>
                    setTranscript(
                        e.target.value
                    )
                }
            />

            <input
                type="file"
                accept="audio/*"
                onChange={(e) =>
                    setAudioFile(
                        e.target.files[0]
                    )
                }
            />

            <button
                onClick={handleCreate}
            >
                Create
            </button>

            <hr />

            {
                tasks.map(task => (
                    <div
                        key={
                            task.listeningTaskId
                        }
                    >
                        <h3>
                            {task.title}
                        </h3>

                        <audio
                            controls
                            src={
                                task.audioUrl
                            }
                        />

                        <button
                            onClick={
                                async () => {

                                    await deleteListeningTaskApi(
                                        task.listeningTaskId
                                    );

                                    loadData();
                                }
                            }
                        >
                            Delete
                        </button>
                    </div>
                ))
            }

        </div>
    );
}

export default TeacherListeningPage;