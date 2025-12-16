import { Routes, Route, Link, useNavigate } from 'react-router-dom';
import { ProtectedRoute } from './components/Auth/ProtectedRoute';
import { useAuth } from './contexts/AuthContext';
import { Login } from './pages/Login';
import { Dashboard } from './pages/Dashboard';
import { Usuarios } from './pages/Usuarios';
import { Contas } from './pages/Contas';
import { Transacoes } from './pages/Transacoes';
import { Auditoria } from './pages/Auditoria';

function App() {
  const auth = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    auth.logout();
    navigate('/login');
  };

  return (
    <div className="min-h-screen bg-gray-100">
      <header className="bg-white shadow">
        <div className="container mx-auto px-4 py-3 flex items-center justify-between">
          <div className="flex items-center space-x-6">
            <Link to="/" className="font-bold text-lg">POC Auditoria</Link>
            {auth.isAuthenticated && (
              <>
                <Link to="/" className="text-sm text-gray-600 hover:text-gray-900">Dashboard</Link>
                <Link to="/usuarios" className="text-sm text-gray-600 hover:text-gray-900">Usuários</Link>
                <Link to="/contas" className="text-sm text-gray-600 hover:text-gray-900">Contas</Link>
                <Link to="/transacoes" className="text-sm text-gray-600 hover:text-gray-900">Transações</Link>
                <Link to="/auditoria" className="text-sm text-gray-600 hover:text-gray-900">Auditoria</Link>
              </>
            )}
          </div>
          {auth.isAuthenticated && (
            <div className="flex items-center space-x-4">
              <span className="text-sm text-gray-600">Olá, {auth.user?.username}</span>
              <button
                onClick={handleLogout}
                className="text-sm text-gray-600 hover:text-gray-900"
              >
                Sair
              </button>
            </div>
          )}
        </div>
      </header>

      <main className="container mx-auto px-4 py-6">
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
          <Route path="/usuarios" element={<ProtectedRoute><Usuarios /></ProtectedRoute>} />
          <Route path="/contas" element={<ProtectedRoute><Contas /></ProtectedRoute>} />
          <Route path="/transacoes" element={<ProtectedRoute><Transacoes /></ProtectedRoute>} />
          <Route path="/auditoria" element={<ProtectedRoute><Auditoria /></ProtectedRoute>} />
        </Routes>
      </main>
    </div>
  );
}

export default App
