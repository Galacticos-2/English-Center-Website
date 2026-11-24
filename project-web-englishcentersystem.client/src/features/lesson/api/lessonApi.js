import apiClient from "../../../app/apiClient";

export const getLessonCategoriesApi = async () => {
    const res = await apiClient.get("/Lesson/categories");
    return res.data.data;
};

export const getLessonSectionsApi = async (
    courseId,
    classRoomId,
    categoryId
) => {
    const res = await apiClient.get("/Lesson/sections", {
        params: {
            courseId,
            classRoomId,
            categoryId
        }
    });

    return res.data.data;
};
export const getMyTeacherClassesApi = async () => {
    const res = await apiClient.get("/Lesson/my-classes");
    return res.data.data;
};
export const getMyLessonSectionsApi = async (
    categoryId
) => {
    const res = await apiClient.get("/Lesson/my-sections", {
        params: {
            categoryId
        }
    });

    return res.data.data;
};
export const createLessonSectionApi = async (data) => {
    const res = await apiClient.post("/Lesson/sections", data);
    return res.data;
};

export const createLessonApi = async (data) => {
    const res = await apiClient.post("/Lesson", data);
    return res.data;
};
export const updateLessonApi = async (lessonId, data) => {
    const res = await apiClient.put(`/Lesson/${lessonId}`, data);
    return res.data;
};
export const updateLessonSectionApi = async (sectionId, data) => {
    const res = await apiClient.put(`/Lesson/sections/${sectionId}`, data);
    return res.data;
};
export const deleteLessonSectionApi = async (sectionId) => {
    const res = await apiClient.delete(`/Lesson/sections/${sectionId}`);
    return res.data;
};
export const uploadLessonPdfApi = async (
    lessonId,
    file
) => {
    const formData = new FormData();

    formData.append("file", file);

    const res = await apiClient.post(
        `/Lesson/${lessonId}/pdf`,
        formData,
        {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    );

    return res.data;
};
export const getLessonStatisticsApi = async (
    courseId,
    classRoomId
) => {
    const res = await apiClient.get(
        "/Lesson/sections",
        {
            params: {
                courseId,
                classRoomId
            }
        }
    );

    return res.data.data;
};
export const uploadLessonVideoApi = async (
    lessonId,
    file
) => {
    const formData = new FormData();

    formData.append("file", file);

    const res = await apiClient.post(
        `/Lesson/${lessonId}/video`,
        formData,
        {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    );

    return res.data;
};
export const deleteLessonApi = async (lessonId) => {
    const res = await apiClient.delete(`/Lesson/${lessonId}`);
    return res.data;
};
export const markLessonCompletedApi = async (lessonId) => {
    const res = await apiClient.post(
        `/Lesson/${lessonId}/complete`
    );

    return res.data;
};