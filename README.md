Sample repository reproducing an apparent bug in MassTransit when scheduling recurring messages using Hangfire combined with AWS SQS.

The `rmq` branch contains a working version against RabbitMQ indicating `MassTransit.AmazonSQS` might be the culprit.

## Prerequisites
- `master` branch: replace the access and secret key in `Program.cs` with those of your own AWS account
- `rmq` branch: make sure you have RabbitMQ running locally (dockerized: `docker run -d --name mt-hangfire-rabbit -p 15672:15672 -p 5672:5672 rabbitmq:3-management`)
