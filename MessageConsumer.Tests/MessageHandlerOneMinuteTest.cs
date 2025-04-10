using NUnit.Framework;
using MessageConsumer;
using MessageConsumer.interfaces;
using MessageConsumer.Services;
using MessageShared;
using System;
using Microsoft.Extensions.Logging.Abstractions; 
using Microsoft.Extensions.Logging;

namespace MessageConsumer.Tests;

public class MessageHandlerOneMinuteTests
{
    private IMessageHandler _handler;

    [SetUp]
    public void Setup()
    {
        _handler = new MessageHandler(new NullLogger<MessageHandler>());
    }

    // Slightly irrelevant
    [Test]
    public void HandleMessage_EvenSecondAnd59SecondsOld_SaveToDatabase()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var fiftyNineSecondsAgo = now.AddSeconds(-59);

        // Ensure the timestamp 59 seconds ago has an even second
        var targetSecond = fiftyNineSecondsAgo.Second % 2 == 0
                           ? fiftyNineSecondsAgo.Second
                           : fiftyNineSecondsAgo.Second - 1;

        var fiftyNineSecondsAgoEvenSecond = new DateTime(fiftyNineSecondsAgo.Year, fiftyNineSecondsAgo.Month, fiftyNineSecondsAgo.Day,
                                                        fiftyNineSecondsAgo.Hour, fiftyNineSecondsAgo.Minute, targetSecond, DateTimeKind.Utc);

        var msg = new Message { Timestamp = fiftyNineSecondsAgoEvenSecond, Counter = 0 };

        // Act
        var result = _handler.HandleMessage(msg);

        // Assert
        Assert.That(result, Is.EqualTo(MessageHandlingResult.SaveToDatabase));
    }

    [Test]
    public void HandleMessage_EvenSecondAnd1Minute1SecondOld_Discard()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var oneMinuteOneSecondAgo = now.AddMinutes(-1).AddSeconds(-1); // Subtract 1 second more

        // Ensure the timestamp 1 minute 1 second ago has an even second
        var targetSecond = oneMinuteOneSecondAgo.Second % 2 == 0
                           ? oneMinuteOneSecondAgo.Second
                           : oneMinuteOneSecondAgo.Second - 1;

        var oneMinuteOneSecondAgoEvenSecond = new DateTime(oneMinuteOneSecondAgo.Year, oneMinuteOneSecondAgo.Month, oneMinuteOneSecondAgo.Day,
                                                           oneMinuteOneSecondAgo.Hour, oneMinuteOneSecondAgo.Minute, targetSecond, DateTimeKind.Utc);

        var msg = new Message { Timestamp = oneMinuteOneSecondAgoEvenSecond, Counter = 0 };

        // Act
        var result = _handler.HandleMessage(msg);

        // Assert
        Assert.That(result, Is.EqualTo(MessageHandlingResult.Discard));
    }

    // Slightly irrelevant
    [Test]
    public void HandleMessage_OddSecondAnd59SecondsOld_RequeueWithIncrement()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var oneMinuteAgoEvenSecond = now.AddMinutes(-1).AddSeconds(3 - now.Second % 2);
        var msg = new Message 
        { 
            Timestamp = oneMinuteAgoEvenSecond, 
            Counter = 0 
        };
        
        // Act 
        var result = _handler.HandleMessage(msg);

        // Assert 
        Assert.That(result, Is.EqualTo(MessageHandlingResult.RequeueWithIncrement));
    }

    [Test]
    public void HandleMessage_OddSecondAnd1MinOld_RequeueWithIncrement()
    {
        // Arrange
        var now = DateTime.UtcNow;

        var oneMinuteAgoOddSecond = now.AddMinutes(-1).AddSeconds(3 - now.Second % 2);
        var msg = new Message 
        { 
            Timestamp = oneMinuteAgoOddSecond, 
            Counter = 0 
        };

        // Act 
        var result = _handler.HandleMessage(msg);
            
        // Assert 
        Assert.That(result, Is.EqualTo(MessageHandlingResult.RequeueWithIncrement));
    }
}