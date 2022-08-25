module "eventbridge" {
  source = "terraform-aws-modules/eventbridge/aws"
  bus_name = var.event_bus_name
  tags = {
    Name = var.event_bus_name
  }
}

resource "aws_dynamodb_table" "vendor_response_store" {
  name           = var.table_name
  billing_mode   = "PAY_PER_REQUEST"
  hash_key       = "PK"
  range_key      = "SK"

  attribute {
    name = "PK"
    type = "S"
  }

  attribute {
    name = "SK"
    type = "S"
  }
}

resource "aws_s3_bucket" "lambda_bucket" {
  bucket = "jamesuk-scatter-gather-deploy"

  acl           = "private"
  force_destroy = true
}

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

resource "aws_iam_policy" "event_bridge_put_events" {
  name   = "event_bridge_put_events_policy"
  path   = "/"
  policy = data.aws_iam_policy_document.eventbridge_put_events.json
}

module "vendor-loan-request-lambda_big_bank" {
  source = "./modules/vendor-loan-request-lambda"
  vendor_name = "Big Bank Ltd"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  event_bridge_arn = module.eventbridge.eventbridge_bus_arn
  event_bridge_name = module.eventbridge.eventbridge_bus_name
  event_bridge_put_events_policy_arn = aws_iam_policy.event_bridge_put_events.arn
}

module "vendor-loan-request-lambda_evil_bank" {
  source = "./modules/vendor-loan-request-lambda"
  vendor_name = "Evil Bank Corp"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  event_bridge_arn = module.eventbridge.eventbridge_bus_arn
  event_bridge_name = module.eventbridge.eventbridge_bus_name
  event_bridge_put_events_policy_arn = aws_iam_policy.event_bridge_put_events.arn
}

module "vendor-loan-request-lambda_friendly_bank" {
  source = "./modules/vendor-loan-request-lambda"
  vendor_name = "Friendly Bank Ltd"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  event_bridge_arn = module.eventbridge.eventbridge_bus_arn
  event_bridge_name = module.eventbridge.eventbridge_bus_name
  event_bridge_put_events_policy_arn = aws_iam_policy.event_bridge_put_events.arn
}

# Vendor Loan Quote Response Handler
module "vendor_loan_response_handler_lambda" {
  source = "./modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir = "${path.module}/functions/VendorLoanQuoteResponseHandler/bin/Release/net6.0/linux-x64/publish"
  zip_file = "VendorLoanQuoteResponseHandler.zip"
  function_name = "VendorLoanQuoteResponseHandler"
  lambda_handler = "VendorLoanQuoteResponseHandler::VendorLoanQuoteResponseHandler.Function::FunctionHandler"
  environment_variables = {
    "TABLE_NAME" = aws_dynamodb_table.vendor_response_store.name
  }
}

resource "aws_iam_role_policy_attachment" "vendor_loan_response_dynamo_write" {
  role       = module.vendor_loan_response_handler_lambda.function_role_name
  policy_arn = aws_iam_policy.dynamo_db_write.arn
}

module "loan_quote_completed_event_target" {
  source = "./modules/event-bridge-lambda-target"
  event_bridge_name = module.eventbridge.eventbridge_bus_name
  event_bridge_rule_name = "vendor_quote_received"
  lambda_function_name = module.vendor_loan_response_handler_lambda.function_name
  lambda_function_arn = module.vendor_loan_response_handler_lambda.function_arn
  event_pattern = <<EOF
{
  "detail-type": [
    "com.vendors.completed-loan-request"
  ]
}
EOF
}

# Aggregator
module "aggregator_function" {
  source = "./modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir = "${path.module}/functions/Aggregator/bin/Release/net6.0/linux-x64/publish"
  zip_file = "Aggregator.zip"
  function_name = "Aggregator"
  lambda_handler = "Aggregator::Aggregator.Function::FunctionHandler"
  environment_variables = {
    "TABLE_NAME" = aws_dynamodb_table.vendor_response_store.name
  }
}

resource "aws_iam_role_policy_attachment" "aggregator_dynamo_db_read" {
  role       = module.aggregator_function.function_role_name
  policy_arn = aws_iam_policy.dynamo_db_read.arn
}

resource "aws_lambda_permission" "allow_stepfunctions_to_call_aggregator" {
    statement_id = "AllowExecutionFromStepFunctions"
    action = "lambda:InvokeFunction"
    function_name = module.aggregator_function.function_name
    principal = "states.amazonaws.com"
    source_arn = module.step_function.state_machine_arn
}

module "step_function" {
  source = "terraform-aws-modules/step-functions/aws"

  name       = "scatter-gather-orchestrator"
  definition = replace(file("statemachine/scatter-gather-orchestration.asl.json"), "{{AGGREGATOR_FUNCTION_ARN}}", module.aggregator_function.function_arn)

  service_integrations = {
    eventbridge = {
        eventbridge = [module.eventbridge.eventbridge_bus_arn]
    },
    lambda = {
      lambda = [module.aggregator_function.function_arn]
    }
  }

  type = "STANDARD"
}

resource "aws_iam_policy" "policy_invoke_lambda" {
  name        = "StepFunctionLambdaFunctionInvocationPolicy"

  policy = <<EOF
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "VisualEditor0",
            "Effect": "Allow",
            "Action": [
                "lambda:InvokeFunction",
                "lambda:InvokeAsync"
            ],
            "Resource": "*"
        }
    ]
}
EOF
}

// Attach policy to IAM Role for Step Function
resource "aws_iam_role_policy_attachment" "iam_for_sfn_attach_policy_invoke_lambda" {
  role       = "${module.step_function.role_name}"
  policy_arn = "${aws_iam_policy.policy_invoke_lambda.arn}"
}
