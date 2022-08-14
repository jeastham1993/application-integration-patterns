variable "vendor_name" {
  description = "The name of the vendor"
  type        = string
}

variable "lambda_bucket_id" {
  description = "The id of the bucket lambda function code will be stored"
  type        = string
}

variable "event_bridge_arn" {
  description = "The ARN of the event bridge to publish to"
  type        = string
}

variable "event_bridge_name" {
  description = "The name of the event bridge to publish to"
  type        = string
}