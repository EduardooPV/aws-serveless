output "api_lambda_name" {
  value = aws_lambda_function.api.function_name
  description = "Nome da Lambda de API para invocar via AWS CLI"
}

output "order_queue_url" {
  value = aws_sqs_queue.order_queue.id
  description = "URL da fila principal"
}

output "dlq_url" {
  value = aws_sqs_queue.dlq.id
  description = "URL da Dead Letter Queue"
}

output "dynamodb_table_name" {
  value = aws_dynamodb_table.orders.name
}