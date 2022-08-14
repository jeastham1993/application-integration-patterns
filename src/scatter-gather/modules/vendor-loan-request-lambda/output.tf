output "function_arn" {
  value       =  aws_lambda_function.vendor_loan_quote_generator.arn
  description = "The arn of the lambda function group"
} 