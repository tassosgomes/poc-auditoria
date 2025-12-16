#!/bin/bash
# build.sh - Script de build completo para POC Auditoria

set -e

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================"
echo "POC Auditoria - Build Completo"
echo "========================================${NC}"
echo ""

# Função para exibir erros
error_exit() {
    echo -e "${RED}Erro: $1${NC}" >&2
    exit 1
}

# Verificar se os diretórios existem
[ -d "ms-contas" ] || error_exit "Diretório ms-contas não encontrado"
[ -d "ms-transacoes" ] || error_exit "Diretório ms-transacoes não encontrado"
[ -d "ms-auditoria" ] || error_exit "Diretório ms-auditoria não encontrado"
[ -d "frontend" ] || error_exit "Diretório frontend não encontrado"

# Build MS-Contas (Java)
echo -e "${YELLOW}[1/5] Building MS-Contas (Java/Spring Boot)...${NC}"
cd ms-contas
if [ -f "mvnw" ]; then
    ./mvnw clean package -DskipTests || error_exit "Falha ao compilar MS-Contas"
elif command -v mvn &> /dev/null; then
    mvn clean package -DskipTests || error_exit "Falha ao compilar MS-Contas"
else
    echo -e "${RED}Maven não encontrado. Pulando build local do MS-Contas.${NC}"
    echo -e "${YELLOW}O build será realizado no Docker.${NC}"
fi
cd ..
echo -e "${GREEN}✓ MS-Contas build concluído${NC}"
echo ""

# Build MS-Transacoes (.NET)
echo -e "${YELLOW}[2/5] Building MS-Transacoes (.NET 8)...${NC}"
cd ms-transacoes
if command -v dotnet &> /dev/null; then
    dotnet publish -c Release -o ./publish || error_exit "Falha ao compilar MS-Transacoes"
else
    echo -e "${RED}.NET SDK não encontrado. Pulando build local do MS-Transacoes.${NC}"
    echo -e "${YELLOW}O build será realizado no Docker.${NC}"
fi
cd ..
echo -e "${GREEN}✓ MS-Transacoes build concluído${NC}"
echo ""

# Build MS-Auditoria (.NET)
echo -e "${YELLOW}[3/5] Building MS-Auditoria (.NET 8)...${NC}"
cd ms-auditoria
if command -v dotnet &> /dev/null; then
    dotnet publish -c Release -o ./publish || error_exit "Falha ao compilar MS-Auditoria"
else
    echo -e "${RED}.NET SDK não encontrado. Pulando build local do MS-Auditoria.${NC}"
    echo -e "${YELLOW}O build será realizado no Docker.${NC}"
fi
cd ..
echo -e "${GREEN}✓ MS-Auditoria build concluído${NC}"
echo ""

# Build Frontend (React)
echo -e "${YELLOW}[4/5] Building Frontend (React/Vite)...${NC}"
cd frontend
if command -v npm &> /dev/null; then
    npm ci || npm install || error_exit "Falha ao instalar dependências do Frontend"
    npm run build || error_exit "Falha ao compilar Frontend"
else
    echo -e "${RED}npm não encontrado. Pulando build local do Frontend.${NC}"
    echo -e "${YELLOW}O build será realizado no Docker.${NC}"
fi
cd ..
echo -e "${GREEN}✓ Frontend build concluído${NC}"
echo ""

# Build Docker images
echo -e "${YELLOW}[5/5] Building Docker images...${NC}"
if command -v docker-compose &> /dev/null; then
    docker-compose build || error_exit "Falha ao criar imagens Docker"
elif command -v docker &> /dev/null && docker compose version &> /dev/null; then
    docker compose build || error_exit "Falha ao criar imagens Docker"
else
    error_exit "Docker Compose não encontrado"
fi
echo -e "${GREEN}✓ Docker images criadas${NC}"
echo ""

# Resumo
echo -e "${GREEN}========================================"
echo "Build concluído com sucesso!"
echo "========================================${NC}"
echo ""
echo -e "${BLUE}Próximos passos:${NC}"
echo "  1. Configure o arquivo .env (use .env.example como base)"
echo "  2. Inicie o ambiente:"
echo "     ${YELLOW}docker-compose up -d${NC}"
echo "  3. Acompanhe os logs:"
echo "     ${YELLOW}docker-compose logs -f${NC}"
echo "  4. Acesse o frontend em:"
echo "     ${YELLOW}http://localhost:3000${NC}"
echo ""
