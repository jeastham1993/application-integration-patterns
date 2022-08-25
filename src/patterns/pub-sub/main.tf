module "eventbridge" {
  source   = "terraform-aws-modules/eventbridge/aws"
  bus_name = var.event_bus_name
  tags = {
    Name = var.event_bus_name
  }
}

resource "aws_sns_topic" "pub-sub-sample" {
  name = var.topic_name
}

# Create S3 bucket to store our application source code.
resource "aws_s3_bucket" "lambda_bucket" {
  bucket = var.code_bucket_name

  acl           = "private"
  force_destroy = true
}

# Initialize module containing IAM policies.
module "iam_policies" {
  source         = "./modules/iam-policies"
  event_bus_name = var.event_bus_name
  topic_name     = var.topic_name
}

module "api_gateway" {
  source            = "./modules/api-gateway"
  api_name          = "pub-sub-api"
  stage_name        = "dev"
  stage_auto_deploy = true
}

###############################################################################
##                                Event Bridge    
###############################################################################

module "event_bridge_publisher_lambda" {
  source           = "./modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/functions/EventBridgePublisher/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "EventBridgePublisher.zip"
  function_name    = "EventBridgePublisher"
  lambda_handler   = "EventBridgePublisher::EventBridgePublisher.Function::FunctionHandler"
  environment_variables = {
    "EVENT_BUS_NAME"          = module.eventbridge.eventbridge_bus_name
    "ENVIRONMENT"             = "dev"
    "POWERTOOLS_SERVICE_NAME" = "com.products.service"
  }
}

module "event_subscriber_api_invoke" {
  source        = "./modules/api-gateway-lambda-integration"
  api_id        = module.api_gateway.api_id
  api_arn       = module.api_gateway.api_arn
  function_arn  = module.event_bridge_publisher_lambda.function_arn
  function_name = module.event_bridge_publisher_lambda.function_name
  http_method   = "POST"
  route         = "/event-bridge"
}

resource "aws_iam_role_policy_attachment" "event_publisher_function_put_events" {
  role       = module.event_bridge_publisher_lambda.function_role_name
  policy_arn = module.iam_policies.event_bridge_put_events.arn
}

module "event_bridge_subscriber_lambda" {
  source           = "./modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/functions/EventBridgeSubscriber/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "EventBridgeSubscriber.zip"
  function_name    = "EventBridgeSubscriber"
  lambda_handler   = "EventBridgeSubscriber::EventBridgeSubscriber.Function::FunctionHandler"
  environment_variables = {
    "EVENT_BUS_NAME"          = module.eventbridge.eventbridge_bus_name
    "ENVIRONMENT"             = "dev"
    "POWERTOOLS_SERVICE_NAME" = "com.products.service"
  }
}

module "event_bridge_subscriver_event_target" {
  source                 = "./modules/event-bridge-lambda-target"
  event_bridge_name      = module.eventbridge.eventbridge_bus_name
  event_bridge_rule_name = "product_created"
  lambda_function_name   = module.event_bridge_subscriber_lambda.function_name
  lambda_function_arn    = module.event_bridge_subscriber_lambda.function_arn
  event_pattern          = <<EOF
{
  "detail-type": [
    "product-created"
  ]
}
EOF
}


###############################################################################
##                                  SNS
###############################################################################

module "sns_publisher_lambda" {
  source           = "./modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/functions/SNSPublisher/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "SNSPublisher.zip"
  function_name    = "SNSPublisher"
  lambda_handler   = "SNSPublisher::SNSPublisher.Function::FunctionHandler"
  environment_variables = {
    "PRODUCT_CREATED_TOPIC_ARN" = aws_sns_topic.pub-sub-sample.arn
    "ENVIRONMENT"               = "dev"
    "POWERTOOLS_SERVICE_NAME"   = "com.products.service"
  }
}

module "sns_publisher_api_invoke" {
  source        = "./modules/api-gateway-lambda-integration"
  api_id        = module.api_gateway.api_id
  api_arn       = module.api_gateway.api_arn
  function_arn  = module.sns_publisher_lambda.function_arn
  function_name = module.sns_publisher_lambda.function_name
  http_method   = "POST"
  route         = "/sns"
}

resource "aws_iam_role_policy_attachment" "sns_publisher_function_sns_publish" {
  role       = module.sns_publisher_lambda.function_role_name
  policy_arn = module.iam_policies.sns_publish.arn
}

module "sns_subscriber_lambda" {
  source           = "./modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/functions/SNSSubscriber/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "SNSSubscriber.zip"
  function_name    = "SNSSubscriber"
  lambda_handler   = "SNSSubscriber::SNSSubscriber.Function::FunctionHandler"
  environment_variables = {
    "ENVIRONMENT"             = "dev"
    "POWERTOOLS_SERVICE_NAME" = "com.products.service"
  }
}

resource "aws_lambda_permission" "sns" {
  action        = "lambda:InvokeFunction"
  function_name = module.sns_subscriber_lambda.function_name
  principal     = "sns.amazonaws.com"
  statement_id  = "AllowSubscriptionToSNS"
  source_arn    = aws_sns_topic.pub-sub-sample.arn
}

resource "aws_sns_topic_subscription" "subscription" {
  endpoint  = module.sns_subscriber_lambda.function_arn
  protocol  = "lambda"
  topic_arn = aws_sns_topic.pub-sub-sample.arn
}