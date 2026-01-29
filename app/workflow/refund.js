exports.handler = async (event) => {
  // Step function sends the event to this function
  console.log("🚨 ALERTA DE ROLLBACK 🚨");
  console.log("💸 Devolvendo dinheiro ao cliente:", event.total_cost);

  // Simulate refund logic

  return { ...event, status: "REFUNDED", rollback_completed: true }
}