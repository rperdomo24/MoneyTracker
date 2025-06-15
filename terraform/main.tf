data "archive_file" "lambda_auth_start_zip" {
  type        = "zip"
  source_dir  = "${path.module}/lambda/auth_start"
  output_path = "${path.module}/lambda_auth_start.zip"
}

resource "aws_lambda_function" "auth_start" {
  function_name    = "gmail-auth-start"
  filename         = data.archive_file.lambda_auth_start_zip.output_path
  source_code_hash = data.archive_file.lambda_auth_start_zip.output_base64sha256
  handler          = "index.handler"
  runtime          = "nodejs20.x"
  role             = aws_iam_role.lambda_exec_role.arn
  timeout          = 120

  environment {
    variables = {
      GOOGLE_CLIENT_ID     = var.google_client_id
      GOOGLE_REDIRECT_URI  = var.google_redirect_uri
    }
  }
}

resource "aws_apigatewayv2_integration" "auth_start_integration" {
  api_id                = aws_apigatewayv2_api.gmail_api.id
  integration_type      = "AWS_PROXY"
  integration_uri       = aws_lambda_function.auth_start.invoke_arn
  integration_method    = "POST"
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_route" "auth_start_route" {
  api_id    = aws_apigatewayv2_api.gmail_api.id
  route_key = "GET /auth/start"
  target    = "integrations/${aws_apigatewayv2_integration.auth_start_integration.id}"
}

terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  required_version = ">= 1.3.0"
}

provider "aws" {
  region = var.aws_region
}

resource "aws_iam_role" "lambda_exec_role" {
  name = "gmail-watcher-lambda-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17",
    Statement = [{
      Action = "sts:AssumeRole",
      Principal = {
        Service = "lambda.amazonaws.com"
      },
      Effect = "Allow"
    }]
  })
}

resource "aws_iam_role_policy_attachment" "lambda_basic_execution" {
  role       = aws_iam_role.lambda_exec_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

data "archive_file" "lambda_gmail_watcher_zip" {
  type        = "zip"
  source_dir  = "${path.module}/lambda/gmail_watcher"
  output_path = "${path.module}/lambda_gmail_watcher.zip"
}

resource "aws_lambda_function" "gmail_watcher" {
  function_name    = "gmail-watcher"
  filename         = data.archive_file.lambda_gmail_watcher_zip.output_path
  source_code_hash = data.archive_file.lambda_gmail_watcher_zip.output_base64sha256
  handler          = "index.handler"
  runtime          = "nodejs20.x"
  role             = aws_iam_role.lambda_exec_role.arn
  timeout          = 120

  environment {
    variables = {
      OPENAI_KEY           = var.openai_api_key
      GOOGLE_CLIENT_ID     = var.google_client_id
      GOOGLE_CLIENT_SECRET = var.google_client_secret
    }
  }
}

data "archive_file" "lambda_auth_callback_zip" {
  type        = "zip"
  source_dir  = "${path.module}/lambda/auth_callback"
  output_path = "${path.module}/lambda_auth_callback.zip"
}

resource "aws_lambda_function" "auth_callback" {
  function_name    = "gmail-auth-callback"
  filename         = data.archive_file.lambda_auth_callback_zip.output_path
  source_code_hash = data.archive_file.lambda_auth_callback_zip.output_base64sha256
  handler          = "index.handler"
  runtime          = "nodejs20.x"
  role             = aws_iam_role.lambda_exec_role.arn
  timeout          = 120

  environment {
    variables = {
      GOOGLE_CLIENT_ID     = var.google_client_id
      GOOGLE_CLIENT_SECRET = var.google_client_secret
      GOOGLE_REDIRECT_URI  = var.google_redirect_uri
    }
  }
}

resource "aws_apigatewayv2_api" "gmail_api" {
  name          = "gmail-watcher-api"
  protocol_type = "HTTP"
}

resource "aws_apigatewayv2_integration" "lambda_integration" {
  api_id                = aws_apigatewayv2_api.gmail_api.id
  integration_type      = "AWS_PROXY"
  integration_uri       = aws_lambda_function.gmail_watcher.invoke_arn
  integration_method    = "POST"
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_integration" "auth_callback_integration" {
  api_id                = aws_apigatewayv2_api.gmail_api.id
  integration_type      = "AWS_PROXY"
  integration_uri       = aws_lambda_function.auth_callback.invoke_arn
  integration_method    = "POST"
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_route" "lambda_route" {
  api_id    = aws_apigatewayv2_api.gmail_api.id
  route_key = "POST /gmail/webhook"
  target    = "integrations/${aws_apigatewayv2_integration.lambda_integration.id}"
}

resource "aws_apigatewayv2_route" "auth_callback_route" {
  api_id    = aws_apigatewayv2_api.gmail_api.id
  route_key = "GET /auth/callback"
  target    = "integrations/${aws_apigatewayv2_integration.auth_callback_integration.id}"
}

resource "aws_apigatewayv2_stage" "default" {
  api_id      = aws_apigatewayv2_api.gmail_api.id
  name        = "$default"
  auto_deploy = true
}

resource "aws_lambda_permission" "allow_apigw" {
  statement_id  = "AllowFromApiGateway"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.gmail_watcher.arn
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.gmail_api.execution_arn}/*/*"
}

resource "aws_lambda_permission" "allow_apigw_auth_callback" {
  statement_id  = "AllowFromApiGatewayAuthCallback"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.auth_callback.arn
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.gmail_api.execution_arn}/*/*"
}
resource "aws_lambda_permission" "allow_apigw_auth_start" {
  statement_id  = "AllowFromApiGatewayAuthStart"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.auth_start.arn
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.gmail_api.execution_arn}/*/*"
}