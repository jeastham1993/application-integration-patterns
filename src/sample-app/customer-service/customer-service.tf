resource "aws_sqs_queue" "customer_service_product_notifications" {
  name = "${var.environment}-customer-service-product-notification-queue"
}

# Create a new EventBridge Rule
resource "aws_cloudwatch_event_rule" "product_created_event_rule" {
  event_bus_name = "${var.environment}-application-integration-patterns-samples"
  event_pattern  = <<PATTERN
{
  "source": ["product-api"],
  "detail-type": ["product-created"]
}
PATTERN
}

# Set the SQS as a target to the EventBridge Rule
resource "aws_cloudwatch_event_target" "sqs-target" {
  event_bus_name = "${var.environment}-application-integration-patterns-samples"
  rule = aws_cloudwatch_event_rule.product_created_event_rule.name
  arn  = aws_sqs_queue.customer_service_product_notifications.arn
}

# Allow the EventBridge to send messages to the SQS queue.
resource "aws_sqs_queue_policy" "test" {
  queue_url = aws_sqs_queue.customer_service_product_notifications.id
  policy    = <<POLICY
{
  "Version": "2012-10-17",
  "Id": "sqspolicy",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Service": "events.amazonaws.com"
      },
      "Action": "sqs:SendMessage",
      "Resource": "${aws_sqs_queue.customer_service_product_notifications.arn}",
      "Condition": {
        "ArnEquals": {
          "aws:SourceArn": "${aws_cloudwatch_event_rule.product_created_event_rule.arn}"
        }
      }
    }
  ]
}
POLICY
}
