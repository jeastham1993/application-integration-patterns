module "eventbridge" {
  source   = "terraform-aws-modules/eventbridge/aws"
  bus_name = "${var.environment}-application-integration-patterns-samples"
  tags = {
    Name = "${var.environment}-application-integration-patterns-samples"
  }
}

resource "aws_ssm_parameter" "event_bus_name" {
  name  = "/${var.environment}/shared/event_bus_name"
  type  = "String"
  value = module.eventbridge.eventbridge_bus_name
}

resource "aws_ssm_parameter" "event_bus_arn" {
  name  = "/${var.environment}/shared/event_bus_arn"
  type  = "String"
  value = module.eventbridge.eventbridge_bus_arn
}