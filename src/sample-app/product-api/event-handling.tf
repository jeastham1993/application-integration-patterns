resource "aws_sns_topic" "product_created_topic" {
  name = "${var.environment}-product-created"
}

# DynamoDB Steam Handler Lambda
module "dynamo_db_stream_handler" {
  source           = "../modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/application/DynamoDbStreamHandler/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "DynamoDbStreamHandler.zip"
  function_name    = "DynamoDbStreamHandler"
  lambda_handler   = "DynamoDbStreamHandler::DynamoDbStreamHandler.Function::FunctionHandler"
  environment_variables = {
    "PRODUCT_CREATED_TOPIC_ARN"    = aws_sns_topic.product_created_topic.arn
    "POWERTOOLS_SERVICE_NAME"      = "product-api"
    "POWERTOOLS_METRICS_NAMESPACE" = "product-api"
  }
}

resource "aws_iam_role_policy_attachment" "dynamo_db_stream_handler_allow_stream_read" {
  role       = module.dynamo_db_stream_handler.function_role_name
  policy_arn = module.iam_policies.dynamo_db_read_stream
}

resource "aws_iam_role_policy_attachment" "dynamo_db_stream_handler_dynamo_db_read" {
  role       = module.dynamo_db_stream_handler.function_role_name
  policy_arn = module.iam_policies.dynamo_db_read
}

resource "aws_iam_role_policy_attachment" "dynamo_db_stream_handler_sns_publish_message" {
  role       = module.dynamo_db_stream_handler.function_role_name
  policy_arn = module.iam_policies.sns_publish_message
}

resource "aws_iam_role_policy_attachment" "dynamo_db_stream_handler_cw_metrics" {
  role       = module.dynamo_db_stream_handler.function_role_name
  policy_arn = module.iam_policies.cloud_watch_put_metrics
}

resource "aws_lambda_event_source_mapping" "example" {
  event_source_arn  = aws_dynamodb_table.synchornous_api_table.stream_arn
  function_name     = module.dynamo_db_stream_handler.function_arn
  starting_position = "LATEST"
}

# Update Product Catalogue
module "product_catalogue_updates_queue" {
  source            = "../modules/sqs-with-dlq"
  queue_name        = "${var.environment}-update-product-catalogue-queue"
  max_receive_count = 2
}

resource "aws_sns_topic_subscription" "product_catalogue_queue_target" {
  topic_arn = aws_sns_topic.product_created_topic.arn
  protocol  = "sqs"
  endpoint  = module.product_catalogue_updates_queue.queue_arn
}

resource "aws_sqs_queue_policy" "product_catalogue_queue_policy" {
  queue_url = module.product_catalogue_updates_queue.queue_id

  policy = <<POLICY
{
  "Version": "2012-10-17",
  "Id": "sqspolicy",
  "Statement": [
    {
      "Sid": "First",
      "Effect": "Allow",
      "Principal": "*",
      "Action": "sqs:SendMessage",
      "Resource": "${module.product_catalogue_updates_queue.queue_arn}",
      "Condition": {
        "ArnEquals": {
          "aws:SourceArn": "${aws_sns_topic.product_created_topic.arn}"
        }
      }
    }
  ]
}
POLICY
}

module "update_product_catalogue_lambda" {
  source           = "../modules/sqs-sourced-lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/application/UpdateProductCatalogue/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "UpdateProductCatalogue.zip"
  function_name    = "UpdateProductCatalogue"
  lambda_handler   = "UpdateProductCatalogue::UpdateProductCatalogue.Function::FunctionHandler"
  queue_arn        = module.product_catalogue_updates_queue.queue_arn
  queue_name       = module.product_catalogue_updates_queue.queue_name
  environment_variables = {
    "PRODUCT_TABLE_NAME"           = aws_dynamodb_table.synchornous_api_table.name
    "POWERTOOLS_SERVICE_NAME"      = "product-api"
    "POWERTOOLS_METRICS_NAMESPACE" = "product-api"
  }
}

resource "aws_iam_role_policy_attachment" "update_product_catalogue_lambda_dynamo_db_read" {
  role       = module.update_product_catalogue_lambda.function_role_name
  policy_arn = module.iam_policies.dynamo_db_read
}

resource "aws_iam_role_policy_attachment" "update_product_catalogue_lambda_cw_metrics" {
  role       = module.update_product_catalogue_lambda.function_role_name
  policy_arn = module.iam_policies.cloud_watch_put_metrics
}

# External Event Publisher
module "external_event_publishing_queue" {
  source            = "../modules/sqs-with-dlq"
  queue_name        = "${var.environment}-external-event-publishing-queue"
  max_receive_count = 2
}

resource "aws_sns_topic_subscription" "external_event_publisher_queue_target" {
  topic_arn = aws_sns_topic.product_created_topic.arn
  protocol  = "sqs"
  endpoint  = module.external_event_publishing_queue.queue_arn
}

resource "aws_sqs_queue_policy" "external_events_queue_policy" {
  queue_url = module.external_event_publishing_queue.queue_id

  policy = <<POLICY
{
  "Version": "2012-10-17",
  "Id": "sqspolicy",
  "Statement": [
    {
      "Sid": "First",
      "Effect": "Allow",
      "Principal": "*",
      "Action": "sqs:SendMessage",
      "Resource": "${module.external_event_publishing_queue.queue_arn}",
      "Condition": {
        "ArnEquals": {
          "aws:SourceArn": "${aws_sns_topic.product_created_topic.arn}"
        }
      }
    }
  ]
}
POLICY
}

module "external_event_publishing_lambda" {
  source           = "../modules/sqs-sourced-lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/application/ExternalEventPublisher/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "ExternalEventPublisher.zip"
  function_name    = "ExternalEventPublisher"
  lambda_handler   = "ExternalEventPublisher::ExternalEventPublisher.Function::FunctionHandler"
  queue_arn        = module.external_event_publishing_queue.queue_arn
  queue_name       = module.external_event_publishing_queue.queue_name
  environment_variables = {
    "POWERTOOLS_SERVICE_NAME"      = "product-api"
    "POWERTOOLS_METRICS_NAMESPACE" = "product-api"
    "EVENT_BUS_PARAMETER"          = "/${var.environment}/shared/event_bus_name"
  }
}

resource "aws_iam_role_policy_attachment" "external_event_publishing_lambda_cw_metrics" {
  role       = module.external_event_publishing_lambda.function_role_name
  policy_arn = module.iam_policies.cloud_watch_put_metrics
}

resource "aws_iam_role_policy_attachment" "external_event_publishing_lambda_put_events" {
  role       = module.external_event_publishing_lambda.function_role_name
  policy_arn = module.iam_policies.event_bridge_put_events
}

resource "aws_iam_role_policy_attachment" "external_event_publishing_lambda_ssm_read" {
  role       = module.external_event_publishing_lambda.function_role_name
  policy_arn = module.iam_policies.ssm_parameter_read
}
