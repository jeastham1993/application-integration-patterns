# Create a set of IAM policies our application will need
resource "aws_iam_policy" "dynamo_db_read" {
  name   = "dynamo_db_read_policy"
  path   = "/"
  policy = data.aws_iam_policy_document.dynamo_db_read.json
}

resource "aws_iam_policy" "dynamo_db_write" {
  name   = "dynamo_db_write_policy"
  path   = "/"
  policy = data.aws_iam_policy_document.dynamo_db_write.json
}

resource "aws_iam_policy" "cloud_watch_put_metrics" {
  name   = "cloud_watch_put_metrics_policy"
  path   = "/"
  policy = data.aws_iam_policy_document.cloud_watch_put_metrics.json
}

resource "aws_iam_policy" "dynamo_db_stream_read_policy" {
  name   = "dynamo_db_stream_read_policy"
  path   = "/"
  policy = data.aws_iam_policy_document.allow_dynamo_db_streams.json
}

resource "aws_iam_policy" "sns_publish_message" {
  name   = "sns_publish_message_policy"
  path   = "/"
  policy = data.aws_iam_policy_document.sns_publish_policy.json
}

resource "aws_iam_policy" "event_bridge_put_events" {
  name   = "event_bridge_put_events_policy"
  path   = "/"
  policy = data.aws_iam_policy_document.event_bridge_put_events.json
}

resource "aws_iam_policy" "ssm_parameter_read" {
  name   = "ssm_parameter_read_policy"
  path   = "/"
  policy = data.aws_iam_policy_document.ssm_parameter_read.json
}