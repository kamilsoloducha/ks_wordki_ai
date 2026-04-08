using Serilog;
using Wordki.Bff.SharedKernel.Abstractions;

namespace Wordki.Bff.SharedKernel.Services;

public sealed class ConsoleEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        Log.Warning("""
                    [EmailSender] Sending email:
                      To: {0}
                      Subject: {1}
                      Body: {2}
                    """,
            to,
            subject,
            body);

        return Task.CompletedTask;
    }
}