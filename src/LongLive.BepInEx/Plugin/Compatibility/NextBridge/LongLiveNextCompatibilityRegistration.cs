using System;
using LongLive.Next.Abstractions.Commands;
using LongLive.Next.Abstractions.Queries;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveNextCompatibilityRegistration
{
    public static void RegisterCommandAlias(
        LongLiveCompatibilityRuntime compatibilityRuntime,
        INextCommandRegistry commandRegistry,
        string redirectId,
        string alias,
        NextCommandHandler handler,
        Func<bool>? isEnabled = null)
    {
        if (compatibilityRuntime is null)
        {
            throw new ArgumentNullException(nameof(compatibilityRuntime));
        }

        if (commandRegistry is null)
        {
            throw new ArgumentNullException(nameof(commandRegistry));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        commandRegistry.Register(alias, (context, complete) =>
        {
            if (isEnabled is not null && !isEnabled())
            {
                compatibilityRuntime.RecordInvocation(redirectId, "command-skipped-disabled", $"alias={alias}");
                complete();
                return;
            }

            try
            {
                handler(context, complete);
                compatibilityRuntime.RecordInvocation(redirectId, "command-ok", $"alias={alias}");
            }
            catch (Exception ex)
            {
                compatibilityRuntime.RecordInvocation(redirectId, "command-error", $"alias={alias}, error={ex.GetType().Name}");
                throw;
            }
        });
    }

    public static void RegisterQueryAlias(
        LongLiveCompatibilityRuntime compatibilityRuntime,
        INextQueryRegistry queryRegistry,
        string redirectId,
        string alias,
        NextQueryHandler handler,
        Func<bool>? isEnabled = null,
        Func<object?>? disabledResultFactory = null)
    {
        if (compatibilityRuntime is null)
        {
            throw new ArgumentNullException(nameof(compatibilityRuntime));
        }

        if (queryRegistry is null)
        {
            throw new ArgumentNullException(nameof(queryRegistry));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        queryRegistry.Register(alias, context =>
        {
            if (isEnabled is not null && !isEnabled())
            {
                compatibilityRuntime.RecordInvocation(redirectId, "query-skipped-disabled", $"alias={alias}");
                return disabledResultFactory is null ? null : disabledResultFactory();
            }

            try
            {
                var value = handler(context);
                compatibilityRuntime.RecordInvocation(redirectId, "query-ok", $"alias={alias}");
                return value;
            }
            catch (Exception ex)
            {
                compatibilityRuntime.RecordInvocation(redirectId, "query-error", $"alias={alias}, error={ex.GetType().Name}");
                throw;
            }
        });
    }
}
