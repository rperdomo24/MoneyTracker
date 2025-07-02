output "gmail_watcher_url" {
  description = "URL pública para usar en el watcher de Gmail Pub/Sub"
  value       = "${aws_apigatewayv2_api.gmail_api.api_endpoint}/gmail/webhook"
}

output "gmail_auth_url" {
  description = "URL pública para la autenticación de Gmail"
  value       = "${aws_apigatewayv2_api.gmail_api.api_endpoint}/auth/start"
}