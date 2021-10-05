using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using SharedModels;
using SharedModels.Config;
using System.Text;

namespace UserApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class UserController : Controller
    {
        private readonly ConnectionFactory _factory;
        private readonly RabbitMqConfiguration _rabbitMqConfiguration;

        public UserController(IOptions<RabbitMqConfiguration> options)
        {
            _rabbitMqConfiguration = options.Value;
            _factory = new ConnectionFactory
            {
                HostName = _rabbitMqConfiguration.Host
            };
        }

        [HttpPost]
        public ActionResult Create(UserModel user)
        {
            if (user != null)
            {
                using var connection = _factory.CreateConnection();
                using var channel = connection.CreateModel();
                channel.QueueDeclare(
                    queue: _rabbitMqConfiguration.Queue,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                var stringfiedMessage = JsonConvert.SerializeObject(user);
                var bytesMessage = Encoding.UTF8.GetBytes(stringfiedMessage);

                channel.BasicPublish(
                    exchange: "",
                    routingKey: _rabbitMqConfiguration.Queue,
                    basicProperties: null,
                    body: bytesMessage
                );

                return Accepted();
            }
            return BadRequest();
        }
    }
}