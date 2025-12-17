import { useState } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { useNavigate } from 'react-router-dom';

export function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const auth = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    const ok = await auth.login(username, password);
    if (ok) {
      navigate('/');
    } else {
      setError('Credenciais inválidas');
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-black">
      <div className="w-full max-w-md px-6">
        <div className="mb-12">
          <h1 className="text-4xl font-bold text-white mb-2">POC Auditoria</h1>
          <p className="text-gray-400 text-lg">Entre com suas credenciais</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <input 
              value={username} 
              onChange={(e) => setUsername(e.target.value)} 
              placeholder="Usuário"
              className="w-full bg-white/5 border border-gray-700 text-white placeholder-gray-500 px-4 py-4 rounded-lg focus:outline-none focus:border-white focus:bg-white/10 transition-all" 
            />
          </div>
          <div>
            <input 
              type="password" 
              value={password} 
              onChange={(e) => setPassword(e.target.value)} 
              placeholder="Senha"
              className="w-full bg-white/5 border border-gray-700 text-white placeholder-gray-500 px-4 py-4 rounded-lg focus:outline-none focus:border-white focus:bg-white/10 transition-all" 
            />
          </div>

          {error && (
            <div className="text-red-400 text-sm bg-red-500/10 border border-red-500/20 px-4 py-3 rounded-lg">
              {error}
            </div>
          )}

          <button 
            type="submit" 
            className="w-full bg-white text-black font-semibold py-4 rounded-lg hover:bg-gray-200 transition-all transform active:scale-98"
          >
            Entrar
          </button>
        </form>

        <div className="mt-8 text-center">
          <p className="text-gray-600 text-sm">Sistema de Auditoria de Transações</p>
        </div>
      </div>
    </div>
  );
}
