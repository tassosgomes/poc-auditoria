import { Routes, Route, Link, useNavigate, useLocation } from 'react-router-dom';
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
  const location = useLocation();
  const isLoginPage = location.pathname === '/login';

  const handleLogout = () => {
    auth.logout();
    navigate('/login');
  };

  if (isLoginPage) {
    return <Login />;
  }

  return (
    <div className="min-h-screen bg-white">
      <header className="bg-black border-b border-gray-900">
        <div className="container mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center space-x-8">
            <Link to="/" className="font-bold text-xl text-white">POC Auditoria</Link>
            {auth.isAuthenticated && (
              <>
                <Link to="/" className="text-sm text-gray-400 hover:text-white transition-colors">Dashboard</Link>
                <Link to="/usuarios" className="text-sm text-gray-400 hover:text-white transition-colors">Usuários</Link>
                <Link to="/contas" className="text-sm text-gray-400 hover:text-white transition-colors">Contas</Link>
                <Link to="/transacoes" className="text-sm text-gray-400 hover:text-white transition-colors">Transações</Link>
                <Link to="/auditoria" className="text-sm text-gray-400 hover:text-white transition-colors">Auditoria</Link>
              </>
            )}
          </div>
          {auth.isAuthenticated && (
            <div className="flex items-center space-x-6">
              <span className="text-sm text-gray-400">Olá, {auth.user?.username}</span>
              <button
                onClick={handleLogout}
                className="text-sm text-gray-400 hover:text-white bg-white/5 px-4 py-2 rounded-lg transition-colors"
              >
                Sair
              </button>
            </div>
          )}
        </div>
      </header>

      <main className="container mx-auto px-6 py-8">
        <Routes>
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
