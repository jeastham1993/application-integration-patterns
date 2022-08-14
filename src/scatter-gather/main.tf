module "eventbridge" {
  source = "terraform-aws-modules/eventbridge/aws"
  bus_name = "application-integration-bus"
  tags = {
    Name = "application-integration-bus"
  }
}

resource "aws_dynamodb_table" "vendor_response_store" {
  name           = "VendorLoanResponseStore"
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

data "archive_file" "lambda_vendor_loan_quote_response_handler" {
  type = "zip"

  source_dir  = "${path.module}/functions/VendorLoanQuoteResponseHandler/bin/Release/net6.0/linux-x64/publish"
  output_path = "${path.module}/VendorLoanQuoteResponseHandler.zip"
}

data "archive_file" "lambda_aggregator" {
  type = "zip"

  source_dir  = "${path.module}/functions/Aggregator/bin/Release/net6.0/linux-x64/publish"
  output_path = "${path.module}/Aggregator.zip"
}

resource "aws_s3_object" "lambda_vendor_loan_quote_response_handler" {
  bucket = aws_s3_bucket.lambda_bucket.id

  key    = "VendorLoanQuoteResponseHandler.zip"
  source = data.archive_file.lambda_vendor_loan_quote_response_handler.output_path

  etag = filemd5(data.archive_file.lambda_vendor_loan_quote_response_handler.output_path)
}

resource "aws_s3_object" "lambda_aggregator" {
  bucket = aws_s3_bucket.lambda_bucket.id

  key    = "Aggregator.zip"
  source = data.archive_file.lambda_aggregator.output_path

  etag = filemd5(data.archive_file.lambda_aggregator.output_path)
}

module "vendor-loan-request-lambda_big_bank" {
  source = "./modules/vendor-loan-request-lambda"
  vendor_name = "Big Bank Ltd"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  event_bridge_arn = module.eventbridge.eventbridge_bus_arn
  event_bridge_name = module.eventbridge.eventbridge_bus_name
}

module "vendor-loan-request-lambda_evil_bank" {
  source = "./modules/vendor-loan-request-lambda"
  vendor_name = "Evil Bank Corp"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  event_bridge_arn = module.eventbridge.eventbridge_bus_arn
  event_bridge_name = module.eventbridge.eventbridge_bus_name
}

module "vendor-loan-request-lambda_friendly_bank" {
  source = "./modules/vendor-loan-request-lambda"
  vendor_name = "Friendly Bank Ltd"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  event_bridge_arn = module.eventbridge.eventbridge_bus_arn
  event_bridge_name = module.eventbridge.eventbridge_bus_name
}

# Vendor Loan Quote Response Handler
resource "aws_lambda_function" "vendor_loan_quote_response_handler" {
  function_name = "VendorLoanQuoteResponseHandler"
  s3_bucket = aws_s3_bucket.lambda_bucket.id
  s3_key    = aws_s3_object.lambda_vendor_loan_quote_response_handler.key
  runtime = "dotnet6"
  handler = "VendorLoanQuoteResponseHandler::VendorLoanQuoteResponseHandler.Function::FunctionHandler"
  source_code_hash = data.archive_file.lambda_vendor_loan_quote_response_handler.output_base64sha256
  role = aws_iam_role.lambda_vendor_response_exec.arn
  timeout = 30
  environment {
    variables = {
      "TABLE_NAME" = aws_dynamodb_table.vendor_response_store.name
    }
  }
}

resource "aws_cloudwatch_log_group" "vendor_loan_quote_response_handler" {
  name = "/aws/lambda/${aws_lambda_function.vendor_loan_quote_response_handler.function_name}"

  retention_in_days = 30
}

resource "aws_iam_role" "lambda_vendor_response_exec" {
  name = "FunctionRole_VendorLoanQuoteResponseHandler"
  inline_policy {
     name = "dynamodb_put"
     policy = jsonencode({
      Version = "2012-10-17"
      Statement = [
        {
          Action = ["dynamodb:PutItem"],
          Effect = "Allow",
          Resource = aws_dynamodb_table.vendor_response_store.arn
        }
      ]
     })
  }
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Sid    = ""
      Principal = {
        Service = "lambda.amazonaws.com"
      }
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "lambda_policy_vendor_response" {
  role       = aws_iam_role.lambda_vendor_response_exec.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

resource "aws_cloudwatch_event_rule" "completed_loan_request" {
    name = "vendor_response_received"
    description = "Vendor response received"
    event_bus_name = module.eventbridge.eventbridge_bus_name
    event_pattern = <<EOF
{
  "detail-type": [
    "com.vendors.completed-loan-request"
  ]
}
EOF
}

resource "aws_cloudwatch_event_target" "vendor_loan_quote_response_handler_target" {
    rule = aws_cloudwatch_event_rule.completed_loan_request.name
    target_id = "vendor_quote_response_handler_lambda"
    arn = aws_lambda_function.vendor_loan_quote_response_handler.arn
    event_bus_name = module.eventbridge.eventbridge_bus_name
}

resource "aws_lambda_permission" "allow_cloudwatch_to_call_vendor_quote_response_handler" {
    statement_id = "AllowExecutionFromCloudWatch"
    action = "lambda:InvokeFunction"
    function_name = aws_lambda_function.vendor_loan_quote_response_handler.function_name
    principal = "events.amazonaws.com"
    source_arn = aws_cloudwatch_event_rule.completed_loan_request.arn
}

# Aggregator
resource "aws_lambda_function" "aggregator" {
  function_name = "Aggregator"
  s3_bucket = aws_s3_bucket.lambda_bucket.id
  s3_key    = aws_s3_object.lambda_aggregator.key
  runtime = "dotnet6"
  handler = "Aggregator::Aggregator.Function::FunctionHandler"
  source_code_hash = data.archive_file.lambda_aggregator.output_base64sha256
  role = aws_iam_role.lambda_aggregator_exec.arn
  timeout = 30
  environment {
    variables = {
      "TABLE_NAME" = aws_dynamodb_table.vendor_response_store.name
    }
  }
}

resource "aws_cloudwatch_log_group" "aggregator" {
  name = "/aws/lambda/${aws_lambda_function.aggregator.function_name}"

  retention_in_days = 30
}

resource "aws_iam_role" "lambda_aggregator_exec" {
  name = "FunctionRole_Aggregator"
  inline_policy {
     name = "dynamodb_quert"
     policy = jsonencode({
      Version = "2012-10-17"
      Statement = [
        {
          Action = ["dynamodb:Query"],
          Effect = "Allow",
          Resource = aws_dynamodb_table.vendor_response_store.arn
        }
      ]
     })
  }
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Sid    = ""
      Principal = {
        Service = "lambda.amazonaws.com"
      }
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "lambda_policy_aggregator" {
  role       = aws_iam_role.lambda_aggregator_exec.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}


resource "aws_lambda_permission" "allow_stepfunctions_to_call_aggregator" {
    statement_id = "AllowExecutionFromStepFunctions"
    action = "lambda:InvokeFunction"
    function_name = aws_lambda_function.aggregator.function_name
    principal = "states.amazonaws.com"
    source_arn = module.step_function.state_machine_arn
}

module "step_function" {
  source = "terraform-aws-modules/step-functions/aws"

  name       = "scatter-gather-orchestrator"
  definition = replace(file("statemachine/scatter-gather-orchestration.asl.json"), "{{AGGREGATOR_FUNCTION_ARN}}", aws_lambda_function.aggregator.arn)

  service_integrations = {
    eventbridge = {
        eventbridge = [module.eventbridge.eventbridge_bus_arn]
    },
    lambda = {
      lambda = [aws_lambda_function.aggregator.arn]
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
