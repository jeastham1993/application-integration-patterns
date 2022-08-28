output "EventBusArn" {
    value = module.eventbridge.eventbridge_bus_arn
}

output "EventBusName" {
    value = module.eventbridge.eventbridge_bus_name
}