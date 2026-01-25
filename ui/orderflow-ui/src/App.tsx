import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { Layout } from "./components/Layout";
import { OrdersPage } from "./pages/OrdersPage";
import { OutboxPage } from "./pages/OutboxPage";

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Layout = barra de arriba */}
        <Route element={<Layout />}>
          {/* Si entras a "/" te manda a "/orders" */}
          <Route path="/" element={<Navigate to="/orders" replace />} />

          {/* Pantallas principales */}
          <Route path="/orders" element={<OrdersPage />} />
          <Route path="/outbox" element={<OutboxPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}