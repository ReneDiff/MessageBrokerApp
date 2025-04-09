using NUnit.Framework;
using MessageConsumer;
using MessageShared;
using System;
using Microsoft.Extensions.Logging.Abstractions; // <-- TILFØJ DENNE
using Microsoft.Extensions.Logging; // Tilføj logging


namespace MessageConsumer.Tests;

public class MessageHandlerTests
{
    private IMessageHandler _handler = null;

    [SetUp]
    public void Setup()
    {
        // Opret en NullLogger (logger der ingenting gør)
        ILogger<MessageHandler> dummyLogger = new NullLogger<MessageHandler>();

        // Giv loggeren med til MessageHandler's konstruktør
        _handler = new MessageHandler(dummyLogger);
    }

    [Test]
    public void Discards_Message_Older_Than_1Minute()
    {
        var msg = new Message
        {
            Timestamp = DateTime.UtcNow.AddMinutes(-2),
            Counter = 1
        };

        var result = _handler.HandleMessage(msg);
        Assert.That(result, Is.EqualTo(MessageHandlingResult.Discard));
    }

    [Test]
    public void Saves_Message_With_Even_Seconds()
    {
        var evenSecond = DateTime.UtcNow.AddSeconds(0 - DateTime.UtcNow.Second % 2); // sikrer lige sekund
        var msg = new Message
        {
            Timestamp = evenSecond,
            Counter = 1
        };

        var result = _handler.HandleMessage(msg);
        Assert.That(result, Is.EqualTo(MessageHandlingResult.SaveToDatabase));
    }

    [Test]
    public void Requeues_Message_With_Odd_Seconds()
    {
        var oddSeconds = DateTime.UtcNow.AddSeconds(1 - DateTime.UtcNow.Second % 2); // sikrer ulige sekund
        var msg = new Message
        {
            Timestamp = oddSeconds,
            Counter = 1
        };

        var result = _handler.HandleMessage(msg);
        
        Assert.That(result, Is.EqualTo(MessageHandlingResult.RequeueWithIncrement));
    }
}
