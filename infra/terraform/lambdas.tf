data "archive_file" "lambda_zip" {
  type        = "zip"
  source_dir  = "${path.module}/../../publish"
  output_path = "${path.module}/lambda_validar_saldo.zip"
}

resource "aws_lambda_function" "validar_saldo" {
  filename      = data.archive_file.lambda_zip.output_path
  function_name = "ValidarSaldo"
  role          = aws_iam_role.step_functions_role.arn
  handler       = "Brokerage.Functions::Brokerage.Functions.BalanceFunctions::ValidateBalance"
  runtime       = "dotnet8"

  timeout     = 60
  memory_size = 512

  source_code_hash = data.archive_file.lambda_zip.output_base64sha256
}