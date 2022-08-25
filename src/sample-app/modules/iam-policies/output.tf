output "dynamo_db_read" {
  value       =  aws_iam_policy.dynamo_db_read.arn
}

output "dynamo_db_write" {
  value       =  aws_iam_policy.dynamo_db_write.arn
}

output "dynamo_db_read_stream" {
  value       =  aws_iam_policy.dynamo_db_stream_read_policy.arn
}

output "cloud_watch_put_metrics" {
  value       =  aws_iam_policy.cloud_watch_put_metrics.arn
}

output "sns_publish_message" {
  value       =  aws_iam_policy.sns_publish_message.arn
}