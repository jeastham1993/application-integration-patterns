output "function_arn" {
  value       =  module.lambda_vendor_loan_quote_generator.function_arn
  description = "The arn of the lambda function"
} 