resource "aws_sns_topic" "product-created-topic" {
  name = "${var.environment}-product-created"
}

# DynamoDB Steam Handler Lambda
module "dynamo_db_stream_handler" {
  source = "../modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir = "${path.module}/application/DynamoDbStreamHandler/bin/Release/net6.0/linux-x64/publish"
  zip_file = "DynamoDbStreamHandler.zip"
  function_name = "DynamoDbStreamHandler"
  lambda_handler = "DynamoDbStreamHandler::DynamoDbStreamHandler.Function::FunctionHandler"
  environment_variables = {
    "PRODUCT_CREATED_TOPIC_ARN" = aws_sns_topic.product-created-topic.arn
    "POWERTOOLS_SERVICE_NAME" = "product-api"
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

resource "aws_iam_role_policy_attachment" "dynamo_db_stream_handler_cw_metrics" {
  role       = module.dynamo_db_stream_handler.function_role_name
  policy_arn = module.iam_policies.cloud_watch_put_metrics
}


# resource "aws_lambda_event_source_mapping" "example" {
#   event_source_arn  = aws_dynamodb_table.synchornous_api_table.stream_arn
#   function_name     = module.dynamo_db_stream_handler.function_arn
#   starting_position = "LATEST"
# }