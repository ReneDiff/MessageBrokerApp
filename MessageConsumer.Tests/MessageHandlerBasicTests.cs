using NUnit.Framework;
using MessageConsumer;
using MessageConsumer.interfaces;
using MessageConsumer.Services;
using MessageShared;
using System;
using Microsoft.Extensions.Logging.Abstractions; 
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis; // Tilføj logging


namespace MessageConsumer.Tests;

public class MessageHandlerBasicTests
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
    public void HandleMessage_MessageOlderThan1Minute_Discard()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var twoMinutesAgo = now.AddMinutes(-2); // sikrer at beskeden er 2 minutter gammel

        var msg = new Message
        {
            Timestamp = twoMinutesAgo,
            Counter = 1
        };

        // Act
        var result = _handler.HandleMessage(msg);

        // Assert
        Assert.That(result, Is.EqualTo(MessageHandlingResult.Discard));
    }

    [Test]
    public void HandleMessage_MessageWithEvenSeconds_SaveToDatabase()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var evenSecond = now.AddSeconds(0 - now.Second % 2); // sikrer lige sekund

        var msg = new Message
        {
            Timestamp = evenSecond,
            Counter = 1
        };

        // Act
        var result = _handler.HandleMessage(msg);

        // Assert
        Assert.That(result, Is.EqualTo(MessageHandlingResult.SaveToDatabase));
    }

    [Test]
    public void HandleMessage_MessageWithOddSeconds_RequeueWithIncrement()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var oddSeconds = now.AddSeconds(1 - now.Second % 2); // sikrer ulige sekund
        var msg = new Message
        {
            Timestamp = oddSeconds,
            Counter = 1
        };

        // Act
        var result = _handler.HandleMessage(msg);
        
        // Assert
        Assert.That(result, Is.EqualTo(MessageHandlingResult.RequeueWithIncrement));
    }
}
