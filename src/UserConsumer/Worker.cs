using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedModels;
using SharedModels.Config;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UserConsumer
{
    internal class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly RabbitMqConfiguration _rabbitMqConfiguration;
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public Worker(ILogger<Worker> logger, IOptions<RabbitMqConfiguration> options)
        {
            _logger = logger;
            _rabbitMqConfiguration = options.Value;
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqConfiguration.Host
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(
                queue: _rabbitMqConfiguration.Queue,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                return ConsumeMessages();
            }

            return Task.CompletedTask;
        }

        private Task ConsumeMessages()
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (sender, eventArgs) =>
            {
                var contentArray = eventArgs.Body.ToArray();
                var contentString = Encoding.UTF8.GetString(contentArray);
                var message = JsonConvert.DeserializeObject<UserModel>(contentString);

                _logger.LogInformation($"Received message at: {DateTimeOffset.Now} : {contentString}");

                _channel.BasicAck(eventArgs.DeliveryTag, false);
            };

            _channel.BasicConsume(_rabbitMqConfiguration.Queue, false, consumer);

            return Task.CompletedTask;
        }
    }
}