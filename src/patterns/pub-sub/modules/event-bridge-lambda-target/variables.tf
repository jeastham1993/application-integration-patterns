variable "event_bridge_name" {
  description = "The name of the event bridge to publish to"
  type        = string
}

variable "event_bridge_rule_name" {
  description = "The name of the event bridge rule"
  type = string
}

variable "lambda_function_name" {
  description = "The name of the lambda function to target"
  type = string
}

variable "lambda_function_arn" {
  description = "The ARN of the lambda function to target"
  type = string
}

variable "event_pattern" {
    description = "The event pattern to match"
    type = string
}