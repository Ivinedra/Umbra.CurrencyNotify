using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Umbra.Common;
using Umbra.Game;
using Umbra.Widgets;

namespace Umbra.CurrencyNotify.Widgets;

public sealed partial class CurrencyNotifyWidget
{
    private readonly Dictionary<uint, Currency> _CurrencyNotify = [];

    private string _lastSeenCustomCurrencyIds = string.Empty;

    private void UpdateCustomCurrencyIds()
    {
        string customCurrencyIds = GetConfigValue<string>("CustomCurrencyIds");

        if (customCurrencyIds == _lastSeenCustomCurrencyIds) return;
        _lastSeenCustomCurrencyIds = customCurrencyIds;

        foreach (var currency in _CurrencyNotify.Values) {
            var group  = Groups[currency.Group];
            var button = CurrencyButtons[currency.Id];
            group.Remove(button, true);
        }

        _CurrencyNotify.Clear();

        foreach (var idString in customCurrencyIds.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
            if (!uint.TryParse(idString, out uint id)) continue;
            if (DefaultCurrencies.ContainsKey(id)) continue;

            Currency? currency = CreateCurrencyFromItemId(id);
            if (currency == null) continue;

            currency.Group = Group.Miscellaneous;
            
            _CurrencyNotify.Add(id, currency);
            AddOrUpdateButtonForCurrency(currency);
        }
    }
}
