# Contains logic for the purchase ordering system
resource "aws_sqs_queue" "purchase-ordering-queue" {
  name                      = "${var.environment}-purchase-order-queue"
}

resource "aws_sns_topic_subscription" "user_updates_sqs_target" {
  topic_arn = var.product_created_topic_arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.purchase-ordering-queue.arn
}