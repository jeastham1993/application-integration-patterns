resource "aws_dynamodb_table" "synchornous_api_table" {
  name             = var.table_name
  billing_mode     = "PAY_PER_REQUEST"
  hash_key         = "PK"
  stream_enabled   = true
  stream_view_type = "NEW_AND_OLD_IMAGES"

  attribute {
    name = "PK"
    type = "S"
  }
}

# Create S3 bucket to store our application source code.
resource "aws_s3_bucket" "lambda_bucket" {
  bucket = var.code_bucket_name

  acl           = "private"
  force_destroy = true
}

# Initialize module containing IAM policies.
module "iam_policies" {
  source      = "../modules/iam-policies"
  table_name  = aws_dynamodb_table.synchornous_api_table.name
  topic_name  = "*"
  environment = var.environment
}

# Create Product Lambda
module "create_product_lambda" {
  source           = "../modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/application/CreateProduct/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "CreateProduct.zip"
  function_name    = "CreateProduct"
  lambda_handler   = "CreateProduct::CreateProduct.Function::TracedFunctionHandler"
  environment_variables = {
    "PRODUCT_TABLE_NAME"           = aws_dynamodb_table.synchornous_api_table.name
    "POWERTOOLS_SERVICE_NAME"      = "product-api"
    "POWERTOOLS_METRICS_NAMESPACE" = "product-api"
  }
}

module "create_product_lambda_api" {
  source        = "../modules/api-gateway-lambda-integration"
  api_id        = module.api_gateway.api_id
  api_arn       = module.api_gateway.api_arn
  function_arn  = module.create_product_lambda.function_arn
  function_name = module.create_product_lambda.function_name
  http_method   = "POST"
  route         = "/"
}

resource "aws_iam_role_policy_attachment" "create_product_lambda_dynamo_db_write" {
  role       = module.create_product_lambda.function_role_name
  policy_arn = module.iam_policies.dynamo_db_write
}

resource "aws_iam_role_policy_attachment" "create_product_lambda_cw_metrics" {
  role       = module.create_product_lambda.function_role_name
  policy_arn = module.iam_policies.cloud_watch_put_metrics
}

resource "aws_iam_role_policy_attachment" "create_product_lambda_sns_publish" {
  role       = module.create_product_lambda.function_role_name
  policy_arn = module.iam_policies.sns_publish_message
}

# Get Product Lambda
module "get_product_lambda" {
  source           = "../modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/application/GetProduct/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "GetProduct.zip"
  function_name    = "GetProduct"
  lambda_handler   = "GetProduct::GetProduct.Function::TracedFunctionHandler"
  environment_variables = {
    "PRODUCT_TABLE_NAME"           = aws_dynamodb_table.synchornous_api_table.name
    "POWERTOOLS_SERVICE_NAME"      = "product-api"
    "POWERTOOLS_METRICS_NAMESPACE" = "product-api"
  }
}

module "get_product_lambda_api" {
  source        = "../modules/api-gateway-lambda-integration"
  api_id        = module.api_gateway.api_id
  api_arn       = module.api_gateway.api_arn
  function_arn  = module.get_product_lambda.function_arn
  function_name = module.get_product_lambda.function_name
  http_method   = "GET"
  route         = "/{productId}"
}

resource "aws_iam_role_policy_attachment" "create_product_lambda_dynamo_db_read" {
  role       = module.get_product_lambda.function_role_name
  policy_arn = module.iam_policies.dynamo_db_read
}

resource "aws_iam_role_policy_attachment" "get_product_lambda_cw_metrics" {
  role       = module.get_product_lambda.function_role_name
  policy_arn = module.iam_policies.cloud_watch_put_metrics
}

# Update Product Lambda
module "update_product_lambda" {
  source           = "../modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/application/UpdateProduct/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "UpdateProduct.zip"
  function_name    = "UpdateProduct"
  lambda_handler   = "UpdateProduct::UpdateProduct.Function::TracedFunctionHandler"
  environment_variables = {
    "PRODUCT_TABLE_NAME"           = aws_dynamodb_table.synchornous_api_table.name
    "POWERTOOLS_SERVICE_NAME"      = "product-api"
    "POWERTOOLS_METRICS_NAMESPACE" = "product-api"
  }
}

module "update_product_lambda_api" {
  source        = "../modules/api-gateway-lambda-integration"
  api_id        = module.api_gateway.api_id
  api_arn       = module.api_gateway.api_arn
  function_arn  = module.update_product_lambda.function_arn
  function_name = module.update_product_lambda.function_name
  http_method   = "PUT"
  route         = "/{productId}"
}

resource "aws_iam_role_policy_attachment" "update_product_lambda_dynamo_db_read" {
  role       = module.update_product_lambda.function_role_name
  policy_arn = module.iam_policies.dynamo_db_crud
}

resource "aws_iam_role_policy_attachment" "update_product_lambda_cw_metrics" {
  role       = module.update_product_lambda.function_role_name
  policy_arn = module.iam_policies.cloud_watch_put_metrics
}

# Delete Product Lambda
module "delete_product_lambda" {
  source           = "../modules/lambda-function"
  lambda_bucket_id = aws_s3_bucket.lambda_bucket.id
  publish_dir      = "${path.module}/application/DeleteProduct/bin/Release/net6.0/linux-x64/publish"
  zip_file         = "DeleteProduct.zip"
  function_name    = "DeleteProduct"
  lambda_handler   = "DeleteProduct::DeleteProduct.Function::TracedFunctionHandler"
  environment_variables = {
    "PRODUCT_TABLE_NAME"           = aws_dynamodb_table.synchornous_api_table.name
    "POWERTOOLS_SERVICE_NAME"      = "product-api"
    "POWERTOOLS_METRICS_NAMESPACE" = "product-api"
  }
}

module "delete_product_lambda_api" {
  source        = "../modules/api-gateway-lambda-integration"
  api_id        = module.api_gateway.api_id
  api_arn       = module.api_gateway.api_arn
  function_arn  = module.delete_product_lambda.function_arn
  function_name = module.delete_product_lambda.function_name
  http_method   = "DELETE"
  route         = "/{productId}"
}

resource "aws_iam_role_policy_attachment" "delete_product_lambda_dynamo_db_read" {
  role       = module.delete_product_lambda.function_role_name
  policy_arn = module.iam_policies.dynamo_db_crud
}

resource "aws_iam_role_policy_attachment" "delete_product_lambda_cw_metrics" {
  role       = module.delete_product_lambda.function_role_name
  policy_arn = module.iam_policies.cloud_watch_put_metrics
}

module "api_gateway" {
  source            = "../modules/api-gateway"
  api_name          = "synchronous-api"
  stage_name        = "dev"
  stage_auto_deploy = true
}
