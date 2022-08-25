resource "aws_sqs_queue" "customer_service_product_notifications" {
  name                      = "${var.environment}-customer-service-product-notification-queue"
}

resource "aws_sns_topic_subscription" "user_updates_sqs_target" {
  topic_arn = var.product_created_topic_arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.customer_service_product_notifications.arn
}

resource "aws_sqs_queue_policy" "results_updates_queue_policy" {
    queue_url = "${aws_sqs_queue.customer_service_product_notifications.id}"

    policy = <<POLICY
{
  "Version": "2012-10-17",
  "Id": "sqspolicy",
  "Statement": [
    {
      "Sid": "First",
      "Effect": "Allow",
      "Principal": "*",
      "Action": "sqs:SendMessage",
      "Resource": "${aws_sqs_queue.customer_service_product_notifications.arn}",
      "Condition": {
        "ArnEquals": {
          "aws:SourceArn": "${var.product_created_topic_arn}"
        }
      }
    }
  ]
}
POLICY
}