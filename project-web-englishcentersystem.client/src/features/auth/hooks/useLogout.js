import { useNavigate }
    from "react-router-dom";

import { authStore }
    from "../store/authStore";

export const useLogout = () => {

    const navigate = useNavigate();

    const logout = () => {

        authStore.clearAuth();

        navigate("/login");
    };

    return logout;
};