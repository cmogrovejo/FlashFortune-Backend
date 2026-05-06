namespace FlashFortune.Application.Interfaces;

public interface IEmailService
{
    Task SendInvitationAsync(string toEmail, string invitationToken, CancellationToken ct = default);
    Task SendPasswordRecoveryAsync(string toEmail, string recoveryToken, CancellationToken ct = default);
}
