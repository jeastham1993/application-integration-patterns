resource "aws_sqs_queue" "main_queue" {
  name = var.queue_name
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.dead_letter_queue.arn
    maxReceiveCount     = var.max_receive_count
  })
}

resource "aws_sqs_queue" "dead_letter_queue" {
  name = "${var.queue_name}-dlq"
}

resource "aws_cloudwatch_metric_alarm" "dlq_alarm" {
  alarm_name          = "${var.queue_name}-alarm"
  comparison_operator = "GreaterThanOrEqualToThreshold"
  evaluation_periods  = "1"
  metric_name         = "ApproximateNumberOfMessagesVisible"
  namespace           = "AWS/SQS"
  period              = "60"
  statistic           = "Average"
  threshold           = 1
  treat_missing_data  = "notBreaching"
  dimensions = {
    "QueueName" = aws_sqs_queue.dead_letter_queue.name
  }
}
