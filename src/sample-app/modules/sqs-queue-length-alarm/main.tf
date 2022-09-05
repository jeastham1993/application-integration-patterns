resource "aws_cloudwatch_metric_alarm" "sqs_queue_length_alarm" {
    alarm_name = var.alarm_name
    comparison_operator = "GreaterThanOrEqualToThreshold"
    evaluation_periods = "1"
    metric_name = "ApproximateNumberOfMessagesVisible"
    namespace = "AWS/SQS"
    period = "60"
    statistic = "Average"
    threshold = 1
    treat_missing_data = "notBreaching"
    dimensions = {
      "QueueName" = var.queue
    }
}