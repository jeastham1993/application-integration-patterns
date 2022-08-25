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