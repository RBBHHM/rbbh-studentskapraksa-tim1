import { RouterProvider } from "@tanstack/react-router";
import { createRoot } from "react-dom/client";

import { themeInitScript } from "./design-system/theme";
import { getRouter } from "./router";
import "./styles.css";

Function(themeInitScript)();

const element = document.getElementById("root");
if (!element) throw new Error("Application root element is missing.");

const router = getRouter();
createRoot(element).render(<RouterProvider router={router} />);
