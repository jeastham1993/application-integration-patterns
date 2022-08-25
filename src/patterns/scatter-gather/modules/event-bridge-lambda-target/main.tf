resource "aws_cloudwatch_event_rule" "event_rule" {
    name = var.event_bridge_rule_name
    event_bus_name = var.event_bridge_name
    event_pattern = var.event_pattern
}

resource "aws_cloudwatch_event_target" "lambda_target" {
    rule = aws_cloudwatch_event_rule.event_rule.name
    target_id = var.lambda_function_name
    arn = var.lambda_function_arn
    event_bus_name = var.event_bridge_name
}

resource "aws_lambda_permission" "allow_event_bridge_to_call_lambda" {
    statement_id = "AllowExecutionFromCloudWatch"
    action = "lambda:InvokeFunction"
    function_name = var.lambda_function_name
    principal = "events.amazonaws.com"
    source_arn = aws_cloudwatch_event_rule.event_rule.arn
}