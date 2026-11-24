import apiClient from "../../../../app/apiClient";

export const getQuestionCategoriesApi = async () => {
    const res = await apiClient.get("/Question/categories");
    return res.data.data;
};

export const createQuestionCategoryApi = async (data) => {
    const res = await apiClient.post("/Question/categories", data);
    return res.data;
};

export const updateQuestionCategoryApi = async (categoryId, data) => {
    const res = await apiClient.put(
        `/Question/categories/${categoryId}`,
        data
    );

    return res.data;
};

export const deleteQuestionCategoryApi = async (categoryId) => {
    const res = await apiClient.delete(
        `/Question/categories/${categoryId}`
    );

    return res.data;
};

export const getQuestionsApi = async (
    categoryId,
    questionType,
    courseType,
    skill
) => {
    const res = await apiClient.get("/Question", {
        params: {
            categoryId: categoryId || null,
            questionType: questionType || null,
            courseType: courseType || null,
            skill: skill || null
            
            
        }
    });

    return res.data.data;
};






export const createQuestionApi = async (data) => {
    const res = await apiClient.post("/Question", data);
    return res.data;
};

export const updateQuestionApi = async (questionId, data) => {
    const res = await apiClient.put(`/Question/${questionId}`, data);
    return res.data;
};

export const deleteQuestionApi = async (questionId) => {
    const res = await apiClient.delete(`/Question/${questionId}`);
    return res.data;
};