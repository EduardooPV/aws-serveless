# Fase 1: Fundação Básica

## 🎯 Objetivo

Construir fluxo assíncrono ponta a ponta: API → SQS → Worker → DynamoDB/S3

## 📊 Arquitetura

```
API Lambda (index.js)
    ↓ envia mensagem
SQS Queue (OrderQueue)
    ↓ polling automático
Worker Lambda (worker.js)
    ↓ persiste
DynamoDB (Orders) + S3 (order-receipts)
```

## 🛠️ Componentes

### 1. LocalStack (`docker-compose.yml`)

- Simula AWS localmente na porta 4566
- Scripts em `/etc/localstack/init/ready.d/` executam automaticamente

### 2. Infraestrutura (`init-aws.sh`)

```bash
awslocal s3 mb s3://order-receipts
awslocal dynamodb create-table --table-name Orders \
  --attribute-definitions AttributeName=order_id,AttributeType=S \
  --key-schema AttributeName=order_id,KeyType=HASH
awslocal sqs create-queue --queue-name OrderQueue
```

**Aprendizado:** `awslocal` = wrapper do AWS CLI. DynamoDB precisa de KeySchema + AttributeDefinitions.

### 3. API Lambda (`index.js`)

- Recebe ordem → Gera ID único → Envia para SQS
- Usa `LOCALSTACK_HOSTNAME` para rede interna do Docker
- AWS SDK v3 modular: `@aws-sdk/client-sqs`

**Aprendizado:** `SendMessageCommand` é assíncrono, retorna imediatamente.

### 4. Worker Lambda (`worker.js`)

- Consome `event.Records` do SQS
- Salva no DynamoDB (`PutCommand`)
- Salva comprovante no S3 (`PutObjectCommand`)

**Aprendizado:** Event Source Mapping conecta SQS→Lambda automaticamente. `DynamoDBDocumentClient` simplifica tipos.

### 5. Deploy (`deploy.sh`)

- Empacota ZIP com código + node_modules
- Cria Lambdas via AWS CLI
- Cria Event Source Mapping (SQS trigger)

**Aprendizado:** `--handler index.handler` = arquivo.função. ARN fake `000000000000` no LocalStack.

## 🔄 Fluxo Completo

```
1. API Lambda invocada → gera ORD-LAMBDA-{timestamp}
2. SQS recebe → mantém invisível 30s
3. Event Source Mapping → invoca Worker
4. Worker processa → salva DynamoDB + S3
5. SQS deleta mensagem (se sucesso)
```

## 🧪 Teste Rápido

```bash
# Deploy
docker-compose up -d
cd app && npm install && ./deploy.sh

# Invocar
aws lambda invoke --function-name OrderProcessorAPI \
  --endpoint-url=http://localhost:4566 --payload '{}' output.json

# Verificar
awslocal dynamodb scan --table-name Orders
awslocal s3 ls s3://order-receipts/
```

## 📚 Conceitos-Chave

- **Processamento Assíncrono:** SQS desacopla API de processamento longo
- **Event-Driven:** Worker reage a eventos, não é chamada diretamente
- **Persistência Dual:** DynamoDB (queries) + S3 (auditoria)
- **Multi-Ambiente:** Código detecta LocalStack via env vars

## ✅ Aprendizados

**Serviços AWS:** Lambda, SQS, DynamoDB, S3, Event Source Mapping  
**Padrões:** Queue-Based Load Leveling, Event-Driven Architecture  
**Ferramentas:** LocalStack, AWS SDK v3, Shell Scripting, Docker Compose
