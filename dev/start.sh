#!/usr/bin/env bash
set -e

ROOT_DIR=$(cd "$(dirname "$0")/.." && pwd)

echo "🔹 Subindo LocalStack..."
cd "$ROOT_DIR/infra"
docker-compose up -d

echo "⏳ Aguardando LocalStack..."
sleep 5

if aws --endpoint-url=http://localhost:4566 sqs list-queues | grep OrderQueue > /dev/null; then
  echo "Infra já existe, pulando Terraform"
else
  echo "🔹 Aplicando Terraform..."
  cd "$ROOT_DIR/infra/terraform"
  terraform init -upgrade
  terraform apply -auto-approve
fi

echo "✅ Infra pronta"

echo "🔹 Iniciando aplicação..."
cd "$ROOT_DIR/src/Brokerage.Api" && dotnet watch run
