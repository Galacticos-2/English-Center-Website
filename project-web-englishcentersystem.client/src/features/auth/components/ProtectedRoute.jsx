import { Navigate }
from "react-router-dom";

import { authStore }
from "../store/authStore";

function ProtectedRoute({ children }) {

    const isAuthenticated =
        authStore.isAuthenticated();

    if (!isAuthenticated) {

        return <Navigate to="/login" />;
    }

    return children;
}

export default ProtectedRoute;