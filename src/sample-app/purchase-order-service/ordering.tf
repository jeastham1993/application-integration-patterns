# Contains logic for the purchase ordering system
module "purchase_ordering_queue" {
  source            = "../modules/sqs-with-dlq"
  queue_name        = "${var.environment}-purchase-order-queue"
  max_receive_count = 2
}

# Create a new EventBridge Rule
resource "aws_cloudwatch_event_rule" "product_created_event_rule" {
  event_bus_name = "${var.environment}-application-integration-patterns-samples"
  event_pattern = <<PATTERN
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
  arn  = module.purchase_ordering_queue.queue_arn
}

# Allow the EventBridge to send messages to the SQS queue.
resource "aws_sqs_queue_policy" "test" {
  queue_url = module.purchase_ordering_queue.queue_id
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
      "Resource": "${module.purchase_ordering_queue.queue_arn}",
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
