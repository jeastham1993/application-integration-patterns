output "queue_id" {
  value = aws_sqs_queue.main_queue.id
}

output "queue_arn" {
  value = aws_sqs_queue.main_queue.arn
}

output "queue_name" {
  value = aws_sqs_queue.main_queue.name
}

output "dlq_id" {
  value = aws_sqs_queue.dead_letter_queue.id
}

output "dlq_arn" {
  value = aws_sqs_queue.dead_letter_queue.arn
}

output "dlq_name" {
  value = aws_sqs_queue.dead_letter_queue.name
} 
