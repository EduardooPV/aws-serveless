exports.handler = async (event) => {
  for (const record of event.Records) {
    try {
      // The body of the SQS message coming from the SNS has a specific format.
      // The original message is in the "Message".
      const snsBody = JSON.parse(record.body);
      const orderData = JSON.parse(snsBody.Message)

      console.log("------------------------------------------------");
      console.log("📧 NOTIFICATION SERVICE");
      console.log(`📩 Enviando e-mail para: ${orderData.customer}`);
      console.log(`🆔 Pedido Confirmado: ${orderData.order_id}`);
      console.log("✅ E-mail enviado com sucesso!");
      console.log("------------------------------------------------");


    } catch (error) {
      // We don't re-throw the error to prevent the message from being retried.
      console.log("❌ Erro ao processar a mensagem:", error);
    }
  }
}