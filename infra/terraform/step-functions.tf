resource "aws_sfn_state_machine" "order_processor_saga" {
  name     = "OrderProcessorSaga"
  role_arn = aws_iam_role.step_functions_role.arn

  definition = jsonencode({
    Comment = "Saga de Processamento de Ordem da Corretora"
    StartAt = "ValidarSaldo"
    States = {
      ValidarSaldo = {
        Type     = "Task"
        Resource = aws_lambda_function.validar_saldo.arn
        Next     = "SaldoSuficiente?"
      }
      "SaldoSuficiente?" = {
        Type = "Choice"
        Choices = [
          {
            Variable      = "$.SaldoValido"
            BooleanEquals = true
            Next          = "BloquearSaldo"
          }
        ]
        Default = "RejeitarOrdem"
      }
      BloquearSaldo = {
        Type     = "Task"
        Resource = "arn:aws:lambda:us-east-1:000000000000:function:BloquearSaldo"
        End      = true
      }
      RejeitarOrdem = {
        Type  = "Fail"
        Error = "SaldoInsuficiente"
        Cause = "O cliente não possui saldo suficiente."
      }
    }
  })
}

# Role necessária (mesmo no LocalStack, o Terraform exige um IAM Role)
resource "aws_iam_role" "step_functions_role" {
  name = "step_functions_role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "states.amazonaws.com"
        }
      }
    ]
  })
}