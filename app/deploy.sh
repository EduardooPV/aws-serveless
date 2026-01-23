#!/bin/bash

# AWS LocalStack Configuration
ENDPOINT="http://localhost:4566"
REGION="us-east-1"
ACCOUNT="000000000000"

# Terminal Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

# Get SQS Queue URL
QUEUE_URL=$(aws sqs get-queue-url --queue-name OrderQueue --endpoint-url=$ENDPOINT --output text)

# Cleanup old deployment files
echo -e "${GREEN}Cleaning up old files...${NC}"
rm -f function.zip worker.zip 

# Package Lambda functions
echo -e "${GREEN}Creating deployment packages...${NC}"
zip -r -q function.zip index.js node_modules
zip -r -q worker.zip worker.js

# Remove existing Lambda functions
echo -e "${RED}Removing old Lambda functions...${NC}"
aws lambda delete-function --function-name OrderProcessorAPI --endpoint-url=$ENDPOINT > /dev/null 2>&1 || true
aws lambda delete-function --function-name OrderWorkerProcessor --endpoint-url=$ENDPOINT > /dev/null 2>&1 || true

# Deploy API Lambda (receives orders and queues them)
echo -e "${GREEN}Creating Lambda: OrderProcessorAPI...${NC}"
aws lambda create-function \
    --function-name OrderProcessorAPI \
    --runtime nodejs18.x \
    --zip-file fileb://function.zip \
    --handler index.handler \
    --role arn:aws:iam::${ACCOUNT}:role/lambda-role \
    --environment "Variables={QUEUE_URL=$QUEUE_URL}" \
    --endpoint-url=$ENDPOINT > /dev/null

# Deploy Worker Lambda (processes queued orders)
echo -e "${GREEN}Creating Lambda: OrderWorkerProcessor...${NC}"
aws lambda create-function \
    --function-name OrderWorkerProcessor \
    --runtime nodejs18.x \
    --zip-file fileb://worker.zip \
    --handler worker.handler \
    --role arn:aws:iam::${ACCOUNT}:role/lambda-role \
    --environment "Variables={QUEUE_URL=$QUEUE_URL}" \
    --endpoint-url=$ENDPOINT > /dev/null

# Connect SQS trigger to Worker Lambda
echo -e "${GREEN}Creating event source mapping (SQS -> Worker)...${NC}"
aws lambda create-event-source-mapping \
    --function-name OrderWorkerProcessor \
    --event-source-arn arn:aws:sqs:${REGION}:${ACCOUNT}:OrderQueue \
    --batch-size 1 \
    --endpoint-url=$ENDPOINT > /dev/null

echo -e "${GREEN}Deployment completed successfully!${NC}"