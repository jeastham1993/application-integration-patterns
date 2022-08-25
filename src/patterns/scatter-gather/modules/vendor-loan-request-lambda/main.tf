module lambda_vendor_loan_quote_generator {
  source = "../lambda-function"
  lambda_bucket_id = var.lambda_bucket_id
  publish_dir = "${path.module}/../../functions/VendorLoanQuoteGenerator/bin/Release/net6.0/linux-x64/publish"
  zip_file = "VendorLoanQuoteGenerator.zip"
  function_name = "VendorLoanQuoteGenerator_${replace(var.vendor_name, " ", "_")}"
  lambda_handler = "VendorLoanQuoteGenerator::VendorLoanQuoteGenerator.Function::FunctionHandler"
  environment_variables = {
    "EVENT_BUS_NAME" = var.event_bridge_name
    "VENDOR_NAME" = var.vendor_name
  }
}

resource "aws_iam_role_policy_attachment" "vendor_loan_generator_event_bridge_policy" {
  role       = module.lambda_vendor_loan_quote_generator.function_role_name
  policy_arn = var.event_bridge_put_events_policy_arn
}

module "request_loan_offers_event_rule" {
  source = "../event-bridge-lambda-target"
  event_bridge_name = var.event_bridge_name
  event_bridge_rule_name = "new-loan-request-${replace(var.vendor_name, " ", "_")}"
  lambda_function_name = module.lambda_vendor_loan_quote_generator.function_name
  lambda_function_arn = module.lambda_vendor_loan_quote_generator.function_arn
  event_pattern = <<EOF
{
  "detail-type": [
    "com.broken.request-loan-offers"
  ]
}
EOF
}