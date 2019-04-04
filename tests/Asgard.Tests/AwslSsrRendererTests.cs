using Xunit;

namespace VOA.ToolChain.Tests;

public sealed class AwslSsrRendererTests
{
    private readonly AwslSsrRenderer _renderer = new();

    [Fact]
    public void RenderSsr_SimpleComponent_GeneratesHtml()
    {
        var source = @"<widget>
    <div>Hello World</div>
</widget>";

        var result = _renderer.render_ssr(source, "");

        Assert.Equal("hello-world", result.scope);
        Assert.Contains("data-voa-ssr=\"HelloWorld\"", result.html);
        Assert.Contains("Hello World", result.html);
    }

    [Fact]
    public void RenderSsr_component_name_KebabCase()
    {
        var source = @"<widget name=""MyComponent"">
    <div>test</div>
</widget>";

        var result = _renderer.render_ssr(source, "");

        Assert.Equal("voa-ssr-my-component", result.scope);
    }

    [Fact]
    public void RenderSsr_InitialState_JsonSerialized()
    {
        var source = @"<widget>
    <script>
        let count: i32 = 0
    </script>
    <div>{count}</div>
</widget>";

        var result = _renderer.render_ssr(source, "");

        Assert.Contains("data-voa-state=", result.html);
        Assert.Contains("count", result.html);
    }

    [Fact]
    public void RenderSsr_HeadTag_ExtractedToHeadTags()
    {
        var source = @"<widget>
    <Head>
        <link rel=""stylesheet"" href=""/theme.css"">
        <meta name=""description"" content=""My page"">
    </Head>
    <div>Content</div>
</widget>";

        var result = _renderer.render_ssr(source, "");

        Assert.Single(result.head_tags);
        Assert.Contains("link", result.head_tags[0]);
        Assert.Contains("meta", result.head_tags[0]);
        Assert.DoesNotContain("<Head>", result.html);
    }

    [Fact]
    public void RenderSsr_ScriptTag_ExtractedToScriptTags()
    {
        var source = @"<widget>
    <Script src=""/analytics.js""></Script>
    <div>Content</div>
</widget>";

        var result = _renderer.render_ssr(source, "");

        Assert.Single(result.script_tags);
        Assert.Contains("analytics.js", result.script_tags[0]);
        Assert.DoesNotContain("<Script", result.html);
    }

    [Fact]
    public void RenderSsr_InlineScriptTag_ExtractedToScriptTags()
    {
        var source = @"<widget>
    <Script>
        console.log('hello')
    </Script>
    <div>Content</div>
</widget>";

        var result = _renderer.render_ssr(source, "");

        Assert.Single(result.script_tags);
        Assert.Contains("console.log", result.script_tags[0]);
    }

    [Fact]
    public void RenderSsr_Suspense_IsSuspenseTrue()
    {
        var source = @"<widget>
    <Suspense>
        <Fallback>
            <div>Loading...</div>
        </Fallback>
        <div>Content</div>
    </Suspense>
</widget>";

        var result = _renderer.render_ssr(source, "");

        Assert.True(result.is_suspense);
        Assert.NotNull(result.fallback_html);
        Assert.Contains("Loading...", result.fallback_html);
    }

    [Fact]
    public void RenderSsr_Suspense_DefaultFallback()
    {
        var source = @"<widget>
    <Suspense>
        <div>Content</div>
    </Suspense>
</widget>";

        var result = _renderer.render_ssr(source, "");

        Assert.True(result.is_suspense);
        Assert.Contains("Loading...", result.fallback_html);
    }

    [Fact]
    public void RenderSsr_ConditionalTruthy_EvaluatesCondition()
    {
        var source = @"<widget>
    <script>
        let visible: bool = true
    </script>
    <if condition={visible}>
        <div>Shown</div>
    </if>
</widget>";

        var result = _renderer.render_ssr(source, "");

        Assert.Contains("Shown", result.html);
        Assert.DoesNotContain("data-voa-state", result.html);
    }

    [Fact]
    public void RenderSsr_ConditionalFalsy_ShowsElseBranch()
    {
        var source = @"<widget>
    <script>
        let visible: bool = false
    </script>
    <if condition={visible}>
        <div>Shown</div>
    <else/>
        <div>Hidden</div>
    </if>
</widget>";

        var result = _renderer.render_ssr(source, "");

        Assert.Contains("Hidden", result.html);
        Assert.DoesNotContain("Shown", result.html);
    }

    [Fact]
    public void RenderSsr_ForLoop_IteratesItems()
    {
        var source = @"<widget>
    <script>
        let items: list = [""a"", ""b"", ""c""]
    </script>
    <loop item in {items}>
        <span>{item}</span>
    </loop>
</widget>";

        var result = _renderer.render_ssr(source, "");

        Assert.Contains("a", result.html);
        Assert.Contains("b", result.html);
        Assert.Contains("c", result.html);
    }

    [Fact]
    public void RenderSsr_Interpolation_escapesHtml()
    {
        var source = @"<widget>
    <script>
        let msg: string = ""<script>alert('xss')</script>""
    </script>
    <div>{msg}</div>
</widget>";

        var result = _renderer.render_ssr(source, "");

        Assert.DoesNotContain("<script>", result.html);
        Assert.Contains("&lt;", result.html);
    }

    [Fact]
    public void RenderSsr_EventBinding_NotRendered()
    {
        var source = @"<widget>
    <button on:click={handleClick}>Click</button>
</widget>";

        var result = _renderer.render_ssr(source, "");

        Assert.Contains("<button", result.html);
        Assert.DoesNotContain("addEventListener", result.html);
    }

    [Fact]
    public void GenerateSsrPage_IncludesHeadTags()
    {
        var result = new AwslSsrRenderResult
        {
            html = "<div>Test</div>",
            component_name = "Test",
            scope = "voa-ssr-test",
            initial_state_json = "{}",
            head_tags = new List<string> { "<link rel=\"canonical\" href=\"/test\">" }
        };

        var page = AwslSsrRenderer.generate_ssr_page("test", new[] { result }, null);

        Assert.Contains("<link rel=\"canonical\" href=\"/test\">", page);
    }

    [Fact]
    public void GenerateSsrPage_IncludesScriptTags()
    {
        var result = new AwslSsrRenderResult
        {
            html = "<div>Test</div>",
            component_name = "Test",
            scope = "voa-ssr-test",
            initial_state_json = "{}",
            script_tags = new List<string> { "<script src=\"/analytics.js\"></script>" }
        };

        var page = AwslSsrRenderer.generate_ssr_page("test", new[] { result }, null);

        Assert.Contains("<script src=\"/analytics.js\"></script>", page);
    }

    [Fact]
    public void GenerateSsrPage_Suspense_IncludesWrapper()
    {
        var result = new AwslSsrRenderResult
        {
            html = "<div>Async content</div>",
            component_name = "AsyncPage",
            scope = "voa-ssr-async-page",
            initial_state_json = "{}",
            is_suspense = true,
            fallback_html = "<div>Loading...</div>"
        };

        var page = AwslSsrRenderer.generate_ssr_page("async-page", new[] { result }, null);

        Assert.Contains("voa-suspense-wrapper", page);
        Assert.Contains("data-suspense=\"loading\"", page);
        Assert.Contains("MutationObserver", page);
    }

    [Fact]
    public void GenerateHydrationScript_HydratesAllComponents()
    {
        var result = new AwslSsrRenderResult
        {
            html = "<div>Test</div>",
            component_name = "Test",
            scope = "voa-ssr-test",
            initial_state_json = "{\"count\": 0}"
        };

        var script = AwslSsrRenderer.generate_hydration_script(new[] { result }, "test", null);

        Assert.Contains("hydrateComponent", script);
        Assert.Contains("data-voa-ssr", script);
        Assert.Contains("data-voa-state", script);
    }
}