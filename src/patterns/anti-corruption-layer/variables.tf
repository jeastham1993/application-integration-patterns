variable "code_bucket_name" {
  description = "The name of the S3 bucket to store Lambda source code"
  type        = string
  default     = "acl-source-code-bucket"
}

variable "event_bus_name" {
  description = "The name of the Amazon Event Bridge event bus to publish to"
  type        = string
  default     = "acl-event-bridge"
}

variable "topic_name" {
  description = "The name of the AWS SNS Topic to publish to"
  type        = string
  default     = "membership-customer-created-event"
}