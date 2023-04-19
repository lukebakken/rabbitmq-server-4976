using System.Diagnostics;
using RabbitMQ.Client;

const string queueName = "rabbitmq-server-4976";

var factory = new ConnectionFactory();
factory.Port = 5672;
factory.HostName = "localhost";
factory.UserName = "guest";
factory.Password = "guest";

var quoumQueueArguments = new Dictionary<string, object> { { "x-queue-type", "quorum" } };
using (var connection = factory.CreateConnection())
{
    using (var channel = connection.CreateModel())
    {
        channel.QueueDelete(queueName);

        channel.ConfirmSelect();

        var declareResult = channel.QueueDeclare(queueName, true, false, false); //create classic queue
        Debug.Assert(queueName.Equals(declareResult.QueueName));

        var props = channel.CreateBasicProperties();
        channel.BasicPublish(string.Empty, queueName, props, ReadOnlyMemory<byte>.Empty);
        channel.WaitForConfirmsOrDie();
    }

    using (var secondChannel = connection.CreateModel())
    {
        secondChannel.ConfirmSelect();

        // NB: adding the following fixes the issue as well
        // var props0 = secondChannel.CreateBasicProperties();
        // secondChannel.BasicPublish(string.Empty, queueName, props0, ReadOnlyMemory<byte>.Empty);
        // secondChannel.WaitForConfirmsOrDie();

        // NB: commenting out the following two lines prevents the issue from happening,
        // or if you move it to use the first channel
        var message = secondChannel.BasicGet(queueName, false);
        secondChannel.BasicAck(message.DeliveryTag, false);

        secondChannel.QueueDelete(queueName);

        var declareResult = secondChannel.QueueDeclare(queueName, true, false, false, quoumQueueArguments); //create quorum queue
        // var declareResult = secondChannel.QueueDeclare(queueName, true, false, false); //create classic queue
        Debug.Assert(queueName.Equals(declareResult.QueueName));

        var props1 = secondChannel.CreateBasicProperties();
        secondChannel.BasicPublish(string.Empty, queueName, props1, ReadOnlyMemory<byte>.Empty);
        secondChannel.WaitForConfirmsOrDie(); //throws AlreadyClosedException here

        secondChannel.QueueDelete(queueName);
    }
}
