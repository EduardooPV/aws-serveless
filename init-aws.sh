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

# Create SQS queue for order processing
echo "Creating SQS queue 'OrderQueue'..."
awslocal sqs create-queue --queue-name OrderQueue

echo "LocalStack initialization complete."