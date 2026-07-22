using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using Avalonia.Threading;
using Gml.Client;
using Gml.Client.Interfaces;
using Gml.Launcher.Core;
using Gml.Launcher.Core.Services;
using Gml.Launcher.Models;
using Gml.Launcher.ViewModels.Base;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Sentry;

namespace Gml.Launcher.ViewModels.Pages;

public class ProfilePageViewModel : PageViewModelBase
{
    private readonly IGmlClientManager _manager;

    [Reactive] public string TextureUrl { get; set; } = string.Empty;
    [Reactive] public ILauncherUser LauncherUser { get; set; }
    [Reactive] public ObservableCollection<ProfileInfoItem> ProfileInfoItems { get; set; } = new();
    [Reactive] public ObservableCollection<ProfileServerStatItem> ServerStats { get; set; } = new();
    [Reactive] public string CabinetUrl { get; set; } = string.Empty;
    [Reactive] public bool IsCabinetLoading { get; set; }
    [Reactive] public bool HasServerStats { get; set; }
    [Reactive] public bool ShowStatsPlaceholder { get; set; } = true;
    [Reactive] public string? CabinetMessage { get; set; }

    internal ProfilePageViewModel(
        IScreen screen,
        ILauncherUser launcherUser,
        IGmlClientManager manager,
        ILocalizationService? localizationService = null) : base(screen,
        localizationService)
    {
        LauncherUser = launcherUser ?? throw new ArgumentNullException(nameof(launcherUser));
        _manager = manager;

        RxApp.TaskpoolScheduler.Schedule(LoadData);
    }

    public new string Title => LocalizationService.GetString(SystemConstants.MainPageTitle);

    private async void LoadData()
    {
        try
        {
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss:fff}] Loading texture data...]");
            var userTextureInfo = await _manager.GetTexturesByName(LauncherUser.Name);

            if (userTextureInfo is not null)
            {
                var skinUrl = userTextureInfo.FullSkinUrl ?? string.Empty;
                await Dispatcher.UIThread.InvokeAsync(() => TextureUrl = skinUrl);
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss:fff}] Textures updated: {skinUrl}");
            }
        }
        catch (Exception exception)
        {
            SentrySdk.CaptureException(exception);
        }

        await LoadUnicoreCabinetAsync();
    }

    private async System.Threading.Tasks.Task LoadUnicoreCabinetAsync()
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsCabinetLoading = true;
                ShowStatsPlaceholder = false;
            });

            var response = await _manager.GetMyUnicoreCabinet(LauncherUser.AccessToken);
            var cabinet = response?.Data;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProfileInfoItems.Clear();
                ServerStats.Clear();
                CabinetMessage = null;
                CabinetUrl = string.Empty;
                HasServerStats = false;
                ShowStatsPlaceholder = false;

                if (cabinet is null || !cabinet.Available)
                {
                    CabinetMessage = cabinet?.Message;
                    ShowStatsPlaceholder = true;
                    return;
                }

                CabinetUrl = cabinet.CabinetUrl ?? string.Empty;
                CabinetMessage = cabinet.Message;

                ProfileInfoItems.Add(new ProfileInfoItem(
                    LocalizationService.GetString(SystemConstants.PlayedTime),
                    FormatPlaytime(cabinet.TotalPlaytime)));

                if (cabinet.Balance.HasValue)
                {
                    ProfileInfoItems.Add(new ProfileInfoItem(
                        LocalizationService.GetString(SystemConstants.Balance),
                        $"{cabinet.Balance.Value:0.##} руб."));
                }

                foreach (var server in cabinet.Servers ?? [])
                {
                    var groups = server.DonateGroups?.Select(g => g.Name).Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToArray() ?? [];
                    ServerStats.Add(new ProfileServerStatItem(
                        server.ServerName,
                        groups.Length > 0 ? string.Join(", ", groups) : "—",
                        FormatPlaytime(server.Playtime)));
                }

                HasServerStats = ServerStats.Count > 0;
                ShowStatsPlaceholder = !HasServerStats;
            });
        }
        catch (Exception exception)
        {
            SentrySdk.CaptureException(exception);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CabinetMessage = LocalizationService.GetString(SystemConstants.ProfileStatsLoadError);
                ShowStatsPlaceholder = true;
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsCabinetLoading = false);
        }
    }

    internal static string FormatPlaytime(long minutes)
    {
        if (minutes <= 0)
            return "0 мин";

        var hours = minutes / 60;
        var mins = minutes % 60;

        if (hours > 0 && mins > 0)
            return $"{hours} ч {mins} мин";
        if (hours > 0)
            return $"{hours} ч";
        return $"{mins} мин";
    }
}

public class ProfileServerStatItem(string serverName, string donateGroup, string playtime)
{
    public string ServerName { get; } = serverName;
    public string DonateGroup { get; } = donateGroup;
    public string Playtime { get; } = playtime;
}
