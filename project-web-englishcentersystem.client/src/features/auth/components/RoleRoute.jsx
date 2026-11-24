import { Navigate }
from "react-router-dom";

import { authStore }
from "../store/authStore";

function RoleRoute({
    children,
    allowedRoles
}) {

    const user =
        authStore.getUser();

    if (!user) {

        return <Navigate to="/login" />;
    }

    if (
        !allowedRoles.includes(
            user.role
        )
    ) {

        return <Navigate to="/login" />;
    }

    return children;
}

export default RoleRoute;