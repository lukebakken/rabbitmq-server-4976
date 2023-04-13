using System.Diagnostics;
using RabbitMQ.Client;

const string queueName = "rabbitmq-server-4976";

var factory = new ConnectionFactory();
factory.Port = 5672;
factory.HostName = "shostakovich";
factory.UserName = "guest";
factory.Password = "guest";

var quoumQueueArguments = new Dictionary<string, object> { { "x-queue-type", "quorum" } };
using var connection = factory.CreateConnection();

using (var channel = connection.CreateModel())
{
    channel.QueueDelete(queueName);

    channel.ConfirmSelect();

    var declareResult = channel.QueueDeclare(queueName, true, false, false); //create classic queue
    Debug.Assert(queueName.Equals(declareResult.QueueName));

    channel.BasicPublish(string.Empty, queueName, channel.CreateBasicProperties(), ReadOnlyMemory<byte>.Empty);
    channel.WaitForConfirmsOrDie();
}

using (var secondChannel  = connection.CreateModel())
{
    secondChannel.ConfirmSelect();

    var message = secondChannel.BasicGet(queueName, false);
    secondChannel.BasicAck(message.DeliveryTag, false);

    secondChannel.QueueDelete(queueName);
    var declareResult = secondChannel.QueueDeclare(queueName, true, false, false, quoumQueueArguments); //create quorum queue
    Debug.Assert(queueName.Equals(declareResult.QueueName));

    secondChannel.BasicPublish(string.Empty, queueName, secondChannel.CreateBasicProperties(), ReadOnlyMemory<byte>.Empty);
    secondChannel.WaitForConfirmsOrDie(); //throws AlreadyClosedException here

    secondChannel.QueueDelete(queueName);
}
