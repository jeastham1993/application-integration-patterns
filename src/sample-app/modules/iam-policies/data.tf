data "aws_caller_identity" "current" {}
data "aws_region" "current" {}

data "aws_iam_policy_document" "dynamo_db_read" {
  statement {
    actions   = ["dynamodb:GetItem", "dynamodb:Scan", "dynamodb:Query", "dynamodb:BatchGetItem", "dynamodb:DescribeTable"]
    resources = ["arn:aws:dynamodb:*:${data.aws_caller_identity.current.account_id}:table/${var.table_name}", "arn:aws:dynamodb:*:*:table/${var.table_name}/*"]
  }
}

data "aws_iam_policy_document" "dynamo_db_write" {
  statement {
    actions = ["dynamodb:PutItem",
      "dynamodb:UpdateItem",
    "dynamodb:BatchWriteItem"]
    resources = ["arn:aws:dynamodb:*:${data.aws_caller_identity.current.account_id}:table/${var.table_name}", "arn:aws:dynamodb:*:${data.aws_caller_identity.current.account_id}:table/${var.table_name}/*"]
  }
}

data "aws_iam_policy_document" "cloud_watch_put_metrics" {
  statement {
    actions   = ["cloudwatch:PutMetricData"]
    resources = ["*"]
  }
}

data "aws_iam_policy_document" "allow_dynamo_db_streams" {
  statement {
    actions = ["dynamodb:GetShardIterator",
	"dynamodb:DescribeStream",
	"dynamodb:GetRecords",
	"dynamodb:ListStreams",
    "dynamodb:ListStreams"]
    resources = [
		"arn:aws:dynamodb:*:${data.aws_caller_identity.current.account_id}:table/${var.table_name}",
		"arn:aws:dynamodb:*:${data.aws_caller_identity.current.account_id}:table/${var.table_name}/*"
	]
  }
}

data "aws_iam_policy_document" "sns_publish_policy" {
  statement {
    actions = ["sns:publish"]
    resources = ["arn:aws:sns:*:${data.aws_caller_identity.current.account_id}:${var.topic_name}"]
  }
}
