using System.Net;
using System.Net.Mail;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Smtp;

/// <summary>
/// SMTP 邮件发送实现，使用 System.Net.Mail 标准库
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly string _host;
    private readonly int _port;
    private readonly string? _username;
    private readonly string? _password;
    private readonly bool _use_ssl;

    /// <summary>
    /// 初始化 SMTP 邮件发送服务
    /// </summary>
    /// <param name="host">SMTP 服务器地址</param>
    /// <param name="port">SMTP 端口，默认 587</param>
    /// <param name="username">认证用户名</param>
    /// <param name="password">认证密码</param>
    /// <param name="useSsl">是否使用 SSL，默认 true</param>
    public SmtpEmailSender(
        string host, int port = 587,
        string? username = null, string? password = null,
        bool useSsl = true)
    {
        _host = host;
        _port = port;
        _username = username;
        _password = password;
        _use_ssl = useSsl;
    }

    /// <inheritdoc />
    public async Task send(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var fromAddress = !string.IsNullOrEmpty(_username) ? _username : "noreply@atlas.local";

        using var message = new MailMessage(fromAddress, to, subject, body);
        using var client = new SmtpClient(_host, _port);

        if (_username is not null && _password is not null)
        {
            client.Credentials = new NetworkCredential(_username, _password);
        }

        client.EnableSsl = _use_ssl;

        await client.SendMailAsync(message, cancellationToken);
    }
}
