namespace Atlas.Cloud.Interfaces;

/// <summary>
/// 邮件发送服务抽象接口，统一各类邮件服务商的操作
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// 发送邮件
    /// </summary>
    /// <param name="to">收件人地址</param>
    /// <param name="subject">邮件主题</param>
    /// <param name="body">邮件正文</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task send(string to, string subject, string body, CancellationToken cancellationToken = default);
}