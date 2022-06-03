using RabbitMQ.Client;

var factory = new ConnectionFactory();

var arguments = new Dictionary<string, object> { { "x-queue-type", "quorum" } };
using var connection = factory.CreateConnection();

using var channel = connection.CreateModel();
channel.ConfirmSelect();

//replace all secondChannel usage with channel and the code will work properly
using var secondChannel = connection.CreateModel();
secondChannel.ConfirmSelect();

channel.QueueDeclare("main", true, false, false); //create classic queue
channel.BasicPublish(string.Empty, "main", channel.CreateBasicProperties(), ReadOnlyMemory<byte>.Empty);
channel.WaitForConfirmsOrDie();

var message = secondChannel.BasicGet("main", false);
secondChannel.BasicAck(message.DeliveryTag, false);

channel.QueueDelete("main");
channel.QueueDeclare("main", true, false, false, arguments); //create quorum queue

secondChannel.BasicPublish(string.Empty, "main", secondChannel.CreateBasicProperties(), ReadOnlyMemory<byte>.Empty);
secondChannel.WaitForConfirmsOrDie(); //throws AlreadyClosedException here

channel.QueueDelete("main");