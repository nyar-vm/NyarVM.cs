namespace Std.Data.Text.Syntax;

public interface IStorage
{
    ITextProvider get_text_provider(TextProviderId id, int version = -1);
    ITextProvider apply_edit(TextProviderId id, IEdit edit);
}