# Fase 9: Arquitetura Profissional (IaC com Terraform)

## 🎯 Objetivo

Migrar infraestrutura de **scripts Shell imperativos** para **Terraform declarativo** (Infrastructure as Code). Benefícios:

- **Versionamento**: Infraestrutura como código no Git
- **Reprodutibilidade**: Recriar ambiente idêntico em segundos
- **Idempotência**: Rodar múltiplas vezes = mesmo resultado
- **State Management**: Terraform rastreia estado da infraestrutura

## 📊 Antes vs Depois

**Antes (Shell Scripts):**

```bash
init-aws.sh   → awslocal s3 mb, dynamodb create-table...
deploy.sh     → zip + aws lambda create-function...

# Problemas:
# ❌ Não é idempotente (rodar 2x dá erro)
# ❌ Sem controle de estado
# ❌ Ordem importa (manual)
# ❌ Cleanup complexo
```

**Depois (Terraform):**

```hcl
terraform/
  ├── provider.tf   → Config LocalStack
  ├── database.tf   → S3 + DynamoDB
  ├── messaging.tf  → SNS + SQS + DLQ
  ├── compute.tf    → Lambdas + Triggers
  └── output.tf     → Outputs úteis

# Vantagens:
# ✅ Idempotente
# ✅ State Management (terraform.tfstate)
# ✅ Dependency Graph automático
# ✅ terraform destroy - limpa tudo
```

## 🛠️ Estrutura dos Arquivos

### 1. Provider (`provider.tf`)

Configura AWS para apontar ao LocalStack:

```hcl
provider "aws" {
  region = "us-east-1"
  access_key = "test"
  secret_key = "test"
  skip_credentials_validation = true

  endpoints {
    s3       = "http://localhost:4566"
    dynamodb = "http://localhost:4566"
    sqs      = "http://localhost:4566"
    sns      = "http://localhost:4566"
    lambda   = "http://localhost:4566"
  }
}
```

### 2. Storage (`database.tf`)

```hcl
resource "aws_s3_bucket" "receipts" {
  bucket = "order-receipts"
}

resource "aws_dynamodb_table" "orders" {
  name           = "Orders"
  hash_key       = "order_id"
  read_capacity  = 5
  write_capacity = 5

  attribute {
    name = "order_id"
    type = "S"
  }
}
```

### 3. Messaging (`messaging.tf`)

```hcl
# SNS Topic
resource "aws_sns_topic" "order_events" {
  name = "OrderEvents"
}

# SQS com DLQ
resource "aws_sqs_queue" "dlq" { name = "OrderQueue-DLQ" }

resource "aws_sqs_queue" "order_queue" {
  name = "OrderQueue"
  visibility_timeout_seconds = 30
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.dlq.arn
    maxReceiveCount     = 3
  })
}

# Fan-out Queues
resource "aws_sqs_queue" "notification_queue" { name = "NotificationQueue" }
resource "aws_sqs_queue" "audit_queue"        { name = "AuditQueue" }
resource "aws_sqs_queue" "reports_queue"      { name = "ReportsQueue" }

# SNS Subscriptions
resource "aws_sns_topic_subscription" "sub_notification" {
  topic_arn = aws_sns_topic.order_events.arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.notification_queue.arn
}
# ... (repetir para audit e reports)
```

**Aprendizado:**

- `aws_sqs_queue.dlq.arn` - Terraform resolve dependências automaticamente
- `jsonencode()` - Converte objeto HCL para JSON

### 4. Compute (`compute.tf`)

```hcl
# Empacotar código automaticamente
data "archive_file" "lambda_code" {
  type        = "zip"
  source_dir  = "${path.module}/../app"
  output_path = "${path.module}/bundle.zip"
}

# IAM Role
resource "aws_iam_role" "lambda_role" {
  name = "serverless_lambda_role"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = { Service = "lambda.amazonaws.com" }
    }]
  })
}

# Lambdas
resource "aws_lambda_function" "api" {
  filename         = data.archive_file.lambda_code.output_path
  function_name    = "OrderProcessorAPI"
  handler          = "index.handler"
  source_code_hash = data.archive_file.lambda_code.output_base64sha256
  runtime          = "nodejs18.x"
  role             = aws_iam_role.lambda_role.arn

  environment {
    variables = {
      QUEUE_URL = aws_sqs_queue.order_queue.id
    }
  }
}

# Event Source Mappings
resource "aws_lambda_event_source_mapping" "worker_trigger" {
  event_source_arn = aws_sqs_queue.order_queue.arn
  function_name    = aws_lambda_function.worker.arn
  batch_size       = 1
}
```

**Aprendizado:**

- `data source` - Lê informações externas (arquivo ZIP)
- `source_code_hash` - Terraform detecta mudanças no código e redeploya

### 5. Outputs (`output.tf`)

```hcl
output "api_lambda_name" {
  value = aws_lambda_function.api.function_name
}

output "order_queue_url" {
  value = aws_sqs_queue.order_queue.id
}
```

## 🔄 Workflow Terraform

### 1. Inicializar

```bash
terraform init  # Download providers
```

### 2. Planejar

```bash
terraform plan  # Visualizar mudanças antes de aplicar
```

**Output:**

```
Plan: 15 to add, 0 to change, 0 to destroy.
```

### 3. Aplicar

```bash
terraform apply --auto-approve  # Criar recursos
```

### 4. Destruir

```bash
terraform destroy --auto-approve  # Limpar tudo
```

## 📚 Conceitos-Chave

### 1. Declarativo vs Imperativo

| Shell (Imperativo)                 | Terraform (Declarativo)           |
| ---------------------------------- | --------------------------------- |
| Você diz **COMO** fazer (comandos) | Você declara **O QUE** quer       |
| `awslocal sqs create-queue...`     | `resource "aws_sqs_queue" {...}`  |
| Precisa lógica de "já existe?"     | Terraform resolve automaticamente |

### 2. State Management

Terraform mantém `terraform.tfstate`:

- Rastreia recursos criados
- Detecta drift (mudanças manuais)
- Permite updates (não recria tudo)

### 3. Dependency Graph

Terraform cria ordem automática:

```
IAM Role → Lambda → Event Source Mapping
SNS Topic → SQS Queue → Subscription
```

### 4. Idempotência

```bash
terraform apply  # Cria 15 recursos
terraform apply  # 0 added, 0 changed, 0 destroyed
```

## 🧪 Testes

### Deploy Completo

```bash
docker-compose up -d
cd terraform
terraform init
terraform apply --auto-approve
terraform output  # Ver URLs/nomes criados
```

### Atualizar Configuração

```hcl
# messaging.tf
resource "aws_sqs_queue" "order_queue" {
  visibility_timeout_seconds = 60  # Era 30
}
```

```bash
terraform plan   # Ver mudança
terraform apply  # UPDATE in-place (sem recriar)
```

### Adicionar Recurso

```hcl
resource "aws_sqs_queue" "new_queue" {
  name = "AnalyticsQueue"
}
```

```bash
terraform apply  # Cria só o novo recurso
```

## 🆚 Terraform vs Outros IaC

| Ferramenta         | Linguagem | Provider | State          |
| ------------------ | --------- | -------- | -------------- |
| **Terraform**      | HCL       | Multi    | Local/S3       |
| **AWS CDK**        | TS/Python | AWS      | CloudFormation |
| **CloudFormation** | YAML      | AWS      | AWS            |
| **Pulumi**         | Python/Go | Multi    | Cloud          |

**Por que Terraform?**

- Padrão de mercado (mais usado)
- Multi-cloud
- Comunidade enorme

## ⚙️ Mudanças no Workflow

**Antes:**

```bash
docker-compose up -d
./deploy.sh
```

**Depois:**

```bash
docker-compose up -d
cd terraform && terraform apply --auto-approve
```

## ✅ Aprendizados

**Conceitos IaC:**

- Declarative vs Imperative
- State Management
- Dependency Graph
- Idempotência

**Terraform:**

- Providers & Endpoints
- Resources & Data Sources
- Outputs
- References automáticas

**Benefícios:**

- ✅ Infraestrutura versionada no Git
- ✅ Replicação fácil de ambientes
- ✅ Rollback simples (git revert + apply)
- ✅ Documentação como código

**Status:** ✅ Infraestrutura profissionalizada com Terraform (padrão de mercado)
