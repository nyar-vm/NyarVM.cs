namespace Nyar.Analyzer.Check;

public interface ICheckerProvider
{
    IChecker create_checker();
}