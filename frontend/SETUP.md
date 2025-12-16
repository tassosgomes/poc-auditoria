# Setup rápido

Local development:

```bash
cd frontend
npm install
npm run dev
```

Build for production:

```bash
npm run build
```

Docker (build and run):

```bash
docker build -t poc-frontend:local .
docker run -p 3000:3000 poc-frontend:local
```

Environment variables (opcionais):
- `VITE_API_CONTAS`
- `VITE_API_TRANSACOES`
- `VITE_API_AUDITORIA`

Credenciais de teste: `admin/admin123` e `user/user123`.
