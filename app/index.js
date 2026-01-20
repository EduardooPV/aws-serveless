const { SQSClient, SendMessageCommand } = require('@aws-sdk/client-sqs');

// Configure AWS SDK for LocalStack or local environment
const isLocalStack = process.env.LOCALSTACK_HOSTNAME ? true : false;

const config = {
  region: "us-east-1",
  endpoint: isLocalStack
    ? `http://${process.env.LOCALSTACK_HOSTNAME}:4566`
    : "http://localhost:4566"
};

const sqsClient = new SQSClient(config);

exports.handler = async (event) => {
  console.log("[API Lambda] Order request received");

  // Fix queue URL for LocalStack internal network
  let queueUrl = process.env.QUEUE_URL;

  if (isLocalStack && queueUrl) {
    queueUrl = queueUrl.replace(/localhost|sqs\.[a-z0-9-]+\.localhost\.localstack\.cloud/, process.env.LOCALSTACK_HOSTNAME);
    console.log(`[API Lambda] Adjusted queue URL for internal network: ${queueUrl}`);
  }

  // Generate order with unique ID
  const orderId = "ORD-LAMBDA-" + Date.now();

  const newOrder = {
    order_id: orderId,
    customer: "Maria Lambda",
    stock_symbol: "VALE3",
    quantity: 50,
    price: 60.00,
    timestamp: new Date().toISOString()
  };

  try {
    // Send order to SQS queue for processing
    const command = new SendMessageCommand({
      QueueUrl: queueUrl,
      MessageBody: JSON.stringify(newOrder)
    });

    await sqsClient.send(command);
    console.log(`[API Lambda] Order ${orderId} successfully queued`);

    return {
      statusCode: 200,
      body: JSON.stringify({
        message: "Order received successfully. Processing in progress.",
        order_id: orderId
      })
    };

  } catch (error) {
    console.error("[API Lambda] Error:", error);
    return { statusCode: 500, body: error.message };
  }
};