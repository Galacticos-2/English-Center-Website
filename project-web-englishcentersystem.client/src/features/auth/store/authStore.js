const TOKEN_KEY = "token";
const USER_KEY = "user";

const getStorage = rememberMe =>
    rememberMe ? localStorage : sessionStorage;

export const authStore = {

    saveAuth(data, rememberMe = true) {

        this.clearAuth();

        const storage = getStorage(rememberMe);

        storage.setItem(TOKEN_KEY, data.token);
        storage.setItem(USER_KEY, JSON.stringify(data));
    },

    clearAuth() {

        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        sessionStorage.removeItem(TOKEN_KEY);
        sessionStorage.removeItem(USER_KEY);
    },

    getToken() {

        return localStorage.getItem(TOKEN_KEY)
            || sessionStorage.getItem(TOKEN_KEY);
    },

    getUser() {

        const user = localStorage.getItem(USER_KEY)
            || sessionStorage.getItem(USER_KEY);

        return user ? JSON.parse(user) : null;
    },

    isAuthenticated() {

        return !!this.getToken();
    }
};
