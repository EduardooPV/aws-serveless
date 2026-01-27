# Fase 3: Notificações & Fan-out (Padrão SNS)

## 🎯 Objetivo

Implementar **comunicação desacoplada entre serviços** usando o padrão Pub/Sub. Quando uma ordem é processada, múltiplos sistemas precisam reagir simultaneamente:

- **Notificações**: Enviar email/SMS ao cliente
- **Auditoria**: Registrar para compliance (CVM)
- **Relatórios**: Gerar dashboards para BackOffice

## 📊 Arquitetura

```
API Lambda (index.js)
    ↓
SQS Queue (OrderQueue)
    ↓
Worker Lambda (worker.js)
    ├─> DynamoDB + S3
    ├─> ✨ SNS Topic (OrderEvents)
          ├──> NotificationQueue → NotificationService Lambda
          ├──> AuditQueue (consumida por futura Lambda)
          └──> ReportsQueue (consumida por futura Lambda)
```

## 🆚 Comparação: Antes vs Depois

### ❌ Antes (Acoplamento Direto)

```javascript
// Worker tinha que conhecer TODOS os sistemas
await enviarEmail(orderData);
await salvarAuditoria(orderData);
await gerarRelatorio(orderData);
// Adicionar novo sistema = modificar Worker!
```

### ✅ Depois (Padrão Fan-out)

```javascript
// Worker só publica evento - não conhece consumidores
await snsClient.send(
  new PublishCommand({
    TopicArn: "OrderEvents",
    Message: JSON.stringify(orderData),
  }),
);
// Adicionar novo sistema = criar nova fila + subscription
```

## 🛠️ Implementações

### 1. Infraestrutura SNS (`init-aws.sh`)

Criação do tópico SNS e filas de fan-out:

```bash
# 5. Criar tópico SNS
echo "Creating SNS Topic 'OrderEvents'..."
awslocal sns create-topic --name OrderEvents
TOPIC_ARN="arn:aws:sns:us-east-1:000000000000:OrderEvents"

# 6. Criar filas consumidoras (fan-out)
echo "Creating Fan-out Queues..."
awslocal sqs create-queue --queue-name NotificationQueue
awslocal sqs create-queue --queue-name AuditQueue
awslocal sqs create-queue --queue-name ReportsQueue

# 7. Obter ARNs das filas
NOTIFY_ARN="arn:aws:sqs:us-east-1:000000000000:NotificationQueue"
AUDIT_ARN="arn:aws:sqs:us-east-1:000000000000:AuditQueue"
REPORTS_ARN="arn:aws:sqs:us-east-1:000000000000:ReportsQueue"

# 8. Criar subscriptions (SNS → SQS)
awslocal sns subscribe --topic-arn $TOPIC_ARN --protocol sqs --notification-endpoint $NOTIFY_ARN
awslocal sns subscribe --topic-arn $TOPIC_ARN --protocol sqs --notification-endpoint $AUDIT_ARN
awslocal sns subscribe --topic-arn $TOPIC_ARN --protocol sqs --notification-endpoint $REPORTS_ARN
```

**Aprendizado:**

- **SNS Topic**: Canal de comunicação Pub/Sub (1 publicador, N assinantes)
- **Subscriptions**: Conectam tópico SNS às filas SQS automaticamente
- **Protocol SQS**: SNS entrega mensagem diretamente na fila (sem polling manual)

### 2. Publicação no Worker (`worker.js`)

Após processar ordem, publicar evento no SNS:

```javascript
const { SNSClient, PublishCommand } = require("@aws-sdk/client-sns");

const snsClient = new SNSClient(config);

// Após salvar no DynamoDB e S3 com sucesso
const topicArn =
  process.env.TOPIC_ARN || "arn:aws:sns:us-east-1:000000000000:OrderEvents";

const snsParams = {
  TopicArn: topicArn,
  Message: JSON.stringify(orderData),
  Subject: `Order Processed: ${orderData.order_id}`,
};

await snsClient.send(new PublishCommand(snsParams));
console.log(`[Worker Lambda] Event published to SNS Topic: OrderEvents`);
```

**Aprendizado:**

- **PublishCommand**: Envia mensagem ao tópico SNS
- **Message**: Payload JSON com dados da ordem
- **Subject**: Título do evento (usado em emails, logs)
- SNS entrega **automaticamente** para todas as filas inscritas

### 3. Lambda de Notificação (`notification.js`)

Nova Lambda para simular envio de email/SMS:

```javascript
exports.handler = async (event) => {
  for (const record of event.Records) {
    try {
      // Mensagem SQS vinda do SNS tem formato específico
      const snsBody = JSON.parse(record.body);
      const orderData = JSON.parse(snsBody.Message);

      console.log("------------------------------------------------");
      console.log("📧 NOTIFICATION SERVICE");
      console.log(`📩 Enviando e-mail para: ${orderData.customer}`);
      console.log(`🆔 Pedido Confirmado: ${orderData.order_id}`);
      console.log("✅ E-mail enviado com sucesso!");
      console.log("------------------------------------------------");
    } catch (error) {
      console.error("❌ Erro ao processar a mensagem:", error);
    }
  }
};
```

**Aprendizado:**

- **Double Parsing**: SQS envelopa mensagem SNS (`record.body` → `snsBody.Message`)
- **Idempotente**: Não lança erro para evitar reprocessamento (logs apenas)
- **Simulação**: Em produção, integrar com Amazon SES, Twilio, etc.

### 4. Deploy Atualizado (`deploy.sh`)

Empacotamento e deploy da Lambda de Notificação:

```bash
# Empacotar notification.js
zip -r -q notification.zip notification.js node_modules

# Criar Lambda
aws lambda create-function \
    --function-name NotificationService \
    --runtime nodejs18.x \
    --zip-file fileb://notification.zip \
    --handler notification.handler \
    --role arn:aws:iam::${ACCOUNT}:role/lambda-role \
    --endpoint-url=$ENDPOINT

# Conectar trigger: NotificationQueue → Lambda
aws lambda create-event-source-mapping \
    --function-name NotificationService \
    --event-source-arn arn:aws:sqs:${REGION}:${ACCOUNT}:NotificationQueue \
    --batch-size 1 \
    --endpoint-url=$ENDPOINT
```

**Aprendizado:**

- **Event Source Mapping**: Conecta fila SQS à Lambda automaticamente
- **batch-size 1**: Processa 1 mensagem por vez (pode ser até 10)
- Worker Lambda agora recebe `TOPIC_ARN` via variável de ambiente

### 5. Dependências (`package.json`)

Adicionado SDK do SNS:

```json
"dependencies": {
  "@aws-sdk/client-sns": "^3.975.0"
}
```

## 🔄 Fluxo Completo com Fan-out

```
1. API Lambda → Ordem vai para OrderQueue
2. Worker Lambda consome ordem
3. Worker salva DynamoDB + S3
4. Worker publica no SNS Topic "OrderEvents"
5. SNS entrega mensagem para 3 filas:
   ├─> NotificationQueue → NotificationService Lambda → 📧 Email
   ├─> AuditQueue → (futura Lambda de Compliance)
   └─> ReportsQueue → (futura Lambda de Analytics)
6. Cada serviço processa INDEPENDENTEMENTE
```

## 🎭 Como é no Painel da AWS

### 1. Criar SNS Topic

- Console AWS → SNS → Topics → Create topic
- Type: **Standard** (não FIFO)
- Name: `OrderEvents`

### 2. Criar Subscriptions

- Topic "OrderEvents" → Create subscription
- Protocol: **Amazon SQS**
- Endpoint: `arn:aws:sqs:us-east-1:123456789012:NotificationQueue`
- Repetir para AuditQueue e ReportsQueue

### 3. Permissões Automáticas

- SNS automaticamente adiciona policy na SQS permitindo `SendMessage`
- Visível em: SQS → Queue → Access Policy (JSON)

### 4. Monitoramento

- CloudWatch Metrics:
  - `NumberOfMessagesPublished` (SNS)
  - `NumberOfNotificationsFailed` (SNS)
  - `ApproximateNumberOfMessagesVisible` (SQS)

## 🧪 Testes

### Teste 1: Fan-out Funcionando

```bash
# Enviar ordem
aws lambda invoke --function-name OrderProcessorAPI \
  --endpoint-url=http://localhost:4566 response.json

# Verificar que as 3 filas receberam mensagem
awslocal sqs receive-message --queue-url http://localhost:4566/000000000000/NotificationQueue
awslocal sqs receive-message --queue-url http://localhost:4566/000000000000/AuditQueue
awslocal sqs receive-message --queue-url http://localhost:4566/000000000000/ReportsQueue
```

### Teste 2: Lambda de Notificação

```bash
# Ver logs da Lambda (simula envio de email)
aws logs tail /aws/lambda/NotificationService \
  --endpoint-url=http://localhost:4566 --follow
```

**Saída esperada:**

```
📧 NOTIFICATION SERVICE
📩 Enviando e-mail para: Maria Lambda
🆔 Pedido Confirmado: ORD-LAMBDA-1769515222195
✅ E-mail enviado com sucesso!
```

### Teste 3: Verificar Subscriptions

```bash
# Listar subscriptions do tópico
awslocal sns list-subscriptions-by-topic \
  --topic-arn arn:aws:sns:us-east-1:000000000000:OrderEvents
```

## 📚 Conceitos-Chave

- **Pub/Sub (Publish-Subscribe)**: Padrão onde publicador não conhece consumidores
- **Fan-out**: 1 mensagem → N destinos simultaneamente
- **Desacoplamento**: Worker não depende de sistemas downstream
- **Escalabilidade**: Adicionar novo serviço não altera código existente
- **Event-Driven Architecture**: Sistemas reagem a eventos, não chamadas diretas

## ⚙️ Mudanças nos Arquivos

**`init-aws.sh`:**

- ✅ Criação do SNS Topic `OrderEvents`
- ✅ Criação de 3 filas: NotificationQueue, AuditQueue, ReportsQueue
- ✅ Subscriptions SNS → SQS

**`worker.js`:**

- ✅ Import do `@aws-sdk/client-sns`
- ✅ Publicação no SNS após sucesso no DynamoDB/S3
- ✅ Variável de ambiente `TOPIC_ARN`

**`notification.js` (NOVO):**

- ✅ Lambda que consome NotificationQueue
- ✅ Simula envio de email com logs estruturados
- ✅ Double parsing (SQS + SNS envelope)

**`deploy.sh`:**

- ✅ Empacotamento de `notification.zip`
- ✅ Deploy da Lambda `NotificationService`
- ✅ Event Source Mapping: NotificationQueue → Lambda
- ✅ Variável `TOPIC_ARN` para Worker Lambda

**`package.json`:**

- ✅ Adicionado `@aws-sdk/client-sns`

**`docker-compose.yml`:**

- ✅ Adicionado serviço `sns` ao LocalStack

## 🆚 Vantagens do Padrão Fan-out

| Aspecto                 | Sem SNS (Acoplado)               | Com SNS (Fan-out)             |
| ----------------------- | -------------------------------- | ----------------------------- |
| **Escalabilidade**      | Difícil adicionar serviços       | Adicionar fila + subscription |
| **Manutenção**          | Modificar Worker para tudo       | Worker não muda               |
| **Tolerância a Falhas** | Falha em 1 serviço bloqueia tudo | Cada fila independente        |
| **Performance**         | Processamento sequencial         | Processamento paralelo        |
| **Rastreabilidade**     | Logs misturados no Worker        | Logs separados por serviço    |

## ✅ Aprendizados

**Serviços AWS:** SNS (Simple Notification Service), SNS Subscriptions, Event Source Mapping  
**Padrões:** Fan-out, Pub/Sub, Event-Driven Architecture, Decoupling  
**Conceitos:** Topic ARN, Message Envelope, Double Parsing, Independent Scaling  
**Produção:** Amazon SES (email), SNS SMS, EventBridge (alternativa mais avançada)

**Status:** ✅ Sistema desacoplado com notificações assíncronas via SNS Fan-out
