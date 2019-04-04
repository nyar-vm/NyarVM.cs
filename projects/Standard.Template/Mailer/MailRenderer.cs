using System.Text.RegularExpressions;

namespace Std.Template.Mailer;

#region 数据模型

/// <summary>
///     邮件模板
/// </summary>
public class EmailTemplate
{
    public string Name { get; set; } = "";
    public string Subject { get; set; } = "";
    public string HtmlBody { get; set; } = "";
    public string TextBody { get; set; } = "";
    public string From { get; set; } = "";
    public string ReplyTo { get; set; } = "";
    public List<string> Cc { get; set; } = [];
    public List<string> Bcc { get; set; } = [];
    public Dictionary<string, string> Headers { get; set; } = new();
}

/// <summary>
///     邮件收件人
/// </summary>
public class EmailRecipient
{
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public Dictionary<string, object> Variables { get; set; } = new();
}

/// <summary>
///     邮件发送结果
/// </summary>
public class EmailResult
{
    public string Recipient { get; init; } = "";
    public string Subject { get; init; } = "";
    public string HtmlBody { get; init; } = "";
    public string TextBody { get; init; } = "";
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
///     批量发送结果
/// </summary>
public class BatchEmailResult
{
    public int TotalCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<EmailResult> Results { get; init; } = [];
}

#endregion

/// <summary>
///     邮件模板渲染器
/// </summary>
public sealed class MailRenderer
{
    private readonly DejaVuRenderer _renderer;
    private readonly string _templateDir = "";
    private readonly TemplateManager _templateManager;

    public MailRenderer(string templateDir)
    {
        _templateDir = Path.GetFullPath(templateDir);
        var loader = new FileSystemTemplateLoader(_templateDir);
        _templateManager = new TemplateManager(loader);
        _renderer = new DejaVuRenderer(DejaVuLanguage.dora, _templateManager);
    }

    /// <summary>
    ///     加载邮件模板
    /// </summary>
    public EmailTemplate LoadTemplate(string templateName)
    {
        var templateDir = Path.Combine(_templateDir, templateName);
        var template = new EmailTemplate { Name = templateName };

        var subjectFile = Path.Combine(templateDir, "subject.dora");
        if (File.Exists(subjectFile)) template.Subject = File.ReadAllText(subjectFile).Trim();

        var htmlFile = Path.Combine(templateDir, "html.dora");
        if (File.Exists(htmlFile)) template.HtmlBody = File.ReadAllText(htmlFile);

        var textFile = Path.Combine(templateDir, "text.dora");
        if (File.Exists(textFile)) template.TextBody = File.ReadAllText(textFile);

        var configFile = Path.Combine(templateDir, "config.yaml");
        if (File.Exists(configFile))
        {
            var content = File.ReadAllText(configFile);
            var parser = new YamlParser();
            var result = parser.parse(content);
            if (result.success && result.value is YamlMapping mapping)
            {
                template.From = DataConvert.GetYamlString(mapping, "from", template.From);
                template.ReplyTo = DataConvert.GetYamlString(mapping, "replyTo", template.ReplyTo);

                if (mapping.try_get_value("cc", out var ccValue))
                    template.Cc = ccValue is YamlSequence ccSeq
                        ? ccSeq.items.OfType<YamlString>().Select(s => s.value).ToList()
                        : DataConvert.GetYamlString(mapping, "cc").Split(',').Select(s => s.Trim()).ToList();

                if (mapping.try_get_value("bcc", out var bccValue))
                    template.Bcc = bccValue is YamlSequence bccSeq
                        ? bccSeq.items.OfType<YamlString>().Select(s => s.value).ToList()
                        : DataConvert.GetYamlString(mapping, "bcc").Split(',').Select(s => s.Trim()).ToList();
            }
        }

        return template;
    }

    /// <summary>
    ///     渲染邮件
    /// </summary>
    public EmailResult Render(EmailTemplate template, EmailRecipient recipient)
    {
        try
        {
            var context = BuildContext(recipient);

            var subject = _renderer.Render(template.Subject, context);
            var htmlBody = _renderer.Render(template.HtmlBody, context);
            var textBody = !string.IsNullOrEmpty(template.TextBody)
                ? _renderer.Render(template.TextBody, context)
                : StripHtml(htmlBody);

            return new EmailResult
            {
                Recipient = recipient.Email,
                Subject = subject,
                HtmlBody = htmlBody,
                TextBody = textBody,
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new EmailResult
            {
                Recipient = recipient.Email,
                Subject = template.Subject,
                HtmlBody = "",
                TextBody = "",
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    ///     批量渲染邮件
    /// </summary>
    public BatchEmailResult RenderBatch(EmailTemplate template, IEnumerable<EmailRecipient> recipients)
    {
        var results = new List<EmailResult>();

        foreach (var recipient in recipients) results.Add(Render(template, recipient));

        return new BatchEmailResult
        {
            TotalCount = results.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            Results = results
        };
    }

    /// <summary>
    ///     渲染邮件并保存到文件
    /// </summary>
    public void RenderToFile(EmailTemplate template, EmailRecipient recipient, string outputDir)
    {
        var result = Render(template, recipient);
        Directory.CreateDirectory(outputDir);

        var fileName = $"{recipient.Email.Replace("@", "_at_")}_{DateTime.Now:yyyyMMdd_HHmmss}";
        File.WriteAllText(Path.Combine(outputDir, $"{fileName}.subject.txt"), result.Subject);
        File.WriteAllText(Path.Combine(outputDir, $"{fileName}.html"), result.HtmlBody);

        if (!string.IsNullOrEmpty(result.TextBody))
            File.WriteAllText(Path.Combine(outputDir, $"{fileName}.txt"), result.TextBody);
    }

    /// <summary>
    ///     批量渲染邮件并保存到文件
    /// </summary>
    public BatchEmailResult RenderBatchToFiles(EmailTemplate template, IEnumerable<EmailRecipient> recipients,
        string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var results = new List<EmailResult>();

        foreach (var recipient in recipients)
        {
            var result = Render(template, recipient);
            results.Add(result);

            if (result.Success)
            {
                var fileName = $"{recipient.Email.Replace("@", "_at_")}_{DateTime.Now:yyyyMMdd_HHmmss}";
                File.WriteAllText(Path.Combine(outputDir, $"{fileName}.subject.txt"), result.Subject);
                File.WriteAllText(Path.Combine(outputDir, $"{fileName}.html"), result.HtmlBody);

                if (!string.IsNullOrEmpty(result.TextBody))
                    File.WriteAllText(Path.Combine(outputDir, $"{fileName}.txt"), result.TextBody);
            }
        }

        return new BatchEmailResult
        {
            TotalCount = results.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            Results = results
        };
    }

    #region 上下文

    private Dictionary<string, object> BuildContext(EmailRecipient recipient)
    {
        return new Dictionary<string, object>
        {
            ["recipient"] = new Dictionary<string, object>
            {
                ["email"] = recipient.Email,
                ["name"] = recipient.Name
            },
            ["variables"] = recipient.Variables
        };
    }

    private static string StripHtml(string html)
    {
        var text = Regex.Replace(html, @"<br\s*/?>", "\n");
        text = Regex.Replace(text, @"</p>", "\n\n");
        text = Regex.Replace(text, @"<[^>]+>", "");
        text = Regex.Replace(text, @"&nbsp;", " ");
        text = Regex.Replace(text, @"&amp;", "&");
        text = Regex.Replace(text, @"&lt;", "<");
        text = Regex.Replace(text, @"&gt;", ">");
        return text.Trim();
    }

    #endregion
}