# Fase 2: Resiliência Financeira

## 🎯 Objetivo

Garantir que **nenhuma ordem seja perdida ou processada duas vezes**. Em sistemas financeiros, perder uma mensagem = perder dinheiro do cliente.

## 📊 Arquitetura

```
API Lambda (index.js)
    ↓
SQS Queue (OrderQueue)
    ├─> Worker Lambda (worker.js)
    │   ├─> Sucesso → DynamoDB + S3
    │   └─> Erro (3 tentativas)
    │           ↓
    └─> Dead Letter Queue (OrderQueue-DLQ)
        └─> Análise manual / Alerta
```

## 🛠️ Implementações

### 1. Dead Letter Queue (DLQ) (`init-aws.sh`)

Criação de fila separada para mensagens que falharam múltiplas vezes:

```bash
# Criar DLQ
awslocal sqs create-queue --queue-name OrderQueue-DLQ

# Obter ARN da DLQ
DLQ_ARN=$(awslocal sqs get-queue-attributes \
  --queue-url http://localhost:4566/000000000000/OrderQueue-DLQ \
  --attribute-names QueueArn \
  --output text | cut -f2)

# Criar fila principal com RedrivePolicy
awslocal sqs create-queue --queue-name OrderQueue \
  --attributes "{\"RedrivePolicy\": \"{\\\"deadLetterTargetArn\\\":\\\"$DLQ_ARN\\\",\\\"maxReceiveCount\\\":\\\"3\\\"}\", \"VisibilityTimeout\": \"30\"}"
```

**Aprendizado:**

- `maxReceiveCount=3`: Após 3 tentativas falhadas, mensagem vai para DLQ
- `VisibilityTimeout=30`: Cada tentativa tem 30s para processar
- DLQ permite análise manual de erros críticos

### 2. Idempotência no DynamoDB (`worker.js`)

Prevenção contra processamento duplicado usando Conditional Writes:

```javascript
const dynamoParams = {
  TableName: "Orders",
  Item: {
    ...orderData,
    status: "COMPLETED",
    processed_at: new Date().toISOString()
  },
  // 🔒 Só insere se order_id NÃO existir
  ConditionExpression: 'attribute_not_exists(order_id)'
};

try {
  await docClient.send(new PutCommand(dynamoParams));
} catch (dbError) {
  if (dbError.name === "ConditionalCheckFailedException") {
    console.warn(`[Worker] Order ${orderData.order_id} already exists. Skipping.`);
    continue; // Não falha, apenas ignora duplicata
  }
  throw dbError; // Outros erros são re-lançados
}
```

**Aprendizado:**

- `ConditionExpression`: Validação atômica no DynamoDB
- `attribute_not_exists(order_id)`: Falha se chave já existe
- Processar mensagem 2x não causa débito duplo

### 3. Exponential Backoff (`worker.js`)

Retry inteligente com tempo crescente entre tentativas:

```javascript
// Obter número de tentativas
const attempts = parseInt(record.attributes.ApproximateReceiveCount || "1");

// Calcular backoff: 2^1 = 2s, 2^2 = 4s, 2^3 = 8s
const newVisibilityTimeout = Math.pow(2, attempts);

console.log(
  `[Worker] Retry attempt ${attempts}. Wait ${newVisibilityTimeout}s`,
);

// Adiar próxima tentativa
await sqsClient.send(
  new ChangeMessageVisibilityCommand({
    QueueUrl: queueUrl,
    ReceiptHandle: record.receiptHandle,
    VisibilityTimeout: newVisibilityTimeout,
  }),
);
```

**Aprendizado:**

- `ApproximateReceiveCount`: SQS rastreia tentativas automaticamente
- Backoff exponencial evita sobrecarregar sistema com retries rápidos
- Após 3 falhas (8s total de espera), vai para DLQ

### 4. Validação de Erro (Teste)

```javascript
// Simular erro para testar DLQ
if (orderData.quantity <= 0) {
  throw new Error("Invalid quantity: Must be positive");
}
```

## 🔄 Fluxo com Resiliência

```
1. Mensagem chega → Worker processa
2. Se SUCESSO:
   └─> Salva DynamoDB (se não existir)
   └─> Salva S3
   └─> SQS deleta mensagem

3. Se ERRO (tentativa 1):
   └─> Worker lança exceção
   └─> SQS esconde mensagem por 2s (backoff)
   └─> Tenta novamente

4. Se ERRO (tentativa 2):
   └─> Esconde por 4s
   └─> Tenta novamente

5. Se ERRO (tentativa 3):
   └─> Esconde por 8s
   └─> Tenta novamente

6. Se ERRO (tentativa 4):
   └─> maxReceiveCount atingido
   └─> Move para DLQ (OrderQueue-DLQ)
   └─> ⚠️ Alerta time de suporte
```

## 🧪 Testes

### Teste 1: Idempotência

```bash
# Enviar mesma ordem 2x
aws lambda invoke --function-name OrderProcessorAPI \
  --endpoint-url=http://localhost:4566 --payload '{}' out1.json

aws lambda invoke --function-name OrderProcessorAPI \
  --endpoint-url=http://localhost:4566 --payload '{}' out2.json

# Verificar: deve ter apenas 1 registro no DynamoDB
awslocal dynamodb scan --table-name Orders --select COUNT
```

### Teste 2: DLQ com Backoff

```bash
# Enviar ordem inválida (quantity negativa)
aws sqs send-message \
  --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/OrderQueue \
  --message-body '{"order_id": "FAIL-TEST", "quantity": -10, "stock_symbol": "ERR4"}' \
  --endpoint-url=http://localhost:4566

# Aguardar 20s (backoff: 2s + 4s + 8s)
# Verificar DLQ
awslocal sqs receive-message \
  --queue-url http://localhost:4566/000000000000/OrderQueue-DLQ
```

### Teste 3: Logs de Retry

```bash
# Ver logs do Worker
aws logs tail /aws/lambda/OrderWorkerProcessor \
  --endpoint-url=http://localhost:4566 --follow
```

## 📚 Conceitos-Chave

- **Dead Letter Queue:** Isolamento de mensagens problemáticas para análise
- **Idempotência:** Operação pode ser repetida sem efeitos colaterais
- **Exponential Backoff:** Tempo crescente entre retries (evita overload)
- **Conditional Writes:** Validação atômica no banco de dados
- **Retry Policies:** Tentativas automáticas antes de desistir

## ⚙️ Mudanças nos Arquivos

**`init-aws.sh`:**

- ✅ Criação de OrderQueue-DLQ
- ✅ RedrivePolicy no OrderQueue (maxReceiveCount=3)

**`worker.js`:**

- ✅ ConditionExpression para idempotência
- ✅ Try-catch para detectar ConditionalCheckFailedException
- ✅ Lógica de exponential backoff com ChangeMessageVisibilityCommand
- ✅ Validação de quantity <= 0 para testes

**`deploy.sh`:**

- ✅ Passar `QUEUE_URL` também para Worker Lambda (necessário para backoff)

## ❌ Limitações Resolvidas

| Fase 1                              | Fase 2                             |
| ----------------------------------- | ---------------------------------- |
| ❌ Mensagem perdida em caso de erro | ✅ DLQ armazena para análise       |
| ❌ Retry imediato (overload)        | ✅ Exponential backoff             |
| ❌ Duplo processamento = débito 2x  | ✅ Idempotência previne duplicatas |
| ❌ Sem visibilidade de falhas       | ✅ Fila DLQ + logs estruturados    |

## ✅ Aprendizados

**Serviços AWS:** Dead Letter Queue, Conditional Expressions, VisibilityTimeout  
**Padrões:** Retry com Backoff, Idempotência, Error Handling, Circuit Breaker (básico)  
**Conceitos Financeiros:** At-most-once processing, Transactional guarantees

**Status:** ✅ Sistema resiliente a falhas temporárias e erros de validação
