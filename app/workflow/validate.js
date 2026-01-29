exports.handler = async (event) => {
  // The event is now the input coming from the previous step or the start
  console.log("Validando pedido:", event);

  const total = event.quantity * event.price;

  if (total > 10000) {
    throw new Error("Saldo insuficiente.")
  };

  // Return the value to be passed to the next step
  return { ...event, total_cost: total, status: "VALIDATED" }
}