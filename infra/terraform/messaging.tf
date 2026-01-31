resource "aws_sns_topic" "order_events" {
  name = "OrderEvents"
}

resource "aws_sqs_queue" "dlq" {
  name = "OrderQueue-DLQ"
}

resource "aws_sqs_queue" "order_queue" {
  name                      = "OrderQueue"
  visibility_timeout_seconds = 30
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.dlq.arn
    maxReceiveCount     = 3
  })
}

resource "aws_sqs_queue" "notification_queue" { name = "NotificationQueue" }
resource "aws_sqs_queue" "audit_queue"        { name = "AuditQueue" }
resource "aws_sqs_queue" "reports_queue"      { name = "ReportsQueue" }

resource "aws_sns_topic_subscription" "sub_notification" {
  topic_arn = aws_sns_topic.order_events.arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.notification_queue.arn
}

resource "aws_sns_topic_subscription" "sub_audit" {
  topic_arn = aws_sns_topic.order_events.arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.audit_queue.arn
}

resource "aws_sns_topic_subscription" "sub_reports" {
  topic_arn = aws_sns_topic.order_events.arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.reports_queue.arn
}