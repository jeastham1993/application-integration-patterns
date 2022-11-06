module "eventbridge" {
  source   = "terraform-aws-modules/eventbridge/aws"
  bus_name = var.event_bus_name
  tags = {
    Name = var.event_bus_name
  }
}

resource "aws_sns_topic" "membership-sns-topic" {
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
  api_name          = "acl-customer-api"
  stage_name        = "dev"
  stage_auto_deploy = true
}

###############################################################################
##                                Event Bridge    
###############################################################################

module "event_bridge_publisher_lambda" {
  source           = "./modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/functions/NewCustomerPublisher/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "NewCustomerPublisher.zip"
  function_name    = "NewCustomerPublisher"
  lambda_handler   = "NewCustomerPublisher::NewCustomerPublisher.Function::FunctionHandler"
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
  route         = "/customer"
}

resource "aws_iam_role_policy_attachment" "event_publisher_function_put_events" {
  role       = module.event_bridge_publisher_lambda.function_role_name
  policy_arn = module.iam_policies.event_bridge_put_events.arn
}

module "membership_assign_points_lambda" {
  source           = "./modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/functions/MembershipAssignPoints/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "MembershipAssignPoints.zip"
  function_name    = "MembershipAssignPoints"
  lambda_handler   = "MembershipAssignPoints::MembershipAssignPoints.Function::FunctionHandler"
  environment_variables = {
    "EVENT_BUS_NAME"          = module.eventbridge.eventbridge_bus_name
    "ENVIRONMENT"             = "dev"
    "POWERTOOLS_SERVICE_NAME" = "com.membership.service"
  }
}

module "membership_assign_points_event_target" {
  source                 = "./modules/event-bridge-lambda-target"
  event_bridge_name      = module.eventbridge.eventbridge_bus_name
  event_bridge_rule_name = "product_created"
  lambda_function_name   = module.membership_assign_points_lambda.function_name
  lambda_function_arn    = module.membership_assign_points_lambda.function_arn
  event_pattern          = <<EOF
{
  "detail-type": [
    "customer-created"
  ]
}
EOF
}

module "membership_update_analytics_lambda" {
  source           = "./modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/functions/MembershipUpdateAnalytics/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "MembershipUpdateAnalytics.zip"
  function_name    = "MembershipUpdateAnalytics"
  lambda_handler   = "MembershipUpdateAnalytics::MembershipUpdateAnalytics.Function::FunctionHandler"
  environment_variables = {
    "EVENT_BUS_NAME"          = module.eventbridge.eventbridge_bus_name
    "ENVIRONMENT"             = "dev"
    "POWERTOOLS_SERVICE_NAME" = "com.membership.service"
  }
}

module "membership_update_analytics_event_target" {
  source                 = "./modules/event-bridge-lambda-target"
  event_bridge_name      = module.eventbridge.eventbridge_bus_name
  event_bridge_rule_name = "product_created"
  lambda_function_name   = module.membership_update_analytics_lambda.function_name
  lambda_function_arn    = module.membership_update_analytics_lambda.function_arn
  event_pattern          = <<EOF
{
  "detail-type": [
    "customer-created"
  ]
}
EOF
}

module "membership_send_welcome_email_lambda" {
  source           = "./modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/functions/MembershipSendWelcomeEmail/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "MembershipSendWelcomeEmail.zip"
  function_name    = "MembershipSendWelcomeEmail"
  lambda_handler   = "MembershipSendWelcomeEmail::MembershipSendWelcomeEmail.Function::FunctionHandler"
  environment_variables = {
    "EVENT_BUS_NAME"          = module.eventbridge.eventbridge_bus_name
    "ENVIRONMENT"             = "dev"
    "POWERTOOLS_SERVICE_NAME" = "com.membership.service"
  }
}

module "membership_send_welcome_email_event_target" {
  source                 = "./modules/event-bridge-lambda-target"
  event_bridge_name      = module.eventbridge.eventbridge_bus_name
  event_bridge_rule_name = "product_created"
  lambda_function_name   = module.membership_send_welcome_email_lambda.function_name
  lambda_function_arn    = module.membership_send_welcome_email_lambda.function_arn
  event_pattern          = <<EOF
{
  "detail-type": [
    "customer-created"
  ]
}
EOF
}