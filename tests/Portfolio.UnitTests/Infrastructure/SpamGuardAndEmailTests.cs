using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Contact;
using Portfolio.Infrastructure.Email;
using Portfolio.Infrastructure.Security;

namespace Portfolio.UnitTests.Infrastructure;

public class FormTimeTrapTests
{
    private readonly FakeTimeProvider _clock = new(TestServices.Now);
    private readonly DataProtectionFormTimeTrap _trap;

    public FormTimeTrapTests()
    {
        _trap = new DataProtectionFormTimeTrap(new EphemeralDataProtectionProvider(), _clock);
    }

    [Fact]
    public void ImmediateSubmission_IsTooFast()
    {
        var token = _trap.Issue();

        Assert.Equal(TimeTrapResult.TooFast, _trap.Check(token));
    }

    [Fact]
    public void HumanPacedSubmission_IsValid()
    {
        var token = _trap.Issue();
        _clock.Advance(TimeSpan.FromSeconds(20));

        Assert.Equal(TimeTrapResult.Valid, _trap.Check(token));
    }

    [Fact]
    public void OldForm_IsExpired()
    {
        var token = _trap.Issue();
        _clock.Advance(TimeSpan.FromHours(3));

        Assert.Equal(TimeTrapResult.Expired, _trap.Check(token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("638000000000000000")]
    [InlineData("kurcalanmis-jeton")]
    public void MissingOrForgedToken_IsInvalid(string? token)
    {
        Assert.Equal(TimeTrapResult.Invalid, _trap.Check(token));
    }
}

public class EmailDispatcherTests
{
    private static readonly EmailMessage Message = new("ben@example.com", "Konu", "Gövde");

    [Fact]
    public async Task RetriesTransientFailure_ThenSucceeds()
    {
        var transport = new FlakyTransport(failures: 1);

        var sent = await CreateDispatcher(transport).SendWithRetryAsync(Message, CancellationToken.None);

        Assert.True(sent);
        Assert.Equal(2, transport.Calls);
    }

    [Fact]
    public async Task GivesUpAfterMaxAttempts()
    {
        var transport = new FlakyTransport(failures: int.MaxValue);

        var sent = await CreateDispatcher(transport).SendWithRetryAsync(Message, CancellationToken.None);

        Assert.False(sent);
        Assert.Equal(3, transport.Calls);
    }

    [Fact]
    public async Task Outbox_HandsMessagesToReader()
    {
        var outbox = new ChannelEmailOutbox(NullLogger<ChannelEmailOutbox>.Instance);

        Assert.True(await outbox.EnqueueAsync(Message));
        Assert.True(outbox.Reader.TryRead(out var read));
        Assert.Equal(Message, read);
    }

    private static EmailDispatcher CreateDispatcher(IEmailTransport transport) => new(
        new ChannelEmailOutbox(NullLogger<ChannelEmailOutbox>.Instance),
        transport,
        Options.Create(new EmailOptions { MaxAttempts = 3, RetryDelay = TimeSpan.Zero }),
        TimeProvider.System,
        NullLogger<EmailDispatcher>.Instance);

    private sealed class FlakyTransport(int failures) : IEmailTransport
    {
        public int Calls { get; private set; }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Calls++;
            return Calls <= failures
                ? Task.FromException(new InvalidOperationException("SMTP geçici olarak kapalı"))
                : Task.CompletedTask;
        }
    }
}
