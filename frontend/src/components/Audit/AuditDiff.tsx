import type { AuditEvent } from '../../types';

interface AuditDiffProps {
  event: AuditEvent;
}

export function AuditDiff({ event }: AuditDiffProps) {
  const getChangedFields = () => {
    if (!event.oldValues && !event.newValues) return [];

    const allKeys = new Set([
      ...Object.keys(event.oldValues || {}),
      ...Object.keys(event.newValues || {}),
    ]);

    return Array.from(allKeys).map((key) => ({
      field: key,
      oldValue: (event.oldValues as any)?.[key],
      newValue: (event.newValues as any)?.[key],
      changed: JSON.stringify((event.oldValues as any)?.[key]) !== 
               JSON.stringify((event.newValues as any)?.[key]),
    }));
  };

  const formatValue = (value: unknown) => {
    if (value === null || value === undefined) return '-';
    if (typeof value === 'object') return JSON.stringify(value, null, 2);
    return String(value);
  };

  const fields = getChangedFields();

  return (
    <div className="p-4">
      {/* Metadados */}
      <div className="mb-6 space-y-2">
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">ID:</span>
          <span className="font-mono">{event.id}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">Data:</span>
          <span>{new Date(event.timestamp).toLocaleString('pt-BR')}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">Operação:</span>
          <span className={`px-2 py-0.5 rounded text-xs font-medium ${
            event.operation === 'INSERT' ? 'bg-green-100 text-green-800' :
            event.operation === 'UPDATE' ? 'bg-yellow-100 text-yellow-800' :
            'bg-red-100 text-red-800'
          }`}>
            {event.operation}
          </span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">Entidade:</span>
          <span>{event.entityName}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">ID Entidade:</span>
          <span className="font-mono text-xs">{event.entityId}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">Usuário:</span>
          <span>{event.userId}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">Serviço:</span>
          <span>{event.sourceService}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">Correlation ID:</span>
          <span className="font-mono text-xs">{event.correlationId}</span>
        </div>
      </div>

      {/* Diff de campos */}
      <div className="border-t pt-4">
        <h3 className="font-semibold mb-3">Alterações</h3>
        <div className="space-y-3">
          {fields.map(({ field, oldValue, newValue, changed }) => (
            <div 
              key={field} 
              className={`p-3 rounded ${changed ? 'bg-yellow-50' : 'bg-gray-50'}`}
            >
              <div className="font-medium text-sm mb-2">{field}</div>
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <div className="text-xs text-gray-500 mb-1">Anterior</div>
                  <div className={`p-2 rounded font-mono text-xs ${
                    event.operation === 'INSERT' ? 'bg-gray-100' :
                    changed ? 'bg-red-100 text-red-800' : 'bg-gray-100'
                  }`}>
                    <pre className="whitespace-pre-wrap break-all">{formatValue(oldValue)}</pre>
                  </div>
                </div>
                <div>
                  <div className="text-xs text-gray-500 mb-1">Novo</div>
                  <div className={`p-2 rounded font-mono text-xs ${
                    event.operation === 'DELETE' ? 'bg-gray-100' :
                    changed ? 'bg-green-100 text-green-800' : 'bg-gray-100'
                  }`}>
                    <pre className="whitespace-pre-wrap break-all">{formatValue(newValue)}</pre>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
