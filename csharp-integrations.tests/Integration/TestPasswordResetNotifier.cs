using csharp_integrations.api.Data;

namespace csharp_integrations.tests.Integration;

/// <summary>
/// Captures password reset notifications for integration tests.
/// </summary>
public sealed class TestPasswordResetNotifier : IPasswordResetNotifier
{
    /// <summary>
    /// Gets the most recent reset notification.
    /// </summary>
    public PasswordResetNotification? LastNotification { get; private set; }

    /// <inheritdoc />
    public Task SendAsync(PasswordResetNotification notification, CancellationToken cancellationToken = default)
    {
        LastNotification = notification;
        return Task.CompletedTask;
    }
}
