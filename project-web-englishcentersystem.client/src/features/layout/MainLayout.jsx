import {
    Outlet
} from "react-router-dom";

import DashboardNavbar
    from "./components/DashboardNavbar";

import {
    NotificationProvider
} from "../notification/context/NotificationProvider";

import "./styles/layout.css";

function MainLayout() {
    return (
        <NotificationProvider>
            <div className="main-layout">
                <DashboardNavbar />

                <main className="main-layout-content">
                    <Outlet />
                </main>
            </div>
        </NotificationProvider>
    );
}

export default MainLayout;