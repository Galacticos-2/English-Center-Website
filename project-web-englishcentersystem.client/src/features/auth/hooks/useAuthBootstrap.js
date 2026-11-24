import { useEffect } from "react";

import { authStore }
    from "../store/authStore";

export const useAuthBootstrap = () => {

    useEffect(() => {

        const token =
            authStore.getToken();

        if (!token) {

            authStore.clearAuth();
        }

    }, []);
};