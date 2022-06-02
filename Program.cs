using RabbitMQ.Client;

var QuorumQueueArguments = new Dictionary<string, object> { { "x-queue-type", "quorum" } };
var factory = new ConnectionFactory
{
    Uri = new Uri("amqp://guest:guest@localhost")
};

using (var connection = factory.CreateConnection())
{
    using (var channel = connection.CreateModel())
    {
        channel.QueueDeclare("main", true, false, false);
        channel.QueueDeclare("temp", true, false, false, QuorumQueueArguments);

        channel.BasicPublish(string.Empty, "main", channel.CreateBasicProperties(), ReadOnlyMemory<byte>.Empty);

        var messageFromMain = channel.BasicGet("main", false);

        channel.BasicPublish(string.Empty, "temp", messageFromMain.BasicProperties, messageFromMain.Body);
        channel.BasicAck(messageFromMain.DeliveryTag, false);

        channel.QueueDelete("main");
        channel.QueueDeclare("main", true, false, false, QuorumQueueArguments);

        var messageFromTemp = channel.BasicGet("temp", false);

        channel.BasicPublish(string.Empty, "main", messageFromTemp.BasicProperties, messageFromTemp.Body);
        channel.BasicAck(messageFromTemp.DeliveryTag, false);

        Console.WriteLine("Num messages in main: " + channel.MessageCount("main"));
    }
}

Console.WriteLine("Done");
Console.ReadLine();



