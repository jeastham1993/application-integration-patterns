output "event_bridge_put_events" {
  value       =  aws_iam_policy.event_bridge_put_events
}

output "sns_publish" {
  value       =  aws_iam_policy.sns_publish
}

output "cloud_watch_put_metrics" {
  value       =  aws_iam_policy.cloud_watch_put_metrics.arn
}