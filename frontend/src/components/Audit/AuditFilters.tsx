import type { AuditQueryParams } from '../../types';

export function AuditFilters({ filters, onChange }:{ filters: AuditQueryParams; onChange: (f: AuditQueryParams)=>void }){
  return (
    <div className="bg-white p-4 rounded">
      <form className="grid grid-cols-1 md:grid-cols-3 gap-3">
        <input type="text" placeholder="Entidade" value={filters.entityName || ''} onChange={(e)=>onChange({...filters, entityName: e.target.value})} className="border p-2 rounded" />
        <input type="text" placeholder="Usuário" value={filters.userId || ''} onChange={(e)=>onChange({...filters, userId: e.target.value})} className="border p-2 rounded" />
        <button type="button" onClick={()=>onChange({})} className="bg-gray-100 p-2 rounded">Limpar</button>
      </form>
    </div>
  );
}
