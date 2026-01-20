const { S3Client, PutObjectCommand } = require("@aws-sdk/client-s3");
const { DynamoDBClient } = require("@aws-sdk/client-dynamodb");
const { DynamoDBDocumentClient, PutCommand } = require("@aws-sdk/lib-dynamodb");

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

exports.handler = async (event) => {
  // Process each message from SQS queue
  for (const record of event.Records) {
    try {
      const orderData = JSON.parse(record.body);

      console.log("[Worker Lambda] Processing order");
      console.log(`[Worker Lambda] Order ID: ${orderData.order_id}`);

      // Save order to DynamoDB
      const dynamoParams = {
        TableName: "Orders",
        Item: {
          ...orderData,
          status: "COMPLETED",
          processed_at: new Date().toISOString()
        }
      };

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

      console.log("[Worker Lambda] Order processing completed successfully");

    } catch (error) {
      console.error("[Worker Lambda] Critical error processing message:", error);
      throw error;
    }
  }
};