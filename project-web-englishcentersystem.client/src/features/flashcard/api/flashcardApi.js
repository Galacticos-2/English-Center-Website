import apiClient from "../../../app/apiClient";

export const getTeacherFlashcardsApi = async () =>
    (await apiClient.get("/flashcards/teacher")).data.data;

export const getStudentFlashcardsApi = async () =>
    (await apiClient.get("/flashcards/student")).data.data;

export const getFlashcardStudyProgressApi = async () =>
    (await apiClient.get("/flashcards/student/progress")).data.data;

export const getFlashcardStudySessionApi = async mode =>
    (await apiClient.get(`/flashcards/student/study-session/${mode}`)).data.data;

export const completeFlashcardStudyApi = async (mode, flashcardId) =>
    (await apiClient.post(`/flashcards/student/progress/${mode}/flashcards/${flashcardId}`)).data.data;

export const resetFlashcardStudyProgressApi = async mode =>
    (await apiClient.delete(`/flashcards/student/progress/${mode}`)).data.data;

export const createFlashcardApi = async data =>
    (await apiClient.post("/flashcards", data)).data.data;

export const updateFlashcardApi = async (flashcardId, data) =>
    (await apiClient.put(`/flashcards/${flashcardId}`, data)).data.data;

export const deleteFlashcardApi = flashcardId =>
    apiClient.delete(`/flashcards/${flashcardId}`);

export const deleteFlashcardsApi = async flashcardIds =>
    (await apiClient.delete("/flashcards/bulk", { data: { flashcardIds } })).data.data;

const createImportFormData = (classRoomId, file) => {
    const formData = new FormData();
    formData.append("classRoomId", classRoomId);
    formData.append("file", file);
    return formData;
};

export const previewFlashcardImportApi = (classRoomId, file) =>
    apiClient.post("/flashcards/import/preview", createImportFormData(classRoomId, file));

export const importFlashcardsApi = async (classRoomId, file) =>
    (await apiClient.post("/flashcards/import", createImportFormData(classRoomId, file))).data.data;
