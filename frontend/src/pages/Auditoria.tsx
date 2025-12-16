import { useState, useEffect } from 'react';
import { auditoriaService } from '../services/auditoriaApi';
import { AuditList } from '../components/Audit/AuditList';
import { AuditFilters } from '../components/Audit/AuditFilters';
import { AuditDiff } from '../components/Audit/AuditDiff';
import type { AuditEvent, AuditQueryParams } from '../types';

export function Auditoria() {
  const [events, setEvents] = useState<AuditEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedEvent, setSelectedEvent] = useState<AuditEvent | null>(null);
  const [filters, setFilters] = useState<AuditQueryParams>({});

  useEffect(() => {
    loadEvents();
  }, [filters]);

  const loadEvents = async () => {
    setLoading(true);
    try {
      const response = await auditoriaService.listar(filters);
      setEvents(response.data);
    } catch (error) {
      console.error('Erro ao carregar eventos:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-6">Auditoria</h1>
      
      <AuditFilters filters={filters} onChange={setFilters} />
      
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-6">
        <div className="bg-white rounded-lg shadow">
          <div className="p-4 border-b">
            <h2 className="text-lg font-semibold">Eventos</h2>
          </div>
          <AuditList 
            events={events} 
            loading={loading}
            onSelect={setSelectedEvent}
            selectedId={selectedEvent?.id}
          />
        </div>
        
        <div className="bg-white rounded-lg shadow">
          <div className="p-4 border-b">
            <h2 className="text-lg font-semibold">Detalhes</h2>
          </div>
          {selectedEvent ? (
            <AuditDiff event={selectedEvent} />
          ) : (
            <div className="p-8 text-center text-gray-500">
              Selecione um evento para ver os detalhes
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
