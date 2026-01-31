resource "aws_s3_bucket" "receipts" {
  bucket = "order-receipts"
}

resource "aws_dynamodb_table" "orders" {
  name           = "Orders"
  read_capacity  = 5
  write_capacity = 5
  hash_key       = "order_id"

  attribute {
    name = "order_id"
    type = "S"
  }
}