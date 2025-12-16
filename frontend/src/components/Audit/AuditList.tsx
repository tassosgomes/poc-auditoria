import type { AuditEvent } from '../../types';

export function AuditList({ events, loading, onSelect, selectedId } : {
  events: AuditEvent[];
  loading: boolean;
  onSelect: (e: AuditEvent) => void;
  selectedId?: string;
}) {
  if (loading) return <div className="p-4">Carregando...</div>;
  if (!events.length) return <div className="p-4 text-gray-500">Nenhum evento encontrado</div>;

  return (
    <div className="divide-y">
      {events.map((ev) => (
        <div key={ev.id} className={`p-3 cursor-pointer hover:bg-gray-50 ${selectedId===ev.id ? 'bg-blue-50' : ''}`} onClick={() => onSelect(ev)}>
          <div className="text-sm font-medium">{ev.entityName} ({ev.operation})</div>
          <div className="text-xs text-gray-500">{new Date(ev.timestamp).toLocaleString()}</div>
        </div>
      ))}
    </div>
  );
}
