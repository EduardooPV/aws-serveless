data "archive_file" "lambda_code" {
  type        = "zip"
  source_dir  = "${path.module}/../app"
  output_path = "${path.module}/bundle.zip"
  excludes    = ["*.zip", "deploy.sh", ".env", "*.tf", ".terraform"]
}

resource "aws_iam_role" "lambda_role" {
  name = "serverless_lambda_role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = { Service = "lambda.amazonaws.com" }
    }]
  })
}

resource "aws_lambda_function" "api" {
  filename         = data.archive_file.lambda_code.output_path
  function_name    = "OrderProcessorAPI"
  role             = aws_iam_role.lambda_role.arn
  handler          = "index.handler"
  source_code_hash = data.archive_file.lambda_code.output_base64sha256
  runtime          = "nodejs18.x"
  timeout          = 10

  environment {
    variables = {
      QUEUE_URL = aws_sqs_queue.order_queue.id
    }
  }
}

resource "aws_lambda_function" "worker" {
  filename         = data.archive_file.lambda_code.output_path
  function_name    = "OrderWorkerProcessor"
  role             = aws_iam_role.lambda_role.arn
  handler          = "worker.handler"
  source_code_hash = data.archive_file.lambda_code.output_base64sha256
  runtime          = "nodejs18.x"
  timeout          = 10

  environment {
    variables = {
      QUEUE_URL = aws_sqs_queue.order_queue.id
      TOPIC_ARN = aws_sns_topic.order_events.arn
    }
  }
}

resource "aws_lambda_function" "notification" {
  filename         = data.archive_file.lambda_code.output_path
  function_name    = "NotificationService"
  role             = aws_iam_role.lambda_role.arn
  handler          = "notification.handler"
  source_code_hash = data.archive_file.lambda_code.output_base64sha256
  runtime          = "nodejs18.x"
  timeout          = 10
}

resource "aws_lambda_event_source_mapping" "worker_trigger" {
  event_source_arn = aws_sqs_queue.order_queue.arn
  function_name    = aws_lambda_function.worker.arn
  batch_size       = 1
}

resource "aws_lambda_event_source_mapping" "notification_trigger" {
  event_source_arn = aws_sqs_queue.notification_queue.arn
  function_name    = aws_lambda_function.notification.arn
  batch_size       = 1
}