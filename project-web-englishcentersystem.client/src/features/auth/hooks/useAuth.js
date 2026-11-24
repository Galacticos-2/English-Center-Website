import { authStore }
from "../store/authStore";

export const useAuth = () => {

    const user = authStore.getUser();

    return {

        user,

        isAuthenticated:
            authStore.isAuthenticated(),

        logout: () => {

            authStore.clearAuth();

            window.location.href = "/login";
        }
    };
};