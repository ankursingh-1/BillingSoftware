import { Outlet } from "react-router-dom";

import Header from "../components/layout/Header";
import Sidebar from "../components/layout/Sidebar";

function MainLayout() {

    return (

        <div
            style={{
                display: "flex",
                minHeight: "100vh",
                background: "#f4f7fb"
            }}
        >

            <Sidebar />

            <div
                style={{
                    flex: 1,
                    display: "flex",
                    flexDirection: "column"
                }}
            >

                <Header />

                <main
                    style={{
                        flex: 1,
                        padding: "25px"
                    }}
                >

                    <Outlet />

                </main>

            </div>

        </div>

    );

}

export default MainLayout;