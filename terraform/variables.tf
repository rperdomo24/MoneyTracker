variable "aws_region" {
  type    = string
  default = "us-east-2"
}

variable "openai_api_key" {
  description = "Tu API key de OpenAI"
  type        = string
  sensitive   = true
}

variable "google_client_id" {
  description = "Client ID de Google para OAuth"
  type        = string
  sensitive   = true
}

variable "google_client_secret" {
  description = "Client Secret Key de Google para OAuth"
  type        = string
  sensitive   = true
}

variable "google_redirect_uri" {
  description = "Redirect URI para recibir el code en Lambda"
  type        = string
}