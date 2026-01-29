exports.handler = async (event) => {
  console.log("Enviando ordem para a Bolsa...");

  // Simulate buy order logic
  if (event.quantity === 13) {
    throw new Error("B3 unavailable: Falha na comunicação com a Bolsa");
  }

  return { ...event, status: "COMPLETED", trade_id: "B3-" + Date.now() };
}