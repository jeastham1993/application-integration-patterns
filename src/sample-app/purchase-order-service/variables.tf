variable "code_bucket_name" {
  description = "The name of the S3 bucket to store Lambda source code"
  type        = string
  default = "purchase-order-service-source-code-bucket"
}

variable "environment" {
  description = "The current environment"
  type        = string
  default = "dev"
}

variable "product_created_topic_arn" {
  description = "ARN for the product created topic"
  type        = string
}