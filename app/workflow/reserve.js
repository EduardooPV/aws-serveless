exports.handler = async (event) => {
  console.log("Bloqueando saldo no valor de:", event.total_cost);

  // Simulate reservation logic

  return { ...event, status: "RESERVED" }
}