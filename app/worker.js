
const { SNSClient } = require("@aws-sdk/client-sns");

// Configure AWS SDK for LocalStack or local environment
const isLocalStack = process.env.LOCALSTACK_HOSTNAME ? true : false;
const config = {
  region: "us-east-1",
  endpoint: isLocalStack
    ? `http://${process.env.LOCALSTACK_HOSTNAME}:4566`
    : "http://localhost:4566"
};

sfnClient = new SNSClient(config);

exports.handler = async (event) => {
  for (const record of event.Records) {
    try {
      const orderData = JSON.parse(record.body);

      // The ARN of the state machine comes from an environment variable
      const stateMachineArn = process.env.STATE_MACHINE_ARN;

      console.log(`🚀 Iniciando Saga para Order ID: ${orderData.order_id}`);

      const command = new StartExecutionCommand({
        stateMachineArn: stateMachineArn,
        input: JSON.stringify(orderData),
        name: `Exec-${orderData.order_id}-${Date.now()}`
      });

      await sfnClient.send(command);
      console.log("✅ Workflow iniciado!");

    } catch (error) {
      console.error("Erro ao iniciar Step Function:", error);
      throw error;
    }
  }
};