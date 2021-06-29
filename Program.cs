using System;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.MemoryStorage;
using MassTransit;
using MassTransit.Context;
using MassTransit.Scheduling;
using MassTransit.Util;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace masstransit_hangfire_awssqs_poc
{
    public class Program
    {
        public static async Task Main()
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Console(LogEventLevel.Debug)
                .MinimumLevel.Debug()
                .CreateLogger();

            GlobalConfiguration.Configuration.UseMemoryStorage();
            
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(logger, true);
            var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                LogContext.ConfigureCurrentLogContext(loggerFactory);

                cfg.Host(new Uri("rabbitmq://guest:guest@localhost:5672"));

                cfg.UseHangfireScheduler("hangfire",
                    options => { options.SchedulePollingInterval = TimeSpan.FromSeconds(1); });

                cfg.ReceiveEndpoint("message",
                    configure =>
                    {
                        configure.Consumer(() => new MessageConsumer(loggerFactory.CreateLogger<MessageConsumer>()));
                    });

                cfg.UseMessageScheduler(new Uri("queue:hangfire"));
            });
            await busControl.StartAsync();
            var consumerQueue = new Uri("queue:message");
            await busControl.ScheduleRecurringSend<IMessage>(consumerQueue, new RecurringSchedule(), new
            {
                Id = NewId.NextGuid(),
                Message = "foobar"
            });
            Console.WriteLine("Press any key to quit");
            Console.ReadKey();
            await busControl.StopAsync();
            loggerFactory.Dispose();
        }
    }

    public class RecurringSchedule : DefaultRecurringSchedule
    {
        public RecurringSchedule()
        {
            CronExpression = "*/10 * * * * *"; // Every 10 seconds
            MisfirePolicy = MissedEventPolicy.Skip;
        }
    }

    public interface IMessage
    {
        Guid Id { get; }
        string Message { get; }
    }

    public class MessageConsumer : IConsumer<IMessage>
    {
        private readonly ILogger<MessageConsumer> _logger;

        public MessageConsumer(ILogger<MessageConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<IMessage> context)
        {
            _logger.LogInformation(
                "Message: {@Message} {MessageId} {ConversationId} {CorrelationId} ({id}) received at {receivedAt}",
                context.Message,
                context.MessageId,
                context.ConversationId,
                context.CorrelationId,
                context.Message.Id,
                DateTime.Now);
            return TaskUtil.Completed;
        }
    }
}
