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
    actions   = ["dynamodb:PutItem",
              "dynamodb:UpdateItem",
              "dynamodb:BatchWriteItem"]
    resources = ["arn:aws:dynamodb:*:${data.aws_caller_identity.current.account_id}:table/${var.table_name}", "arn:aws:dynamodb:*:${data.aws_caller_identity.current.account_id}:table/${var.table_name}/*"]
  }
}

data "aws_iam_policy_document" "eventbridge_put_events" {
  statement {
    actions   = ["events:PutEvents"]
    resources = ["arn:aws:events:*:${data.aws_caller_identity.current.account_id}:event-bus/${var.event_bus_name}"]
  }
}
