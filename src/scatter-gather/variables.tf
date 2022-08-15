variable "table_name" {
  description = "The name of the DyanamoDB table"
  type        = string
  default = "VendorLoanResponseStore"
}

variable "event_bus_name" {
  description = "The name of the EventBridge event bus"
  type        = string
  default = "application-integration-bus"
}