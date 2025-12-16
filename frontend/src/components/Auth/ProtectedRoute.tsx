import type { ReactNode } from 'react';
import { useAuth } from '../../contexts/AuthContext';
import { Navigate } from 'react-router-dom';

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const auth = useAuth();
  if (!auth.isAuthenticated) return <Navigate to="/login" replace />;
  return <>{children}</>;
}
