using Markstash.Application.Abstractions;
using Markstash.Domain.Preferences;
using Microsoft.Extensions.Logging;

namespace Markstash.Application.Preferences;

internal sealed partial class PreferencesService : IPreferencesService
{
    private readonly object _gate = new();
    private readonly IUserPreferencesStore _store;
    private readonly ILogger<PreferencesService> _logger;

    public PreferencesService(
        IUserPreferencesStore store,
        ILogger<PreferencesService> logger)
    {
        _store = store;
        _logger = logger;

        var result = store.Load();
        Current = result.Preferences;
        LoadStatus = result.Status;
        LastPersistenceError = result.Message;
        IsWritable = result.IsWritable;

        if (result.Status is not PreferencesLoadStatus.Loaded and not PreferencesLoadStatus.Default)
        {
            LogNonStandardLoad(_logger, result.Status, result.Message);
        }
    }

    public UserPreferences Current { get; private set; }

    public PreferencesLoadStatus LoadStatus { get; }

    public string? LastPersistenceError { get; private set; }

    public bool IsWritable { get; }

    public event EventHandler<UserPreferences>? Changed;

    public event EventHandler<string>? PersistenceFailed;

    public bool SetTheme(ThemePreference theme)
    {
        if (!Enum.IsDefined(theme))
        {
            throw new ArgumentOutOfRangeException(nameof(theme), theme, "Unknown theme preference.");
        }

        UserPreferences? changedPreferences = null;
        string? failureMessage = null;
        lock (_gate)
        {
            if (Current.Theme == theme)
            {
                return true;
            }

            if (!IsWritable)
            {
                failureMessage = LastPersistenceError ??
                    "Preferences are read-only because their format is not supported.";
            }
            else
            {
                var updated = Current with { Theme = theme };
                try
                {
                    _store.Save(updated);
                    Current = updated;
                    LastPersistenceError = null;
                    changedPreferences = Current;
                }
                catch (Exception exception)
                {
                    LastPersistenceError = exception.Message;
                    failureMessage = exception.Message;
                    LogPersistenceFailure(_logger, exception);
                }
            }
        }

        if (changedPreferences is not null)
        {
            NotifyChanged(changedPreferences);
            return true;
        }

        if (failureMessage is not null)
        {
            NotifyPersistenceFailed(failureMessage);
        }

        return false;
    }

    private void NotifyChanged(UserPreferences preferences)
    {
        foreach (var handler in Changed?.GetInvocationList()
                     .OfType<EventHandler<UserPreferences>>()
                     .ToArray() ?? [])
        {
            try
            {
                handler(this, preferences);
            }
            catch (Exception exception)
            {
                TryLogPreferenceObserverFailure(exception, nameof(Changed));
            }
        }
    }

    private void NotifyPersistenceFailed(string message)
    {
        foreach (var handler in PersistenceFailed?.GetInvocationList()
                     .OfType<EventHandler<string>>()
                     .ToArray() ?? [])
        {
            try
            {
                handler(this, message);
            }
            catch (Exception exception)
            {
                TryLogPreferenceObserverFailure(exception, nameof(PersistenceFailed));
            }
        }
    }

    private void TryLogPreferenceObserverFailure(Exception exception, string eventName)
    {
        try
        {
            LogPreferenceObserverFailure(_logger, exception, eventName);
        }
        catch
        {
            System.Diagnostics.Debug.WriteLine(
                $"Unable to notify preferences observer for {eventName}: {exception}");
        }
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Preferences loaded with status {LoadStatus}: {Message}")]
    private static partial void LogNonStandardLoad(
        ILogger logger,
        PreferencesLoadStatus loadStatus,
        string? message);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Unable to persist user preferences.")]
    private static partial void LogPersistenceFailure(
        ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "A preferences observer failed while raising {EventName}.")]
    private static partial void LogPreferenceObserverFailure(
        ILogger logger,
        Exception exception,
        string eventName);
}
