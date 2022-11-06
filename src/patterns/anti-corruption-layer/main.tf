module "eventbridge" {
  source   = "terraform-aws-modules/eventbridge/aws"
  bus_name = var.event_bus_name
  tags = {
    Name = var.event_bus_name
  }
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
  topic_name     = "dev-membership-customer-created-event-received-topic"
}

module "api_gateway" {
  source            = "./modules/api-gateway"
  api_name          = "acl-customer-api"
  stage_name        = "dev"
  stage_auto_deploy = true
}

###############################################################################
##                                Customer Service    
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

###############################################################################
##                                Membership Service    
###############################################################################

resource "aws_sns_topic" "membership_sns_topic" {
  name = "dev-membership-customer-created-event-received-topic"
}

resource "aws_sqs_queue" "customer_created_event_received_queue" {
  name                      = "dev-membership-customer-created-event-recevied"
  tags = {
    Environment = "dev"
  }
}

resource "aws_cloudwatch_event_rule" "customer_created_event_rule" {
  event_bus_name = var.event_bus_name
  name = "dev-membership-customer-created-event"
  event_pattern          = <<EOF
{
  "detail-type": [
    "customer-created"
  ]
}
EOF
}

resource "aws_sqs_queue_policy" "customer_created_event_received_queue_policy" {
  queue_url = aws_sqs_queue.customer_created_event_received_queue.id
  policy    = <<POLICY
{
  "Version": "2012-10-17",
  "Id": "sqspolicy",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Service": "events.amazonaws.com"
      },
      "Action": "sqs:SendMessage",
      "Resource": "${aws_sqs_queue.customer_created_event_received_queue.arn}",
      "Condition": {
        "ArnEquals": {
          "aws:SourceArn": "${aws_cloudwatch_event_rule.customer_created_event_rule.arn}"
        }
      }
    }
  ]
}
POLICY
}

# Set the SQS as a target to the EventBridge Rule
resource "aws_cloudwatch_event_target" "customer_created_sqs_target" {
  event_bus_name = var.event_bus_name
  rule = aws_cloudwatch_event_rule.customer_created_event_rule.name
  arn  = aws_sqs_queue.customer_created_event_received_queue.arn
}

module "membership_customer_created_event_adapter" {
  source           = "./modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/functions/MembershipCustomerCreatedAdapter/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "MembershipCustomerCreatedAdapter.zip"
  function_name    = "MembershipCustomerCreatedAdapter"
  lambda_handler   = "MembershipCustomerCreatedAdapter::MembershipCustomerCreatedAdapter.Function::FunctionHandler"
  environment_variables = {
    "ENVIRONMENT"             = "dev"
    "POWERTOOLS_SERVICE_NAME" = "com.membership.service"
    "TOPIC_ARN"               = aws_sns_topic.membership_sns_topic.arn
  }
}

resource "aws_lambda_event_source_mapping" "customer_created_queue_event_source" {
  event_source_arn = aws_sqs_queue.customer_created_event_received_queue.arn
  function_name    = module.membership_customer_created_event_adapter.function_arn
}

resource "aws_iam_policy" "allow_sqs_permissions" {
  name        = "AllowSqsPermissions"

  policy = <<EOF
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "AllowSQSFromLambda",
            "Effect": "Allow",
            "Action": [
              "sqs:ReceiveMessage",
              "sqs:DeleteMessage",
              "sqs:GetQueueAttributes"
            ],
            "Resource": "*"
        }
    ]
}
EOF
}

resource "aws_iam_role_policy_attachment" "allow_sqs_from_lambda" {
  role       = module.membership_customer_created_event_adapter.function_role_name
  policy_arn = aws_iam_policy.allow_sqs_permissions.arn
}

resource "aws_iam_role_policy_attachment" "sns_publisher_function_sns_publish" {
  role       = module.membership_customer_created_event_adapter.function_role_name
  policy_arn = module.iam_policies.sns_publish.arn
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

resource "aws_lambda_permission" "assign_points_allow_sns" {
  action        = "lambda:InvokeFunction"
  function_name = module.membership_assign_points_lambda.function_name
  principal     = "sns.amazonaws.com"
  statement_id  = "AllowSubscriptionToSNS"
  source_arn    = aws_sns_topic.membership_sns_topic.arn
}

resource "aws_sns_topic_subscription" "assign_points_subscription" {
  endpoint  = module.membership_assign_points_lambda.function_arn
  protocol  = "lambda"
  topic_arn = aws_sns_topic.membership_sns_topic.arn
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

resource "aws_lambda_permission" "update_analytics_allow_sns" {
  action        = "lambda:InvokeFunction"
  function_name = module.membership_update_analytics_lambda.function_name
  principal     = "sns.amazonaws.com"
  statement_id  = "AllowSubscriptionToSNS"
  source_arn    = aws_sns_topic.membership_sns_topic.arn
}

resource "aws_sns_topic_subscription" "update_analytics_subscription" {
  endpoint  = module.membership_update_analytics_lambda.function_arn
  protocol  = "lambda"
  topic_arn = aws_sns_topic.membership_sns_topic.arn
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

resource "aws_lambda_permission" "send_welcome_email_allow_sns" {
  action        = "lambda:InvokeFunction"
  function_name = module.membership_send_welcome_email_lambda.function_name
  principal     = "sns.amazonaws.com"
  statement_id  = "AllowSubscriptionToSNS"
  source_arn    = aws_sns_topic.membership_sns_topic.arn
}

resource "aws_sns_topic_subscription" "welcome_email_subscription" {
  endpoint  = module.membership_send_welcome_email_lambda.function_arn
  protocol  = "lambda"
  topic_arn = aws_sns_topic.membership_sns_topic.arn
}