using Avalonia;
using Avalonia.Styling;
using Markstash.Domain.Preferences;

namespace Markstash.App.Services;

internal sealed class AvaloniaThemeService : IThemeService
{
    public ThemePreference Current { get; private set; } = ThemePreference.System;

    public void Apply(ThemePreference preference)
    {
        Current = preference;

        if (Avalonia.Application.Current is null)
        {
            return;
        }

        Avalonia.Application.Current.RequestedThemeVariant = preference switch
        {
            ThemePreference.Light => ThemeVariant.Light,
            ThemePreference.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}
