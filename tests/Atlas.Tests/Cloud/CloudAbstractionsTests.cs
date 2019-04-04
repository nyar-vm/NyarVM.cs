using Xunit;

namespace Atlas.Tests.Cloud;

/// <summary>
///     云服务新增抽象接口存在性测试
/// </summary>
public sealed class CloudAbstractionsTests
{
    [Fact]
    public void IEmailSender_HasSendAsync()
    {
        var method = typeof(IEmailSender).GetMethod("send");

        Assert.NotNull(method);
        Assert.Equal(4, method.GetParameters().Length);
    }

    [Fact]
    public void IChatCompletionService_HasCompleteAsync()
    {
        var method = typeof(IChatCompletionService).GetMethod("complete");

        Assert.NotNull(method);
        Assert.Equal(2, method.GetParameters().Length);
    }

    [Fact]
    public void ChatRequest_HasRequiredProperties()
    {
        var req = new ChatRequest
        {
            model = "gpt-4",
            messages = new List<ChatMessage>
            {
                new() { role = "user", content = "你好" }
            },
            temperature = 0.5,
            max_tokens = 512
        };

        Assert.Equal("gpt-4", req.model);
        Assert.Single(req.messages);
        Assert.Equal(0.5, req.temperature);
        Assert.Equal(512, req.max_tokens);
    }

    [Fact]
    public void ChatMessage_HasRoleAndContent()
    {
        var msg = new ChatMessage
        {
            role = "assistant",
            content = "你好，有什么可以帮助你的？"
        };

        Assert.Equal("assistant", msg.role);
        Assert.Equal("你好，有什么可以帮助你的？", msg.content);
    }

    [Fact]
    public void ChatResponse_HasAllProperties()
    {
        var resp = new ChatResponse
        {
            id = "chatcmpl-123",
            model = "gpt-4",
            message = new ChatMessage { role = "assistant", content = "回复内容" },
            finish_reason = "stop",
            usage_total_tokens = 42
        };

        Assert.Equal("chatcmpl-123", resp.id);
        Assert.Equal("gpt-4", resp.model);
        Assert.Equal("assistant", resp.message.role);
        Assert.Equal("stop", resp.finish_reason);
        Assert.Equal(42, resp.usage_total_tokens);
    }

    [Fact]
    public void IEmbeddingService_HasEmbedAsync()
    {
        var method = typeof(IEmbeddingService).GetMethod("embed");

        Assert.NotNull(method);
        Assert.Equal(2, method.GetParameters().Length);
    }

    [Fact]
    public void IImageGenerationService_HasGenerateAsync()
    {
        var method = typeof(IImageGenerationService).GetMethod("generate");

        Assert.NotNull(method);
        Assert.Equal(2, method.GetParameters().Length);
    }

    [Fact]
    public void IPushNotificationService_HasPushAsync()
    {
        var method = typeof(IPushNotificationService).GetMethod("push");

        Assert.NotNull(method);
        Assert.Equal(2, method.GetParameters().Length);
    }

    [Fact]
    public void ICdnService_HasPurgeAndPrefetch()
    {
        var purge = typeof(ICdnService).GetMethod("purge");
        var prefetch = typeof(ICdnService).GetMethod("prefetch");

        Assert.NotNull(purge);
        Assert.NotNull(prefetch);
    }

    [Fact]
    public void ISpeechService_HasTtsAndAsr()
    {
        var tts = typeof(ISpeechService).GetMethod("tts");
        var asr = typeof(ISpeechService).GetMethod("asr");

        Assert.NotNull(tts);
        Assert.NotNull(asr);
    }

    [Fact]
    public void ITranslationService_HasTranslateAsync()
    {
        var method = typeof(ITranslationService).GetMethod("translate");

        Assert.NotNull(method);
        Assert.Equal(2, method.GetParameters().Length);
    }

    [Fact]
    public void ISearchService_HasAllMethods()
    {
        var search = typeof(ISearchService).GetMethod("search");
        var index = typeof(ISearchService).GetMethod("index");
        var delete = typeof(ISearchService).GetMethod("delete_index");

        Assert.NotNull(search);
        Assert.NotNull(index);
        Assert.NotNull(delete);
    }
}

/// <summary>
///     云服务供应商实现存在性测试
/// </summary>
public sealed class CloudProviderTests
{
    [Theory]
    [InlineData(typeof(OpenAiChatCompletion), typeof(IChatCompletionService))]
    [InlineData(typeof(AzureOpenAiChatCompletion), typeof(IChatCompletionService))]
    [InlineData(typeof(DeepSeekChatCompletion), typeof(IChatCompletionService))]
    [InlineData(typeof(AnthropicChatCompletion), typeof(IChatCompletionService))]
    [InlineData(typeof(GoogleGeminiChatCompletion), typeof(IChatCompletionService))]
    [InlineData(typeof(TongyiChatCompletion), typeof(IChatCompletionService))]
    [InlineData(typeof(WenxinChatCompletion), typeof(IChatCompletionService))]
    [InlineData(typeof(ZhipuChatCompletion), typeof(IChatCompletionService))]
    [InlineData(typeof(MoonshotChatCompletion), typeof(IChatCompletionService))]
    [InlineData(typeof(MinimaxChatCompletion), typeof(IChatCompletionService))]
    [InlineData(typeof(IflytekChatCompletion), typeof(IChatCompletionService))]
    [InlineData(typeof(AlibabaBlobStorage), typeof(IBlobStorage))]
    [InlineData(typeof(TencentBlobStorage), typeof(IBlobStorage))]
    [InlineData(typeof(AwsBlobStorage), typeof(IBlobStorage))]
    [InlineData(typeof(AzureBlobStorage), typeof(IBlobStorage))]
    [InlineData(typeof(VolcengineTosStorage), typeof(IBlobStorage))]
    [InlineData(typeof(HuaweiObsStorage), typeof(IBlobStorage))]
    [InlineData(typeof(BaiduBosStorage), typeof(IBlobStorage))]
    [InlineData(typeof(JdOssStorage), typeof(IBlobStorage))]
    [InlineData(typeof(QiniuKodoStorage), typeof(IBlobStorage))]
    [InlineData(typeof(GoogleCloudStorage), typeof(IBlobStorage))]
    [InlineData(typeof(OracleObjectStorage), typeof(IBlobStorage))]
    [InlineData(typeof(AlibabaSmsProvider), typeof(ISmsProvider))]
    [InlineData(typeof(TencentSmsProvider), typeof(ISmsProvider))]
    [InlineData(typeof(VolcengineSmsProvider), typeof(ISmsProvider))]
    [InlineData(typeof(AwsSnsSmsProvider), typeof(ISmsProvider))]
    [InlineData(typeof(AzureSmsProvider), typeof(ISmsProvider))]
    [InlineData(typeof(HuaweiSmsProvider), typeof(ISmsProvider))]
    [InlineData(typeof(BaiduSmsProvider), typeof(ISmsProvider))]
    [InlineData(typeof(JdSmsProvider), typeof(ISmsProvider))]
    [InlineData(typeof(TwilioSmsProvider), typeof(ISmsProvider))]
    [InlineData(typeof(QiniuSmsProvider), typeof(ISmsProvider))]
    [InlineData(typeof(AlibabaKeyVaultService), typeof(IKeyVaultService))]
    [InlineData(typeof(AwsKeyVaultService), typeof(IKeyVaultService))]
    [InlineData(typeof(AzureKeyVaultService), typeof(IKeyVaultService))]
    [InlineData(typeof(TencentKeyVaultService), typeof(IKeyVaultService))]
    [InlineData(typeof(VolcengineKeyVaultService), typeof(IKeyVaultService))]
    [InlineData(typeof(HuaweiKeyVaultService), typeof(IKeyVaultService))]
    [InlineData(typeof(GoogleSecretManagerService), typeof(IKeyVaultService))]
    [InlineData(typeof(SmtpEmailSender), typeof(IEmailSender))]
    [InlineData(typeof(AwsSesEmailSender), typeof(IEmailSender))]
    [InlineData(typeof(AzureEmailSender), typeof(IEmailSender))]
    [InlineData(typeof(AlibabaEmailSender), typeof(IEmailSender))]
    [InlineData(typeof(TencentEmailSender), typeof(IEmailSender))]
    [InlineData(typeof(SendGridEmailSender), typeof(IEmailSender))]
    [InlineData(typeof(OpenAiEmbedding), typeof(IEmbeddingService))]
    [InlineData(typeof(AzureOpenAiEmbedding), typeof(IEmbeddingService))]
    [InlineData(typeof(DeepSeekEmbedding), typeof(IEmbeddingService))]
    [InlineData(typeof(TongyiEmbedding), typeof(IEmbeddingService))]
    [InlineData(typeof(WenxinEmbedding), typeof(IEmbeddingService))]
    [InlineData(typeof(ZhipuEmbedding), typeof(IEmbeddingService))]
    [InlineData(typeof(JinaEmbedding), typeof(IEmbeddingService))]
    [InlineData(typeof(VolcengineEmbedding), typeof(IEmbeddingService))]
    [InlineData(typeof(OpenAiImageGeneration), typeof(IImageGenerationService))]
    [InlineData(typeof(AzureOpenAiImageGeneration), typeof(IImageGenerationService))]
    [InlineData(typeof(TongyiImageGeneration), typeof(IImageGenerationService))]
    [InlineData(typeof(ZhipuImageGeneration), typeof(IImageGenerationService))]
    [InlineData(typeof(VolcengineImageGeneration), typeof(IImageGenerationService))]
    [InlineData(typeof(ApnsPushProvider), typeof(IPushNotificationService))]
    [InlineData(typeof(FcmPushProvider), typeof(IPushNotificationService))]
    [InlineData(typeof(HuaweiPushProvider), typeof(IPushNotificationService))]
    [InlineData(typeof(XiaomiPushProvider), typeof(IPushNotificationService))]
    [InlineData(typeof(VolcenginePushProvider), typeof(IPushNotificationService))]
    [InlineData(typeof(AwsCloudFrontCdn), typeof(ICdnService))]
    [InlineData(typeof(AzureCdn), typeof(ICdnService))]
    [InlineData(typeof(AlibabaCdn), typeof(ICdnService))]
    [InlineData(typeof(TencentCdn), typeof(ICdnService))]
    [InlineData(typeof(VolcengineCdn), typeof(ICdnService))]
    [InlineData(typeof(AlibabaSpeechService), typeof(ISpeechService))]
    [InlineData(typeof(TencentSpeechService), typeof(ISpeechService))]
    [InlineData(typeof(IflytekSpeechService), typeof(ISpeechService))]
    [InlineData(typeof(AzureSpeechService), typeof(ISpeechService))]
    [InlineData(typeof(VolcengineSpeechService), typeof(ISpeechService))]
    [InlineData(typeof(AwsTranslationService), typeof(ITranslationService))]
    [InlineData(typeof(AzureTranslationService), typeof(ITranslationService))]
    [InlineData(typeof(AlibabaTranslationService), typeof(ITranslationService))]
    [InlineData(typeof(TencentTranslationService), typeof(ITranslationService))]
    [InlineData(typeof(BaiduTranslationService), typeof(ITranslationService))]
    [InlineData(typeof(ElasticsearchService), typeof(ISearchService))]
    [InlineData(typeof(AlibabaSearchService), typeof(ISearchService))]
    [InlineData(typeof(TencentSearchService), typeof(ISearchService))]
    [InlineData(typeof(MeilisearchService), typeof(ISearchService))]
    public void Implementation_ImplementsInterface(Type implType, Type interfaceType)
    {
        Assert.True(interfaceType.IsAssignableFrom(implType),
            $"{implType.Name} should implement {interfaceType.Name}");
    }
}