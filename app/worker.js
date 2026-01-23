const { S3Client, PutObjectCommand } = require("@aws-sdk/client-s3");
const { DynamoDBClient } = require("@aws-sdk/client-dynamodb");
const { DynamoDBDocumentClient, PutCommand } = require("@aws-sdk/lib-dynamodb");
const { SQSClient, ChangeMessageVisibilityCommand } = require("@aws-sdk/client-sqs");

// Configure AWS SDK for LocalStack or local environment
const isLocalStack = process.env.LOCALSTACK_HOSTNAME ? true : false;
const config = {
  region: "us-east-1",
  endpoint: isLocalStack
    ? `http://${process.env.LOCALSTACK_HOSTNAME}:4566`
    : "http://localhost:4566"
};

const s3Client = new S3Client(config);
const dynamoClient = new DynamoDBClient(config);
const docClient = DynamoDBDocumentClient.from(dynamoClient);
const sqsClient = new SQSClient(config);

exports.handler = async (event) => {
  // Process each message from SQS queue
  for (const record of event.Records) {
    try {
      const orderData = JSON.parse(record.body);

      console.log("[Worker Lambda] Processing order");
      console.log(`[Worker Lambda] Order ID: ${orderData.order_id}`);

      // Error simulation for testing DLQ
      if (orderData.quantity <= 0) {
        throw new Error("Invalid quantity: Must be positive. Moving to DLQ logic.");
      }

      // Save order to DynamoDB
      const dynamoParams = {
        TableName: "Orders",
        Item: {
          ...orderData,
          status: "COMPLETED",
          processed_at: new Date().toISOString()
        },
        // Ensure we don't overwrite an existing order with the same order_id
        ConditionExpression: 'attribute_not_exists(order_id)'
      };

      try {
        await docClient.send(new PutCommand(dynamoParams));
        console.log(`[Worker Lambda] Order saved to DynamoDB`);

        // Save order receipt to S3
        const s3Params = {
          Bucket: "order-receipts",
          Key: `receipt-${orderData.order_id}.json`,
          Body: JSON.stringify(orderData, null, 2),
          ContentType: "application/json"
        };

        await s3Client.send(new PutObjectCommand(s3Params));
        console.log(`[Worker Lambda] Receipt saved to S3`);
      } catch (dbError) {
        // If error is "ConditionalCheckFailed" then the order already exists
        if (dbError.name === "ConditionalCheckFailedException") {
          console.warn(`[Worker Lambda] IDEMPOTENCY HIT: Order ${orderData.order_id} already processed. Ignoring.`);
          // No return error thrown, just skip to next record
          continue;
        } else {
          // Rethrow other errors to SQS for DLQ processing
          throw dbError;
        }
      }

      console.log("[Worker Lambda] Order processing completed successfully");

    } catch (error) {
      // Log error and rethrow to trigger DLQ if configured
      console.error(`[Worker Lambda] Error processing order ${record.messageId}:`, error.message);

      try {
        // Discover how to many times this message has been received
        const attempts = parseInt(record.attributes.ApproximateReceiveCount || "1");

        // Calculate backoff times (2^attempts)
        const newVisibilityTimeout = Math.pow(2, attempts);

        console.log(`[Worker Lambda] Applying Exponential Backoff. Wait ${newVisibilityTimeout}s before retry.`);

        // 3. Inform SQS to "hide" the message for this time
        // Needs extracting QueueUrl from ARN or using an env var
        // In event Lambda SQS, the URL is not directly available, but we can construct it or use an env var if passed.
        // TRICK: The 'config.endpoint' already points to LocalStack, we can use the standard URL if we know the name,
        // or pass the QUEUE_URL to the worker also in deploy.sh.

        // Lets assume we pass the QUEUE_URL in deploy.sh for the worker too (Best practice!)
        if (process.env.QUEUE_URL) {
          let queueUrl = process.env.QUEUE_URL;

          if (isLocalStack) {
            queueUrl = queueUrl.replace(/localhost|sqs\.[a-z0-9-]+\.localhost\.localstack\.cloud/, process.env.LOCALSTACK_HOSTNAME);
          }

          await sqsClient.send(new ChangeMessageVisibilityCommand({
            QueueUrl: queueUrl,
            ReceiptHandle: record.receiptHandle,
            VisibilityTimeout: newVisibilityTimeout
          }));
        }
      } catch (backoffError) {
        console.error("Failed to apply backoff:", backoffError);
      }

      throw error;
    }
  }
};