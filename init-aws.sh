#!/bin/bash
echo "Initializing LocalStack resources..."

# Create S3 bucket for order receipts
echo "Creating S3 bucket 'order-receipts'..."
awslocal s3 mb s3://order-receipts

# Create DynamoDB table for orders (Primary Key: order_id)
echo "Creating DynamoDB table 'Orders'..."
awslocal dynamodb create-table \
  --table-name Orders \
  --attribute-definitions AttributeName=order_id,AttributeType=S \
  --key-schema AttributeName=order_id,KeyType=HASH \
  --provisioned-throughput ReadCapacityUnits=5,WriteCapacityUnits=5

# 3. Create the Dead Letter Queue (DLQ)
echo "Creating DLQ 'OrderQueue-DLQ'..."
awslocal sqs create-queue --queue-name OrderQueue-DLQ

# Get the DLQ ARN (Amazon Resource Name)
# We need this ID to tell the main queue where to send failed messages
DLQ_ARN=$(awslocal sqs get-queue-attributes \
  --queue-url http://localhost:4566/000000000000/OrderQueue-DLQ \
  --attribute-names QueueArn \
  --output text | cut -f2)
echo "DLQ ARN: $DLQ_ARN"

# 4. Create the Main Queue with Redrive Policy
# maxReceiveCount=3: Try 3 times before moving to DLQ
# visibilityTimeout=30: 30 seconds to process each message
echo "Creating Main Queue 'OrderQueue' linked to DLQ..."
awslocal sqs create-queue --queue-name OrderQueue \
--attributes "{\"RedrivePolicy\": \"{\\\"deadLetterTargetArn\\\":\\\"$DLQ_ARN\\\",\\\"maxReceiveCount\\\":\\\"3\\\"}\", \"VisibilityTimeout\": \"30\"}"

echo "LocalStack initialization complete."