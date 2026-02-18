using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Ave.Extensions.SocketIO.Client.Infrastructure;

namespace UnitTests.Extensions.SocketIO.Client.Infrastructure;

public class RandomDelayRetryPolicyTests
{
    [Fact(DisplayName = "RDP-001: RetryAsync with times less than 1 should throw ArgumentException")]
    public async Task RDP001()
    {
        var mockRandom = new Mock<IRandom>();
        var policy = new RandomDelayRetryPolicy(mockRandom.Object);

        var act = () => policy.RetryAsync(0, () => Task.CompletedTask);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact(DisplayName = "RDP-002: RetryAsync with times 1 should execute func once")]
    public async Task RDP002()
    {
        var mockRandom = new Mock<IRandom>();
        var policy = new RandomDelayRetryPolicy(mockRandom.Object);
        var callCount = 0;

        await policy.RetryAsync(1, () =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        callCount.Should().Be(1);
    }

    [Fact(DisplayName = "RDP-003: RetryAsync should succeed on first try and not call random")]
    public async Task RDP003()
    {
        var mockRandom = new Mock<IRandom>();
        var policy = new RandomDelayRetryPolicy(mockRandom.Object);

        await policy.RetryAsync(3, () => Task.CompletedTask);

        mockRandom.Verify(r => r.Next(It.IsAny<int>()), Times.Never);
    }

    [Fact(DisplayName = "RDP-004: RetryAsync should retry on failure and eventually succeed")]
    public async Task RDP004()
    {
        var mockRandom = new Mock<IRandom>();
        mockRandom.Setup(r => r.Next(It.IsAny<int>())).Returns(0);
        var policy = new RandomDelayRetryPolicy(mockRandom.Object);
        var callCount = 0;

        await policy.RetryAsync(3, () =>
        {
            callCount++;
            if (callCount < 2)
            {
                throw new Exception("Transient error");
            }
            return Task.CompletedTask;
        });

        callCount.Should().Be(2);
    }

    [Fact(DisplayName = "RDP-005: RetryAsync should throw on last attempt if all retries fail")]
    public async Task RDP005()
    {
        var mockRandom = new Mock<IRandom>();
        mockRandom.Setup(r => r.Next(It.IsAny<int>())).Returns(0);
        var policy = new RandomDelayRetryPolicy(mockRandom.Object);

        var act = () => policy.RetryAsync(3, () => throw new InvalidOperationException("Persistent error"));
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Persistent error");
    }

    [Fact(DisplayName = "RDP-006: RetryAsync with times 1 should throw immediately on failure")]
    public async Task RDP006()
    {
        var mockRandom = new Mock<IRandom>();
        var policy = new RandomDelayRetryPolicy(mockRandom.Object);

        var act = () => policy.RetryAsync(1, () => throw new InvalidOperationException("Failure"));
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Failure");
    }
}
