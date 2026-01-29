resource "aws_lambda_function" "step_validate" {
  filename      = data.archive_file.lambda_code.output_path
  function_name = "Step_Validate"
  role          = aws_iam_role.lambda_role.arn
  handler       = "workflow/validate.handler" # Caminho dentro do ZIP
  runtime       = "nodejs18.x"
  source_code_hash = data.archive_file.lambda_code.output_base64sha256
}

resource "aws_lambda_function" "step_reserve" {
  filename      = data.archive_file.lambda_code.output_path
  function_name = "Step_Reserve"
  role          = aws_iam_role.lambda_role.arn
  handler       = "workflow/reserve.handler"
  runtime       = "nodejs18.x"
  source_code_hash = data.archive_file.lambda_code.output_base64sha256
}

resource "aws_lambda_function" "step_buy" {
  filename      = data.archive_file.lambda_code.output_path
  function_name = "Step_Buy"
  role          = aws_iam_role.lambda_role.arn
  handler       = "workflow/buy.handler"
  runtime       = "nodejs18.x"
  source_code_hash = data.archive_file.lambda_code.output_base64sha256
}

resource "aws_lambda_function" "step_refund" {
  filename      = data.archive_file.lambda_code.output_path
  function_name = "Step_Refund"
  role          = aws_iam_role.lambda_role.arn
  handler       = "workflow/refund.handler"
  runtime       = "nodejs18.x"
  source_code_hash = data.archive_file.lambda_code.output_base64sha256
}

resource "aws_sfn_state_machine" "order_processing_machine" {
  name     = "OrderProcessingSaga"
  role_arn = aws_iam_role.lambda_role.arn

  definition = jsonencode({
    Comment = "Orquestração de Compra de Ações com Saga Pattern",
    StartAt = "ValidarSaldo",
    States = {
      
      # Passo 1: Validação
      ValidarSaldo = {
        Type = "Task",
        Resource = aws_lambda_function.step_validate.arn,
        Next = "ReservarFundos"
      },

      ReservarFundos = {
        Type = "Task",
        Resource = aws_lambda_function.step_reserve.arn,
        Next = "ComprarNaBolsa"
      },

      ComprarNaBolsa = {
        Type = "Task",
        Resource = aws_lambda_function.step_buy.arn,
        Retry = [
          {
            ErrorEquals = ["States.ALL"], # Para qualquer erro (ex: B3 fora do ar)
            IntervalSeconds = 2,          # Começa esperando 2s
            MaxAttempts = 3,              # Tenta 3 vezes
            BackoffRate = 2.0             # Multiplica por 2 a cada erro (2s -> 4s -> 8s)
          }
        ],
        Next = "Sucesso",
        Catch = [{
          ErrorEquals = ["States.ALL"],
          Next = "ReembolsarCliente"
        }]
      },

      Sucesso = {
        Type = "Succeed"
      },

      ReembolsarCliente = {
        Type = "Task",
        Resource = aws_lambda_function.step_refund.arn,
        Next = "FalhaCompensada"
      },

      FalhaCompensada = {
        Type = "Fail",
        Cause = "A compra falhou, mas o dinheiro foi devolvido.",
        Error = "TransactionFailed"
      }
    }
  })
}