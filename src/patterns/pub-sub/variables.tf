variable "code_bucket_name" {
  description = "The name of the S3 bucket to store Lambda source code"
  type        = string
  default = "pub-sub-source-code-bucket"
}

variable "event_bus_name" {
  description = "The name of the Amazon Event Bridge event bus to publish to"
  type        = string
  default = "pub-sub-event-bridge"
}

variable "topic_name" {
  description = "The name of the AWS SNS Topic to publish to"
  type        = string
  default = "pub-sub-topic"
}