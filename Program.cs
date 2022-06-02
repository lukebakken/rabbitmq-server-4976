using RabbitMQ.Client;

var QuorumQueueArguments = new Dictionary<string, object> { { "x-queue-type", "quorum" } };
var factory = new ConnectionFactory
{
    Uri = new Uri("amqp://guest:guest@localhost")
};

SetupQueues(factory);
AddTestMessage(factory);

using (var connection = factory.CreateConnection())
{
    using (var channel = connection.CreateModel())
    {
        // adding the test message here makes it work as expected
        //channel.BasicPublish(string.Empty, "main", channel.CreateBasicProperties(), ReadOnlyMemory<byte>.Empty);
        channel.QueueDeclare("temp", true, false, false, QuorumQueueArguments);
        channel.QueueBind("temp", "main", string.Empty);
        channel.QueueUnbind("main", "main", string.Empty);

        var messageFromMain = channel.BasicGet("main", false);

        channel.BasicPublish(string.Empty, "temp", messageFromMain.BasicProperties, messageFromMain.Body);
        channel.BasicAck(messageFromMain.DeliveryTag, false);

        channel.QueueDelete("main");
        channel.QueueDeclare("main", true, false, false, QuorumQueueArguments);
        channel.QueueBind("main", "main", string.Empty);

        var messageFromTemp = channel.BasicGet("temp", false);

        channel.BasicPublish(string.Empty, "main", messageFromTemp.BasicProperties, messageFromTemp.Body);
        channel.BasicAck(messageFromTemp.DeliveryTag, false);

        //using the channel from this point on will throw
        Console.WriteLine("Num messages in main: " + channel.MessageCount("main"));
    }
}

Console.WriteLine("Done");
Console.ReadLine();

void SetupQueues(ConnectionFactory factory)
{
    using (var connection = factory.CreateConnection())
    {
        using (var channel = connection.CreateModel())
        {
            try
            {
                channel.QueueDelete("main");
            }
            catch (Exception)
            {
            }
        }
    }

    using (var connection = factory.CreateConnection())
    {
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare("main", true, false, false);
            channel.ExchangeDeclare("main", ExchangeType.Fanout, true);
            channel.QueueBind("main", "main", string.Empty);
        }
    }
}

void AddTestMessage(ConnectionFactory factory)
{
    using (var connection = factory.CreateConnection())
    {
        using (var channel = connection.CreateModel())
        {
            channel.BasicPublish(string.Empty, "main", channel.CreateBasicProperties(), ReadOnlyMemory<byte>.Empty);
        }
    }
}

