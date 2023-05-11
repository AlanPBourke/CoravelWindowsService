using Coravel.Invocable;
using NLog;

namespace CoravelWindowsService.Invocables;

public class InvocableJob : IInvocable
{
    public static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public virtual Task Invoke()
    {
        throw new NotImplementedException();
    }
}
