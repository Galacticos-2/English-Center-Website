import {
    useState,
    useEffect
} from "react";

import {
    createCourseApi,
    updateCourseApi
} from "../api/courseApi";

function CourseModal({
    open,
    onClose,
    onSuccess,
    editData
}) {
    
    const [formData, setFormData] =
        useState({
            name: "",
            description: "",
            courseType: ""
        });
    useEffect(() => {

        if (editData) {
            setFormData({
                name:
                    editData.name,

                description:
                    editData.description,

                courseType:
                    editData.courseType
            });

            return;
        }

        setFormData({
            name: "",
            description: "",
            courseType: ""
        });

    }, [editData, open]);
    if (!open) return null;

    const handleChange = (e) => {

        setFormData({
            ...formData,
            [e.target.name]:
                e.target.value
        });
    };
    const handleSubmit =
        async (e) => {

            e.preventDefault();

            if (editData) {
                await updateCourseApi(
                    editData.courseId,
                    {
                        name:
                            formData.name,

                        description:
                            formData.description,

                        courseType:
                            formData.courseType,

                        classes: []
                    }
                );
            }
            else {
                await createCourseApi({
                    ...formData,
                    numberOfClasses: 0,
                    classes: []
                });
            }

            onSuccess();
            setFormData({
                name: "",
                description: "",
                courseType: ""
            });

            onClose();
        };

    return (

        <div className="modal-overlay">

            <div className="student-modal">

                <div className="modal-header">

                    <h2>
                        {
                            editData
                                ? "Edit Course"
                                : "Create Course"
                        }
                    </h2>

                    <button
                        className="close-btn"
                        onClick={onClose}
                    >
                        ×
                    </button>

                </div>

                <form
                    className="student-form"
                    onSubmit={handleSubmit}
                >

                    <input
                        type="text"
                        name="name"
                        value={formData.name}
                        placeholder="Course name"
                        onChange={handleChange}
                    />

                    <textarea
                        name="description"
                        value={formData.description}
                        placeholder="Description"
                        onChange={handleChange}
                    />

                    <select
                        name="courseType"
                        value={formData.courseType}
                        onChange={handleChange}
                    >
                        <option value="">
                            Select course type
                        </option>

                        <option value="Vstep">
                            Vstep
                        </option>

                        <option value="TOEIC">
                            TOEIC
                        </option>

                        <option value="Communication">
                            Communication
                        </option>

                    </select>

                    <button
                        type="submit"
                        className="create-btn"
                    >
                        {
                            editData
                                ? "Update Course"
                                : "Create Course"
                        }
                    </button>

                </form>

            </div>

        </div>
    );
}

export default CourseModal;
