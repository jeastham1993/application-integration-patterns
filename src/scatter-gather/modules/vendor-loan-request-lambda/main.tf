data "archive_file" "lambda_vendor_loan_quote_generator" {
  type = "zip"

  source_dir  = "${path.module}/../../functions/VendorLoanQuoteGenerator/bin/Release/net6.0/linux-x64/publish"
  output_path = "${path.module}/../../VendorLoanQuoteGenerator.zip"
}

resource "aws_s3_object" "lambda_vendor_loan_quote_generator" {
  bucket = var.lambda_bucket_id

  key    = "VendorLoanQuoteGenerator.zip"
  source = data.archive_file.lambda_vendor_loan_quote_generator.output_path

  etag = filemd5(data.archive_file.lambda_vendor_loan_quote_generator.output_path)
}

# Vendor Loan Quote Generator
resource "aws_lambda_function" "vendor_loan_quote_generator" {
  function_name = "VendorLoanQuoteGenerator_${replace(var.vendor_name, " ", "_")}"
  s3_bucket = var.lambda_bucket_id
  s3_key    = aws_s3_object.lambda_vendor_loan_quote_generator.key
  runtime = "dotnet6"
  handler = "VendorLoanQuoteGenerator::VendorLoanQuoteGenerator.Function::FunctionHandler"
  source_code_hash = data.archive_file.lambda_vendor_loan_quote_generator.output_base64sha256
  role = aws_iam_role.lambda_exec.arn
  timeout = 30
  environment {
    variables = {
      "EVENT_BUS_NAME" = var.event_bridge_name
      "VENDOR_NAME" = var.vendor_name
    }
  }
}

resource "aws_cloudwatch_log_group" "vendor_loan_quote_generator" {
  name = "/aws/lambda/${aws_lambda_function.vendor_loan_quote_generator.function_name}"

  retention_in_days = 30
}

resource "aws_iam_role" "lambda_exec" {
  name = "FunctionRole_VendorLoanQuoteGenerator_${replace(var.vendor_name, " ", "_")}"
  inline_policy {
     name = "allow_event_bridge_put_events"
     policy = jsonencode({
      Version = "2012-10-17"
      Statement = [
        {
          Action = ["events:PutEvents"],
          Effect = "Allow",
          Resource = var.event_bridge_arn
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

resource "aws_iam_role_policy_attachment" "lambda_policy" {
  role       = aws_iam_role.lambda_exec.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

resource "aws_cloudwatch_event_rule" "new_loan_request" {
    name = "new-loan-request"
    description = "Request for a new loan"
    event_bus_name = var.event_bridge_name
    event_pattern = <<EOF
{
  "detail-type": [
    "com.broken.request-loan-offers"
  ]
}
EOF
}

resource "aws_cloudwatch_event_target" "vendor_loan_quote_generator_target" {
    rule = aws_cloudwatch_event_rule.new_loan_request.name
    target_id = "vendor_quote_generator_lambda_${replace(var.vendor_name, " ", "_")}"
    arn = aws_lambda_function.vendor_loan_quote_generator.arn
    event_bus_name = var.event_bridge_name
}

resource "aws_lambda_permission" "allow_cloudwatch_to_call_vendor_quote_generator" {
    statement_id = "AllowExecutionFromCloudWatch"
    action = "lambda:InvokeFunction"
    function_name = aws_lambda_function.vendor_loan_quote_generator.function_name
    principal = "events.amazonaws.com"
    source_arn = aws_cloudwatch_event_rule.new_loan_request.arn
}