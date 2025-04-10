using NUnit.Framework;
using MessageConsumer;
using MessageConsumer.interfaces;
using MessageConsumer.Services;
using MessageShared;
using System;
using Microsoft.Extensions.Logging.Abstractions; 
using Microsoft.Extensions.Logging;

namespace MessageConsumer.Tests;

public class MessageHandlerCounterTests
{
    private IMessageHandler _handler;

    [SetUp]
    public void Setup()
    {
        _handler = new MessageHandler(new NullLogger<MessageHandler>());
    }

    
    [Test]
    public void HandleMessage_MaxRetriesReached_Discard()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var evenSecond = now.AddSeconds(0 - now.Second % 2); // Ensure even second
        var message = new Message
        {
            RetryCount = 5, // Simulate max retries reached
            Timestamp = evenSecond // Timestamp within the last minute and even second
        };

        // Act
        var result = _handler.HandleMessage(message);

        // Assert
        Assert.That(result, Is.EqualTo(MessageHandlingResult.Discard));
    }


    [Test]
    public void HandleMessage_CounterIsMoreThanMaxRetriesAndMaxRetriesNotReached_SaveToDatabase()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var evenSecond = now.AddSeconds(0 - now.Second % 2); // Ensure even second
        var message = new Message
        {
            Counter = 6, // Simulate counter more than max retries
            RetryCount = 3, // Simulate max retries not reached
            Timestamp = evenSecond // Timestamp within the last minute and even second
        };

        // Act
        var result = _handler.HandleMessage(message);

        // Assert
        Assert.That(result, Is.EqualTo(MessageHandlingResult.SaveToDatabase));
    }

    [Test]
    public void HandleMessage_OddSecond_CounterIsMoreThanMaxRetries_MaxRetriesNotReached_RequeueWithIncrement()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var oddSecond = now.AddSeconds(1 - now.Second % 2); // Ensure odd second
        var message = new Message
        {
            Counter = 6, // Simulate counter more than max retries
            RetryCount = 3, // Simulate max retries not reached
            Timestamp = oddSecond // Timestamp within the last minute and even second
        };

        // Act
        var result = _handler.HandleMessage(message);

        // Assert
        Assert.That(result, Is.EqualTo(MessageHandlingResult.RequeueWithIncrement));
    }
}