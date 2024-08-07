// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using Profile = AutoMapper.Profile;

namespace ASC.Core.Tenants;

[DebuggerDisplay("{TenantId} {Name}")]
public class TenantQuota : IMapFrom<DbQuota>
{
    public static readonly TenantQuota Default = new(Tenant.DefaultTenant)
    {
        Name = "Default",
        MaxFileSize = 25 * 1024 * 1024, // 25Mb
        MaxTotalSize = long.MaxValue,
        CountUser = int.MaxValue,
        CountRoomAdmin = int.MaxValue,
        CountRoom = int.MaxValue
    };

    [SwaggerSchemaCustomInt("Tenant ID")]
    public int TenantId { get; set; }

    [SwaggerSchemaCustomString("Name", Example = "Default")]
    public string Name { get; set; }

    [SwaggerSchemaCustomString("Price", Example = "10.0")]
    public decimal Price { get; set; }

    [SwaggerSchemaCustomString("Price currency symbol")]
    public string PriceCurrencySymbol { get; set; }

    [SwaggerSchemaCustomString("Product ID")]
    public string ProductId { get; set; }

    [SwaggerSchemaCustomBoolean("Specifies if the tenant quota is visible or not")]
    public bool Visible { get; set; }

    [JsonIgnore]
    public IReadOnlyList<TenantQuotaFeature> TenantQuotaFeatures { get; private set; }

    private List<string> _featuresList;

    [SwaggerSchemaCustomString("Tenant quota features")]
    public string Features
    {
        get
        {
            return string.Join(",", _featuresList);
        }
        set
        {
            _featuresList = value != null ? value.Split(' ', ',', ';').ToList() : new List<string>();
        }
    }

    private readonly MaxFileSizeFeature _maxFileSizeFeature;

    [SwaggerSchemaCustomLong("Maximum file size", Example = 25 * 1024 * 1024)]
    public long MaxFileSize
    {
        get => _maxFileSizeFeature.Value;
        set => _maxFileSizeFeature.Value = value;
    }

    private readonly MaxTotalSizeFeature _maxTotalSizeFeature;

    [SwaggerSchemaCustomLong("Maximum total size", Example = long.MaxValue)]
    public long MaxTotalSize
    {
        get => _maxTotalSizeFeature.Value;
        set => _maxTotalSizeFeature.Value = value;
    }

    private readonly CountUserFeature _countUserFeature;

    [SwaggerSchemaCustomInt("Number of portal users")]
    public int CountUser
    {
        get => _countUserFeature.Value;
        set => _countUserFeature.Value = value;
    }

    private readonly CountPaidUserFeature _countPaidUserFeature;

    [SwaggerSchemaCustomInt("Number of portal room administrators")]
    public int CountRoomAdmin
    {
        get => _countPaidUserFeature.Value;
        set => _countPaidUserFeature.Value = value;
    }

    private readonly UsersInRoomFeature _usersInRoomFeature;

    [SwaggerSchemaCustomInt("Number of room users")]
    public int UsersInRoom
    {
        get => _usersInRoomFeature.Value;
        set => _usersInRoomFeature.Value = value;
    }

    private readonly CountRoomFeature _countRoomFeature;

    [SwaggerSchemaCustomInt("Number of rooms")]
    public int CountRoom
    {
        get => _countRoomFeature.Value;
        set => _countRoomFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _nonProfitFeature;

    [SwaggerSchemaCustomBoolean("Specifies if the tenant quota is nonprofit or not")]
    public bool NonProfit
    {
        get => _nonProfitFeature.Value;
        set => _nonProfitFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _trialFeature;

    [SwaggerSchemaCustomBoolean("Specifies if the tenant quota is trial or not")]
    public bool Trial
    {
        get => _trialFeature.Value;
        set => _trialFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _freeFeature;

    [SwaggerSchemaCustomBoolean("Specifies if the tenant quota is free or not")]
    public bool Free
    {
        get => _freeFeature.Value;
        set => _freeFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _updateFeature;

    [SwaggerSchemaCustomBoolean("Specifies if the tenant quota is updated or not")]
    public bool Update
    {
        get => _updateFeature.Value;
        set => _updateFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _auditFeature;

    [SwaggerSchemaCustomBoolean("Specifies if the audit trail is available or not")]
    public bool Audit
    {
        get => _auditFeature.Value;
        set => _auditFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _docsEditionFeature;

    [SwaggerSchemaCustomBoolean("Specifies if this tenant quota is Docs edition or not")]
    public bool DocsEdition
    {
        get => _docsEditionFeature.Value;
        set => _docsEditionFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _ldapFeature;

    [SwaggerSchemaCustomBoolean("Specifies if the LDAP settings are available or not")]
    public bool Ldap
    {
        get => _ldapFeature.Value;
        set => _ldapFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _ssoFeature;

    [SwaggerSchemaCustomBoolean("Specifies if the SSO settings are available or not")]
    public bool Sso
    {
        get => _ssoFeature.Value;
        set => _ssoFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _statisticFeature;

    [SwaggerSchemaCustomBoolean("Specifies if the statistic settings are available or not")]
    public bool Statistic
    {
        get => _statisticFeature.Value;
        set => _statisticFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _whiteLabelFeature;

    [SwaggerSchemaCustomBoolean("Specifies if the white label settings are available or not")]
    public bool WhiteLabel
    {
        get => _whiteLabelFeature.Value;
        set => _whiteLabelFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _customizationFeature;

    [SwaggerSchemaCustomBoolean("Specifies if the customization settings are available or not")]
    public bool Customization
    {
        get => _customizationFeature.Value;
        set => _customizationFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _customFeature;

    [SwaggerSchemaCustomBoolean("Specifies if the custom domain URL is available or not")]
    public bool Custom
    {
        get => _customFeature.Value;
        set => _customFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _autoBackupRestoreFeature;

    [SwaggerSchemaCustomBoolean("Specifies if the automatic Backup&amp;Restore feature is available or not")]
    public bool AutoBackupRestore
    {
        get => _autoBackupRestoreFeature.Value;
        set => _autoBackupRestoreFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _oauthFeature;

    [SwaggerSchemaCustomBoolean("Specifies if Oauth is available or not")]
    public bool Oauth
    {
        get => _oauthFeature.Value;
        set => _oauthFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _contentSearchFeature;

    [SwaggerSchemaCustomBoolean("Specifies if the content search is available or not")]
    public bool ContentSearch
    {
        get => _contentSearchFeature.Value;
        set => _contentSearchFeature.Value = value;
    }

    private readonly TenantQuotaFeatureFlag _thirdPartyFeature;

    [SwaggerSchemaCustomBoolean("Specifies if the third-party accounts linking is available or not")]
    public bool ThirdParty
    {
        get => _thirdPartyFeature.Value;
        set => _thirdPartyFeature.Value = value;
    }

    public TenantQuota()
    {
        _featuresList = new List<string>();

        _countUserFeature = new CountUserFeature(this) { Order = 1 };
        _countPaidUserFeature = new CountPaidUserFeature(this);
        _usersInRoomFeature = new UsersInRoomFeature(this) { Order = 8, Visible = false };
        _countRoomFeature = new CountRoomFeature(this) { Order = 2 };
        _maxTotalSizeFeature = new MaxTotalSizeFeature(this);
        _maxFileSizeFeature = new MaxFileSizeFeature(this);
        _nonProfitFeature = new TenantQuotaFeatureFlag(this) { Name = "non-profit", Visible = false };
        _trialFeature = new TenantQuotaFeatureFlag(this) { Name = "trial", Visible = false };
        _freeFeature = new FreeFeature(this) { Visible = false };
        _updateFeature = new TenantQuotaFeatureFlag(this) { Name = "update", Standalone = true };
        _auditFeature = new TenantQuotaFeatureFlag(this) { Name = "audit", Order = 7 };
        _docsEditionFeature = new TenantQuotaFeatureFlag(this) { Name = "docs", Visible = false };
        _ldapFeature = new TenantQuotaFeatureFlag(this) { Name = "ldap", Order = 3 };
        _ssoFeature = new TenantQuotaFeatureFlag(this) { Name = "sso", Order = 5 };
        _whiteLabelFeature = new TenantQuotaFeatureFlag(this) { Name = "whitelabel", Order = 4 };
        _customizationFeature = new TenantQuotaFeatureFlag(this) { Name = "customization", Visible = false };
        _customFeature = new TenantQuotaFeatureFlag(this) { Name = "custom", Visible = false };
        _autoBackupRestoreFeature = new TenantQuotaFeatureFlag(this) { Name = "restore", Order = 6 };
        _oauthFeature = new TenantQuotaFeatureFlag(this) { Name = "oauth" };
        _contentSearchFeature = new TenantQuotaFeatureFlag(this) { Name = "contentsearch", Visible = false };
        _thirdPartyFeature = new TenantQuotaFeatureFlag(this) { Name = "thirdparty", Order = 9 };
        _statisticFeature = new TenantQuotaFeatureFlag(this) { Name = "statistic", Order = 10 };

        TenantQuotaFeatures = new List<TenantQuotaFeature>
        {
            _countUserFeature,
            _countPaidUserFeature,
            _usersInRoomFeature,
            _countRoomFeature,
            _maxTotalSizeFeature,
            _maxFileSizeFeature,
            _nonProfitFeature,
            _trialFeature,
            _freeFeature,
            _updateFeature,
            _auditFeature,
            _docsEditionFeature,
            _ldapFeature,
            _ssoFeature,
            _whiteLabelFeature,
            _customizationFeature,
            _customFeature,
            _autoBackupRestoreFeature,
            _oauthFeature,
            _contentSearchFeature,
            _thirdPartyFeature,
            _statisticFeature
        };
    }

    public TenantQuota(int tenant) : this()
    {
        TenantId = tenant;
    }

    public TenantQuota(TenantQuota quota) : this()
    {
        TenantId = quota.TenantId;
        Name = quota.Name;
        Price = quota.Price;
        ProductId = quota.ProductId;
        Visible = quota.Visible;
        MaxFileSize = quota.MaxFileSize;
        Features = quota.Features;
    }

    public override int GetHashCode()
    {
        return TenantId.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj is TenantQuota q && q.TenantId == TenantId;
    }

    public async Task CheckAsync(IServiceProvider serviceProvider)
    {
        foreach (var checker in serviceProvider.GetServices<ITenantQuotaFeatureChecker>())
        {
            await checker.CheckUsed(this);
        }
    }

    public static TenantQuota operator *(TenantQuota quota, int quantity)
    {
        var newQuota = new TenantQuota(quota);

        newQuota.Price *= quantity;

        for (var i = 0; i < newQuota.TenantQuotaFeatures.Count; i++)
        {
            newQuota.TenantQuotaFeatures[i].Multiply(quantity);
        }

        return newQuota;
    }

    public static TenantQuota operator +(TenantQuota old, TenantQuota quota)
    {
        if (old == null)
        {
            return quota;
        }

        var newQuota = new TenantQuota(old);
        newQuota.Price += quota.Price;
        newQuota.Visible &= quota.Visible;
        newQuota.ProductId = "";

        foreach (var f in newQuota.TenantQuotaFeatures)
        {
            if (f is MaxFileSizeFeature fileSize)
            {
                if (quota.MaxFileSize != long.MaxValue)
                {
                    fileSize.Value = Math.Max(fileSize.Value, quota.MaxFileSize);
                }
            }
            else if (f is TenantQuotaFeatureCount count)
            {
                var currentValue = count.Value;
                var newValue = quota.GetFeature<int>(f.Name).Value;

                if (currentValue == count.Default && newValue != currentValue)
                {
                    count.Value = newValue;
                }
                else if (currentValue != count.Default && newValue != count.Default)
                {
                    try
                    {
                        if (newValue != int.MaxValue)
                        {
                            count.Value = checked(count.Value + newValue);
                        }
                    }
                    catch (OverflowException)
                    {
                        count.Value = int.MaxValue;
                    }
                }
            }
            else if (f is TenantQuotaFeatureSize length)
            {
                var currentValue = length.Value;
                var newValue = quota.GetFeature<long>(f.Name).Value;

                if (currentValue == length.Default && newValue != currentValue)
                {
                    length.Value = newValue;
                }
                else
                {
                    try
                    {
                        if (newValue != long.MaxValue)
                        {
                            length.Value = checked(length.Value + newValue);
                        }
                    }
                    catch (OverflowException)
                    {
                        length.Value = long.MaxValue;
                    }
                }
            }
            else if (f is TenantQuotaFeatureFlag flag)
            {
                flag.Value |= quota.GetFeature<bool>(f.Name).Value;
            }
        }

        return newQuota;
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DbQuota, TenantQuota>()
            .ForMember(dest => dest.Price, o => o.MapFrom<TenantQuotaPriceResolver>());
    }

    public TenantQuotaFeature<T> GetFeature<T>(string name)
    {
        return TenantQuotaFeatures.OfType<TenantQuotaFeature<T>>().FirstOrDefault(f => string.Equals(f.Name.Split(':')[0], $"{name}", StringComparison.OrdinalIgnoreCase));
    }

    public T GetFeature<T>() where T : TenantQuotaFeature
    {
        return TenantQuotaFeatures.OfType<T>().FirstOrDefault();
    }

    internal string GetFeature(string name)
    {
        return _featuresList.Find(f => string.Equals(f.Split(':')[0], $"{name}", StringComparison.OrdinalIgnoreCase));
    }

    internal void ReplaceFeature<T>(string name, T value, T defaultValue)
    {
        var featureValue = GetFeature(name);
        _featuresList.Remove(featureValue);

        if (!EqualityComparer<T>.Default.Equals(value, default) && !EqualityComparer<T>.Default.Equals(value, defaultValue))
        {
            _featuresList.Add(value is bool ? $"{name}" : $"{name}:{value}");
        }
    }
}