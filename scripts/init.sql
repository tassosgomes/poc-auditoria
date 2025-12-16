-- ===========================================
-- Extensões necessárias
-- ===========================================
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ===========================================
-- Schema: contas (MS-Contas)
-- ===========================================
CREATE SCHEMA IF NOT EXISTS contas;

CREATE TABLE IF NOT EXISTS contas.usuarios (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nome VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    senha_hash VARCHAR(255) NOT NULL,
    ativo BOOLEAN DEFAULT TRUE,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS contas.contas_bancarias (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    numero_conta VARCHAR(20) NOT NULL UNIQUE,
    usuario_id UUID NOT NULL REFERENCES contas.usuarios(id),
    saldo DECIMAL(18,2) DEFAULT 0.00,
    tipo VARCHAR(20) NOT NULL,
    ativa BOOLEAN DEFAULT TRUE,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_contas_usuario ON contas.contas_bancarias(usuario_id);

-- ===========================================
-- Schema: transacoes (MS-Transacoes)
-- ===========================================
CREATE SCHEMA IF NOT EXISTS transacoes;

CREATE TABLE IF NOT EXISTS transacoes.transacoes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conta_origem_id UUID NOT NULL,
    conta_destino_id UUID,
    tipo VARCHAR(20) NOT NULL,
    valor DECIMAL(18,2) NOT NULL,
    descricao VARCHAR(255),
    status VARCHAR(20) DEFAULT 'PENDENTE',
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    processado_em TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_transacoes_conta_origem ON transacoes.transacoes(conta_origem_id);
CREATE INDEX IF NOT EXISTS idx_transacoes_data ON transacoes.transacoes(criado_em);

-- ===========================================
-- Dados Iniciais (Seed)
-- ===========================================
INSERT INTO contas.usuarios (id, nome, email, senha_hash) VALUES 
    ('11111111-1111-1111-1111-111111111111', 'Administrador', 'admin@poc.com', 'admin123'),
    ('22222222-2222-2222-2222-222222222222', 'Usuário Teste', 'user@poc.com', 'user123')
ON CONFLICT (email) DO NOTHING;

INSERT INTO contas.contas_bancarias (numero_conta, usuario_id, saldo, tipo) VALUES
    ('0001-1', '11111111-1111-1111-1111-111111111111', 10000.00, 'CORRENTE'),
    ('0001-2', '22222222-2222-2222-2222-222222222222', 5000.00, 'CORRENTE'),
    ('0002-1', '22222222-2222-2222-2222-222222222222', 2000.00, 'POUPANCA')
ON CONFLICT (numero_conta) DO NOTHING;
