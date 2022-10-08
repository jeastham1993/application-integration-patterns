variable "table_name" {
  description = "The name of the DyanamoDB table"
  type        = string
  default = "ApplicationIntegrationPatternsExample"
}

variable "code_bucket_name" {
  description = "The name of the S3 bucket to store Lambda source code"
  type        = string
  default = "synchronous-api-source-code-bucket"
}

variable "environment" {
  description = "The current environment"
  type        = string
  default = "dev"
}

variable "honeycomb_api_key" {
  description = "API Key to pass to Honeycomb"
  type = string
}