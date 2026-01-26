import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App";

import "./index.css"; // ✅ CSS global
import "./App.css";   // ✅ opcional (puede estar vacio)

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
