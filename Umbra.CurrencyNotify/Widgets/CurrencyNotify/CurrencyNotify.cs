using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Umbra.Common;
using Umbra.Game;
using Umbra.Widgets;
using Una.Drawing;

using FFXIVClientStructs.FFXIV.Client.UI;

namespace Umbra.CurrencyNotify.Widgets;

[ToolbarWidget(
    "CurrencyNotify",
    "Currency Notify",
    "Provides a \"notification\" when you exceed a user-defined limit for user-selected currencies.",
    ["currency", "gil", "tomestone", "seals", "company", "rewards"]
)]
public sealed partial class CurrencyNotifyWidget(
    WidgetInfo                  info,
    string?                     guid         = null,
    Dictionary<string, object>? configValues = null
) : StandardToolbarWidget(info, guid, configValues)
{
    private string? _lastCustomCurrencyIds;

    public override MenuPopup Popup { get; } = new();

    protected override StandardWidgetFeatures Features =>
        StandardWidgetFeatures.Icon |
        StandardWidgetFeatures.Text |
        StandardWidgetFeatures.SubText |
        StandardWidgetFeatures.ProgressBar;

    private IDataManager DataManager { get; } = Framework.Service<IDataManager>();
    private IPlayer      Player      { get; } = Framework.Service<IPlayer>();

    protected override void OnLoad()
    {
        UpdateWidgetLabel();
        _lastCustomCurrencyIds = GetConfigValue<string>("CustomCurrencyIds");
        UpdateCustomCurrencyIds();
        InitializeMenu();

        Node.OnClick         += OnNodeClicked;
        Node.OnRightClick    += OpenCurrenciesWindow;
    }

    protected override void OnUnload()
    {
        Node.OnClick         -= OnNodeClicked;
        Node.OnRightClick    -= OpenCurrenciesWindow;
    }

    protected override void OnDraw()
    {
        PollConfigChanges();
        UpdateWidgetLabel();

        Popup.IsDisabled = !GetConfigValue<bool>("EnableMouseInteraction");

        foreach (var currency in DefaultCurrencies.Values) ProcessCurrency(currency);
        foreach (var currency in _CurrencyNotify.Values) ProcessCurrency(currency);
    }

    private void PollConfigChanges()
    {
        string? current = GetConfigValue<string>("CustomCurrencyIds");
        if (string.Equals(current, _lastCustomCurrencyIds, StringComparison.Ordinal)) return;

        _lastCustomCurrencyIds = current;
        UpdateCustomCurrencyIds();
        InitializeMenu();
    }

    private void OnNodeClicked(Node node)
    {
        if (!GetConfigValue<bool>("EnableMouseInteraction")) OpenCurrenciesWindow(node);
    }

    private void ProcessCurrency(Currency currency)
    {
        UpdateCurrency(currency);
        AddOrUpdateButtonForCurrency(currency);
    }

    private unsafe void OpenCurrenciesWindow(Node _)
    {
        UIModule* uiModule = UIModule.Instance();
        if (uiModule == null) return;
        uiModule->ExecuteMainCommand(66);
    }

    private void UpdateWidgetLabel()
    {
        Currency? currency = GetTrackedCurrency();

        if (currency != null) {
            SetWidgetLabelFromCurrency(currency);
            return;
        }

        string customLabel = GetConfigValue<string>("CustomLabel").Trim();
        SetText(string.IsNullOrEmpty(customLabel) ? I18N.Translate("Widget.Currencies.Name") : customLabel);
        SetSubText(null);
        SetProgressBarValue(0);
        SetProgressBarConstraint(0, 1);
        ClearIcon();
    }

    private void SetWidgetLabelFromCurrency(Currency currency)
    {
        string capText  = GetCountText(currency, GetConfigValue<bool>("ShowCapOnWidget"));
        bool   showName = GetConfigValue<bool>("ShowName");

        if (IsSubTextEnabled) {
            SetText(showName ? currency.Name : capText);
            SetSubText(showName ? capText : null);
        } else {
            SetText(showName ? $"{capText} {currency.Name}" : capText);
            SetSubText(null);
        }

        SetGameIconId(currency.IconId);

        if (currency.IsCapped) {
            SetProgressBarConstraint(0, 100);
            ProgressBarNode.UseOverflow     = true;
            ProgressBarNode.Value           = 200;
        } else {
            ProgressBarNode.UseOverflow     = false;

            if (currency.WeeklyCapacity > 0) {
                SetProgressBarConstraint(0, currency.WeeklyCapacity);
                SetProgressBarValue(currency.WeeklyCount);
            } else if (currency.Capacity > 0) {
                SetProgressBarConstraint(0, (int)currency.Capacity);
                SetProgressBarValue(currency.Count);
            } else {
                SetProgressBarConstraint(0, 1);
                SetProgressBarValue(0);
            }
        }
    }
}
