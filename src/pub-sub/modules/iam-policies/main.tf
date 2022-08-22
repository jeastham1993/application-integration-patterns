# Create a set of IAM policies our application will need
resource "aws_iam_policy" "event_bridge_put_events" {
  name   = "event_bridge_put_events"
  path   = "/"
  policy = data.aws_iam_policy_document.event_bridge_put_events.json
}

resource "aws_iam_policy" "sns_publish" {
  name   = "sns_publish"
  path   = "/"
  policy = data.aws_iam_policy_document.sns_publish.json
}

resource "aws_iam_policy" "cloud_watch_put_metrics" {
  name   = "cloud_watch_put_metrics_policy"
  path   = "/"
  policy = data.aws_iam_policy_document.cloud_watch_put_metrics.json
}