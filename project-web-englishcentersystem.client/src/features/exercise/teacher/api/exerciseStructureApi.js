import apiClient from "../../../../app/apiClient";

export const getExerciseCategoriesApi = async () => {
    const res = await apiClient.get(
        "/exercise-structure/categories"
    );

    return res.data.data;
};

export const getExerciseSectionsApi = async (
    courseId,
    classRoomId,
    exerciseCategoryId
) => {
    const res = await apiClient.get(
        "/exercise-structure/sections",
        {
            params: {
                courseId: courseId || null,
                classRoomId: classRoomId || null,
                exerciseCategoryId: exerciseCategoryId || null
            }
        }
    );

    return res.data.data;
};
export const updateExerciseSectionApi = async (
    exerciseSectionId,
    data
) => {
    const res = await apiClient.put(
        `/exercise-structure/sections/${exerciseSectionId}`,
        data
    );

    return res.data;
};
export const deleteExerciseSectionApi = async (exerciseSectionId) => {
    const response = await apiClient.delete(
        `/exercise-structure/sections/${exerciseSectionId}`
    );

    return response.data;
};
export const createExerciseSectionApi = async (data) => {
    const res = await apiClient.post(
        "/exercise-structure/sections",
        data
    );

    return res.data;
};