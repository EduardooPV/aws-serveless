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
ORDER_QUEUE_URL="$ENDPOINT/$ACCOUNT/OrderQueue"
NOTIFY_QUEUE_URL="$ENDPOINT/$ACCOUNT/NotificationQueue"
TOPIC_ARN="arn:aws:sns:${REGION}:${ACCOUNT}:OrderEvents"

# Cleanup old deployment files
echo -e "${RED}Cleaning up old files...${NC}"
rm -f function.zip worker.zip notification.zip

# Package Lambda functions
echo -e "${GREEN}Packaging...${NC}"
zip -r -q function.zip index.js node_modules
zip -r -q worker.zip worker.js node_modules
zip -r -q notification.zip notification.js node_modules

# Remove existing Lambda functions
echo -e "${RED}Removing old functions...${NC}"
aws lambda delete-function --function-name OrderProcessorAPI --endpoint-url=$ENDPOINT > /dev/null 2>&1 || true
aws lambda delete-function --function-name OrderWorkerProcessor --endpoint-url=$ENDPOINT > /dev/null 2>&1 || true
aws lambda delete-function --function-name NotificationService --endpoint-url=$ENDPOINT > /dev/null 2>&1 || true

# Deploy API Lambda (receives orders and queues them)
echo -e "${GREEN}Creating Lambda: OrderProcessorAPI...${NC}"
aws lambda create-function \
    --function-name OrderProcessorAPI \
    --runtime nodejs18.x \
    --zip-file fileb://function.zip \
    --handler index.handler \
    --role arn:aws:iam::${ACCOUNT}:role/lambda-role \
    --environment "Variables={QUEUE_URL=$ORDER_QUEUE_URL}" \
    --endpoint-url=$ENDPOINT > /dev/null


# Deploy Worker Lambda (processes queued orders)
echo -e "${GREEN}Creating Lambda: OrderWorkerProcessor...${NC}"
aws lambda create-function \
    --function-name OrderWorkerProcessor \
    --runtime nodejs18.x \
    --zip-file fileb://worker.zip \
    --handler worker.handler \
    --role arn:aws:iam::${ACCOUNT}:role/lambda-role \
    --environment "Variables={QUEUE_URL=$ORDER_QUEUE_URL,TOPIC_ARN=$TOPIC_ARN}" \
    --endpoint-url=$ENDPOINT > /dev/null

# Connect SQS trigger to Worker Lambda
echo -e "${GREEN}Creating event source mapping (SQS -> Worker)...${NC}"
aws lambda create-event-source-mapping \
    --function-name OrderWorkerProcessor \
    --event-source-arn arn:aws:sqs:${REGION}:${ACCOUNT}:OrderQueue \
    --batch-size 1 \
    --endpoint-url=$ENDPOINT > /dev/null

# Notification Lambda
echo -e "${GREEN}Deploying Notification Service...${NC}"
aws lambda create-function \
    --function-name NotificationService \
    --runtime nodejs18.x \
    --zip-file fileb://notification.zip \
    --handler notification.handler \
    --role arn:aws:iam::${ACCOUNT}:role/lambda-role \
    --endpoint-url=$ENDPOINT > /dev/null

# Gatilho: NotificationQueue -> NotificationLambda
echo -e "${GREEN}Connecting Notification Trigger...${NC}"
aws lambda create-event-source-mapping \
    --function-name NotificationService \
    --event-source-arn arn:aws:sqs:${REGION}:${ACCOUNT}:NotificationQueue \
    --batch-size 1 \
    --endpoint-url=$ENDPOINT > /dev/null

echo -e "${GREEN}Deployment completed successfully!${NC}"