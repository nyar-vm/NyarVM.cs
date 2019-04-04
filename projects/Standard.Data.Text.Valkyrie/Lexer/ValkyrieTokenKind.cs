namespace Std.Data.Text.Valkyrie.Lexer;

/// <summary>
///     Valkyrie 词法节点类型
/// </summary>
public enum ValkyrieTokenKind : short
{
    #region Basics

    /// <summary>未知类型。</summary>
    error = -1,

    /// <summary>结束流，终止符。</summary>
    eos = 0,

    /// <summary>#。</summary>
    comment_start = 1,

    /// <summary>CommentContent。</summary>
    comment_content = 2,

    #endregion

    #region Keywords

    /// <summary>namespace。</summary>
    @namespace = 27,

    /// <summary>using。</summary>
    @using = 28,

    /// <summary>let。</summary>
    let = 10,

    /// <summary>micro。</summary>
    micro = 11,

    /// <summary>mezzo。</summary>
    mezzo = 12,

    /// <summary>macro。</summary>
    macro = 13,

    /// <summary>if。</summary>
    @if = 19,

    /// <summary>loop。</summary>
    loop = 21,

    /// <summary>while。</summary>
    @while = 22,

    /// <summary>until。</summary>
    until = 23,

    /// <summary>structure。</summary>
    structure = 123,

    /// <summary>class。</summary>
    @class = 29,

    /// <summary>enums。</summary>
    enums = 30,

    /// <summary>flags。</summary>
    flags = 31,

    /// <summary>union。</summary>
    union = 32,

    /// <summary>unite。</summary>
    unite = 39,

    /// <summary>trait。</summary>
    trait = 48,

    /// <summary>match。</summary>
    match = 24,

    /// <summary>catch。</summary>
    @catch = 38,

    /// <summary>case。</summary>
    @case = 25,

    /// <summary>type。</summary>
    type = 40,

    /// <summary>when。</summary>
    when = 66,

    /// <summary>else。</summary>
    @else = 20,

    /// <summary>end。</summary>
    end = 26,

    /// <summary>break。</summary>
    @break = 35,

    /// <summary>continue。</summary>
    @continue = 36,

    /// <summary>raise。</summary>
    raise = 14,

    /// <summary>yield。</summary>
    yield = 15,

    /// <summary>try。</summary>
    @try = 16,

    /// <summary>unsafe 修饰符。</summary>
    @unsafe = 17,

    /// <summary>return。</summary>
    @return = 18,

    /// <summary>resume。</summary>
    resume = 37,

    /// <summary>in。</summary>
    @in = 34,

    /// <summary>is。</summary>
    @is = 67,

    /// <summary>as。</summary>
    @as = 68,

    /// <summary>where。</summary>
    where = 41,

    /// <summary>imply。</summary>
    imply = 42,

    #endregion

    #region Literals

    /// <summary>null。</summary>
    @null = 82,

    /// <summary>true。</summary>
    @true = 80,

    /// <summary>false。</summary>
    @false = 81,

    /// <summary>标识符。</summary>
    identifier = 84,

    /// <summary>数字。</summary>
    number = 85,

    /// <summary>字符串。</summary>
    @string = 86,

    #endregion

    #region ECS Extension

    /// <summary>component。</summary>
    component = 1001,

    /// <summary>system。</summary>
    system = 1002,

    #endregion

    #region Widget Extension

    /// <summary>widget。</summary>
    widget = 1401,

    #endregion

    #region Schema Extension

    /// <summary>model。</summary>
    model = 1201,

    /// <summary>service。</summary>
    service = 1202,

    /// <summary>model。</summary>
    message = 1203,

    #endregion

    #region Shader Extension

    /// <summary>shader。</summary>
    shader = 1501,

    /// <summary>vertex。</summary>
    vertex = 52,

    /// <summary>fragment。</summary>
    fragment = 53,

    /// <summary>compute。</summary>
    compute = 54,

    /// <summary>uniform。</summary>
    uniform = 55,

    /// <summary>varying。</summary>
    varying = 56,

    /// <summary>cbuffer。</summary>
    c_buffer = 57,

    /// <summary>texture。</summary>
    texture = 58,

    /// <summary>sampler。</summary>
    sampler = 59,

    /// <summary>discard。</summary>
    discard = 60,

    /// <summary>raygen。</summary>
    raygen = 61,

    /// <summary>closesthit。</summary>
    closesthit = 62,

    /// <summary>anyhit。</summary>
    anyhit = 63,

    /// <summary>miss。</summary>
    miss = 64,

    /// <summary>constant。</summary>
    constant = 65,

    /// <summary>binding。</summary>
    binding = 66,

    #endregion

    #region Neural Extension

    /// <summary>neural。</summary>
    neural = 49,

    #endregion

    #region Operators

    /// <summary>+。</summary>
    plus = 100,

    /// <summary>-。</summary>
    minus = 101,

    /// <summary>*。</summary>
    star = 102,

    /// <summary>/。</summary>
    slash = 103,

    /// <summary>%。</summary>
    percent = 104,

    /// <summary>=。</summary>
    equal = 105,

    /// <summary>+=。</summary>
    plus_equal = 106,

    /// <summary>-=。</summary>
    minus_equal = 107,

    /// <summary>*=。</summary>
    star_equal = 108,

    /// <summary>/=。</summary>
    slash_equal = 109,

    /// <summary>%=。</summary>
    percent_equal = 110,

    /// <summary>==。</summary>
    equal_equal = 111,

    /// <summary>!=。</summary>
    bang_equal = 112,

    /// <summary>&lt;=。</summary>
    less_equal = 115,

    /// <summary>&gt;=。</summary>
    greater_equal = 116,

    /// <summary>&amp;&amp;。</summary>
    amp_amp = 117,

    /// <summary>||。</summary>
    pipe_pipe = 118,

    /// <summary>!。</summary>
    bang = 119,

    /// <summary>&amp;。</summary>
    amp = 120,

    /// <summary>|。</summary>
    pipe = 121,

    /// <summary>^。</summary>
    power = 122,

    /// <summary>~。</summary>
    tilde = 123,

    /// <summary>&amp;=。</summary>
    amp_equal = 124,

    /// <summary>|=。</summary>
    pipe_equal = 125,

    /// <summary>^=。</summary>
    caret_equal = 126,

    /// <summary>&lt;&lt;。</summary>
    less_less = 127,

    /// <summary>&gt;&gt;。</summary>
    greater_greater = 128,

    /// <summary>&lt;&lt;=。</summary>
    less_less_equal = 129,

    /// <summary>&gt;&gt;=。</summary>
    greater_greater_equal = 130,

    /// <summary>-&gt;。</summary>
    arrow = 131,

    /// <summary>=&gt;。</summary>
    fat_arrow = 132,

    /// <summary>??。</summary>
    question_question = 133,

    /// <summary>++。</summary>
    plus_plus = 134,

    /// <summary>--。</summary>
    minus_minus = 135,

    /// <summary>?。</summary>
    question = 136,

    /// <summary>.</summary>
    dot = 137,

    /// <summary>..</summary>
    dot_dot = 138,

    /// <summary>...</summary>
    dot_dot_dot = 139,

    /// <summary>..= 闭区间范围模式</summary>
    dot_dot_equal = 140,

    #endregion

    #region Punctuations

    /// <summary>:。</summary>
    colon = 200,

    /// <summary>::。</summary>
    double_colon = 201,

    /// <summary>,。</summary>
    comma = 202,

    /// <summary>;。</summary>
    semicolon = 203,

    #endregion

    #region Delimiters

    /// <summary>&lt;#。</summary>
    comment_l = 401,

    /// <summary>#&gt;。</summary>
    comment_r = 402,

    /// <summary>(。</summary>
    parenthesis_l = 431,

    /// <summary>)。</summary>
    parenthesis_r = 432,

    /// <summary>[。</summary>
    bracket_l = 403,

    /// <summary>]。</summary>
    bracket_r = 404,

    /// <summary>⁅。</summary>
    offset_l = 405,

    /// <summary>⁆。</summary>
    offset_r = 406,

    /// <summary>{。</summary>
    brace_l = 207,

    /// <summary>}。</summary>
    brace_r = 208,

    /// <summary>&lt;。</summary>
    less = 209,

    /// <summary>&gt;。</summary>
    greater = 210,

    /// <summary>⟨。</summary>
    generic_l = 211,

    /// <summary>⟩。</summary>
    generic_r = 212,

    /// <summary>&lt;%。</summary>
    template_l = 213,

    /// <summary>%&gt;。</summary>
    template_r = 214,

    #endregion
}