import {
    useState
}
from "react";

import SpeakingRecorder
from "./SpeakingRecorder";

import {
    submitSpeakingAnswerApi
}
from "../../api/speakingApi";

function SpeakingExercisePlayer({

    speakingTask

}){

    const [audio,
        setAudio]=
        useState();

    const [result,
        setResult]=
        useState();

    const submit=
        async()=>{

            const form=
                new FormData();

            form.append(
                "audioFile",
                audio,
                "answer.webm");

            form.append(
                "speakingTaskId",
                speakingTask.speakingTaskId);

            const data=
                await submitSpeakingAnswerApi(
                    form);

            setResult(data);
        };

    return(

        <div>

            <iframe

                title="pdf"

                src={
                    speakingTask.pdfUrl
                }

                width="100%"

                height="600"

            />

            <SpeakingRecorder

                onRecorded={
                    setAudio
                }

            />

            <button
                onClick={submit}>
                Submit
            </button>

            {
                result&&(

                    <div>

                        <h3>

                            Overall

                        </h3>

                        <h1>

                            {
                                result.overallScore
                            }

                        </h1>

                        <p>

                            {
                                result.feedback
                            }

                        </p>

                    </div>

                )
            }

        </div>

    );

}

export default SpeakingExercisePlayer;