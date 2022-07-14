// (c) Copyright Ascensio System SIA 2010-2022
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

namespace ASC.Core.Billing;

[Singletone]
public class TariffServiceStorage
{
    public ICache Cache { get; }
    internal readonly ICacheNotify<TariffCacheItem> Notify;

    public TariffServiceStorage(ICacheNotify<TariffCacheItem> notify, ICache cache)
    {
        Cache = cache;
        Notify = notify;
        Notify.Subscribe((i) =>
        {
            Cache.Remove(TariffService.GetTariffCacheKey(i.TenantId));
            Cache.Remove(TariffService.GetBillingUrlCacheKey(i.TenantId));
            Cache.Remove(TariffService.GetBillingPaymentCacheKey(i.TenantId)); // clear all payments
        }, CacheNotifyAction.Remove);

        //TODO: Change code of WCF -> not supported in .NET standard/.Net Core
        /*try
        {
            var section = (ClientSection)ConfigurationManager.GetSection("system.serviceModel/client");
            if (section != null)
            {
                billingConfigured = section.Endpoints.Cast<ChannelEndpointElement>()
                    .Any(e => e.Contract == typeof(IService).FullName);
            }
        }
        catch (Exception err)
        {
            log.Error(err);
        }*/
    }
}

public class TariffService : ITariffService
{
    private const int DefaultTrialPeriod = 30;
    private static readonly TimeSpan _defaultCacheExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _standaloneCacheExpiration = TimeSpan.FromMinutes(15);

    private readonly ICache _cache;
    private readonly ICacheNotify<TariffCacheItem> _notify;
    private readonly ILogger<TariffService> _logger;
    private readonly IQuotaService _quotaService;
    private readonly ITenantService _tenantService;
    private readonly int _paymentDelay;
    private TimeSpan _cacheExpiration;
    private readonly CoreBaseSettings _coreBaseSettings;
    private readonly CoreSettings _coreSettings;
    private readonly IConfiguration _configuration;
    private readonly IDbContextFactory<CoreDbContext> _dbContextFactory;
    private readonly TariffServiceStorage _tariffServiceStorage;
    private readonly BillingClient _billingClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly int _activeUsersMin;
    //private readonly int _activeUsersMax;

    public TariffService()
    {
        _cacheExpiration = _defaultCacheExpiration;
    }

    public TariffService(
        IQuotaService quotaService,
        ITenantService tenantService,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        IDbContextFactory<CoreDbContext> coreDbContextManager,
        TariffServiceStorage tariffServiceStorage,
        ILogger<TariffService> logger,
        BillingClient billingClient,
        IHttpClientFactory httpClientFactory)
        : this()

    {
        _logger = logger;
        _quotaService = quotaService;
        _tenantService = tenantService;
        _coreSettings = coreSettings;
        _configuration = configuration;
        _tariffServiceStorage = tariffServiceStorage;
        _billingClient = billingClient;
        _httpClientFactory = httpClientFactory;
        _coreBaseSettings = coreBaseSettings;
        int.TryParse(configuration["core:payment:delay"], out var paymentDelay);

        _paymentDelay = paymentDelay;

        _cache = _tariffServiceStorage.Cache;
        _notify = _tariffServiceStorage.Notify;
        _dbContextFactory = coreDbContextManager;
        var range = (_configuration["core.payment-user-range"] ?? "").Split('-');
        if (!int.TryParse(range[0], out _activeUsersMin))
        {
            _activeUsersMin = 0;
        }
        //if (range.Length < 2 || !int.TryParse(range[1], out _activeUsersMax))
        //{
        //    _activeUsersMax = constants.MaxEveryoneCount;
        //}
    }

    public Tariff GetTariff(int tenantId, bool withRequestToPaymentSystem = true)
    {
        //single tariff for all portals
        if (_coreBaseSettings.Standalone)
        {
            tenantId = -1;
        }

        var key = GetTariffCacheKey(tenantId);
        var tariff = _cache.Get<Tariff>(key);
        if (tariff == null)
        {
            tariff = GetBillingInfo(tenantId);
            tariff = CalculateTariff(tenantId, tariff);
            _cache.Insert(key, tariff, DateTime.UtcNow.Add(GetCacheExpiration()));

            if (_billingClient.Configured && withRequestToPaymentSystem)
            {
                //Task.Run(() =>
                //  {
                      try
                      {
                          var client = GetBillingClient();
                          var currentPayments = client.GetCurrentPayments(GetPortalId(tenantId));

                          var asynctariff = Tariff.CreateDefault(true);

                          foreach (var currentPayment in currentPayments)
                          {
                              var quota = _quotaService.GetTenantQuotas().SingleOrDefault(q => q.ProductId == currentPayment.ProductId);
                              if (quota == null)
                              {
                                  throw new InvalidOperationException($"Quota with id {currentPayment.ProductId} not found for portal {GetPortalId(tenantId)}.");
                              }

                              var paymentEndDate = 9999 <= currentPayment.EndDate.Year ? DateTime.MaxValue : currentPayment.EndDate;
                              asynctariff.DueDate = DateTime.Compare(asynctariff.DueDate, paymentEndDate) < 0 ? asynctariff.DueDate : paymentEndDate;

                              asynctariff.Quotas.Add(new Tuple<int, int>(quota.Tenant, currentPayment.Quantity));
                          }

                          if (SaveBillingInfo(tenantId, asynctariff, false))
                          {
                              asynctariff = CalculateTariff(tenantId, asynctariff);
                              ClearCache(tenantId);
                              _cache.Insert(key, asynctariff, DateTime.UtcNow.Add(GetCacheExpiration()));
                          }
                      }
                      catch (BillingNotFoundException)
                      {
                          var freeTariff = tariff.Quotas.Exists(tariffRow =>
                          {
                              var q = _quotaService.GetTenantQuota(tariffRow.Item1);
                              return q == null
                                  || q.Trial
                                  || q.Free
                                  || q.NonProfit
                                  || q.Open
                                  || q.Custom;
                          });

                          if (!freeTariff)
                          {
                              var asynctariff = Tariff.CreateDefault();
                              asynctariff.DueDate = DateTime.Today.AddDays(-1);
                              asynctariff.State = TariffState.NotPaid;

                              if (SaveBillingInfo(tenantId, asynctariff))
                              {
                                  asynctariff = CalculateTariff(tenantId, asynctariff);
                                  ClearCache(tenantId);
                                  _cache.Insert(key, asynctariff, DateTime.UtcNow.Add(GetCacheExpiration()));
                              }
                          }
                      }
                      catch (Exception error)
                      {
                          LogError(error, tenantId.ToString());
                      }
                  //});
            }
        }

        return tariff;
    }

    public void SetTariff(int tenantId, Tariff tariff)
    {
        ArgumentNullException.ThrowIfNull(tariff);

        List<TenantQuota> quotas = null;
        if (tariff.Quotas == null ||
            (quotas = tariff.Quotas.Select(q => _quotaService.GetTenantQuota(q.Item1)).ToList()).Any(q => q == null))
        {
            return;
        }

        SaveBillingInfo(tenantId, tariff);
        if (quotas.Any(q => q.Trial))
        {
            // reset trial date
            var tenant = _tenantService.GetTenant(tenantId);
            if (tenant != null)
            {
                tenant.VersionChanged = DateTime.UtcNow;
                _tenantService.SaveTenant(_coreSettings, tenant);
            }
        }

        ClearCache(tenantId);
    }

    internal static string GetTariffCacheKey(int tenantId)
    {
        return string.Format("{0}:{1}", tenantId, "tariff");
    }

    internal static string GetBillingUrlCacheKey(int tenantId)
    {
        return string.Format("{0}:{1}", tenantId, "billing:urls");
    }

    internal static string GetBillingPaymentCacheKey(int tenantId)
    {
        return string.Format("{0}:{1}", tenantId, "billing:payments");
    }


    public void ClearCache(int tenantId)
    {
        _notify.Publish(new TariffCacheItem { TenantId = tenantId }, CacheNotifyAction.Remove);
    }

    public IEnumerable<PaymentInfo> GetPayments(int tenantId)
    {
        var key = GetBillingPaymentCacheKey(tenantId);
        var payments = _cache.Get<List<PaymentInfo>>(key);
        if (payments == null)
        {
            payments = new List<PaymentInfo>();
            if (_billingClient.Configured)
            {
                try
                {
                    var quotas = _quotaService.GetTenantQuotas();
                    var client = GetBillingClient();
                    foreach (var pi in client.GetPayments(GetPortalId(tenantId)))
                    {
                        var quota = quotas.SingleOrDefault(q => q.ProductId == pi.ProductRef);
                        if (quota != null)
                        {
                            pi.QuotaId = quota.Tenant;
                        }
                        payments.Add(pi);
                    }
                }
                catch (Exception error)
                {
                    LogError(error, tenantId.ToString());
                }
            }

            _cache.Insert(key, payments, DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
        }

        return payments;
    }

    public Uri GetShoppingUri(int tenant, string currency = null, string language = null, string customerId = null, string quantity = null)
    {
        var key = "shopingurl_all";
        var url = _cache.Get<string>(key);
        if (url == null)
        {
            url = string.Empty;
            if (_billingClient.Configured)
            {
                try
                {
                    var productIds = _quotaService.GetTenantQuotas()
                                               .Where(q => !string.IsNullOrEmpty(q.ProductId) && q.Visible)
                                               .Select(q => q.ProductId);

                    var client = GetBillingClient();
                    url =
                        client.GetPaymentUrl(
                            "__Tenant__",
                            productIds.ToArray(),
                            null,
                            null,
                            !string.IsNullOrEmpty(currency) ? "__Currency__" : null,
                            !string.IsNullOrEmpty(language) ? "__Language__" : null,
                            !string.IsNullOrEmpty(customerId) ? "__CustomerID__" : null,
                            !string.IsNullOrEmpty(quantity) ? "__Quantity__" : null
                            );
                }
                catch (Exception error)
                {
                    _logger.ErrorWithException(error);
                }
            }
            _cache.Insert(key, url, DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
        }

        ResetCacheExpiration();

        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        var result = new Uri(url.ToString()
                               .Replace("__Tenant__", HttpUtility.UrlEncode(GetPortalId(tenant)))
                               .Replace("__Currency__", HttpUtility.UrlEncode(currency ?? ""))
                               .Replace("__Language__", HttpUtility.UrlEncode((language ?? "").ToLower()))
                               .Replace("__CustomerID__", HttpUtility.UrlEncode(customerId ?? ""))
                               .Replace("__Quantity__", HttpUtility.UrlEncode(quantity ?? "")));
        return result;
    }

    public Uri GetShoppingUri(int? tenant, int quotaId, string affiliateId, string currency = null, string language = null, string customerId = null, string quantity = null)
    {
        var quota = _quotaService.GetTenantQuota(quotaId);
        if (quota == null)
        {
            return null;
        }

        var key = tenant.HasValue
                      ? GetBillingUrlCacheKey(tenant.Value)
                      : string.Format($"notenant{(!string.IsNullOrEmpty(affiliateId) ? "_" + affiliateId : "")}");
        key += quota.Visible ? "" : "0";
        if (_cache.Get<Dictionary<string, Uri>>(key) is not IDictionary<string, Uri> urls)
        {
            urls = new Dictionary<string, Uri>();
            if (_billingClient.Configured)
            {
                try
                {
                    var products = _quotaService.GetTenantQuotas()
                                               .Where(q => !string.IsNullOrEmpty(q.ProductId) && q.Visible == quota.Visible)
                                               .Select(q => q.ProductId)
                                               .ToArray();

                    var client = GetBillingClient();
                    urls =
                        client.GetPaymentUrls(
                            tenant.HasValue ? GetPortalId(tenant.Value) : null,
                            products,
                            tenant.HasValue ? GetAffiliateId(tenant.Value) : affiliateId,
                            tenant.HasValue ? GetCampaign(tenant.Value) : null,
                            !string.IsNullOrEmpty(currency) ? "__Currency__" : null,
                            !string.IsNullOrEmpty(language) ? "__Language__" : null,
                            !string.IsNullOrEmpty(customerId) ? "__CustomerID__" : null,
                            !string.IsNullOrEmpty(quantity) ? "__Quantity__" : null
                            );
                }
                catch (Exception error)
                {
                    _logger.ErrorGetShoppingUri(error);
                }
            }
            _cache.Insert(key, urls, DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
        }

        ResetCacheExpiration();

        if (!string.IsNullOrEmpty(quota.ProductId) && urls.TryGetValue(quota.ProductId, out var url))
        {
            if (url == null)
            {
                return null;
            }

            url = new Uri(url.ToString()
                                   .Replace("__Currency__", HttpUtility.UrlEncode(currency ?? ""))
                                   .Replace("__Language__", HttpUtility.UrlEncode((language ?? "").ToLower()))
                                   .Replace("__CustomerID__", HttpUtility.UrlEncode(customerId ?? ""))
                                   .Replace("__Quantity__", HttpUtility.UrlEncode(quantity ?? "")));
            return url;
        }
        return null;
    }

    public Uri GetShoppingUri(string[] productIds, string affiliateId = null, string currency = null, string language = null, string customerId = null, string quantity = null)
    {
        var key = "shopingurl" + string.Join("_", productIds) + (!string.IsNullOrEmpty(affiliateId) ? "_" + affiliateId : "");
        var url = _cache.Get<string>(key);
        if (url == null)
        {
            url = string.Empty;
            if (_billingClient.Configured)
            {
                try
                {
                    var client = GetBillingClient();
                    url =
                        client.GetPaymentUrl(
                            null,
                            productIds,
                            affiliateId,
                            null,
                            !string.IsNullOrEmpty(currency) ? "__Currency__" : null,
                            !string.IsNullOrEmpty(language) ? "__Language__" : null,
                            !string.IsNullOrEmpty(customerId) ? "__CustomerID__" : null,
                            !string.IsNullOrEmpty(quantity) ? "__Quantity__" : null
                            );
                }
                catch (Exception error)
                {
                    _logger.ErrorWithException(error);
                }
            }
            _cache.Insert(key, url, DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
        }

        ResetCacheExpiration();

        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        var result = new Uri(url.ToString()
                               .Replace("__Currency__", HttpUtility.UrlEncode(currency ?? ""))
                               .Replace("__Language__", HttpUtility.UrlEncode((language ?? "").ToLower()))
                               .Replace("__CustomerID__", HttpUtility.UrlEncode(customerId ?? ""))
                               .Replace("__Quantity__", HttpUtility.UrlEncode(quantity ?? "")));
        return result;
    }

    public IDictionary<string, Dictionary<string, decimal>> GetProductPriceInfo(params string[] productIds)
    {
        ArgumentNullException.ThrowIfNull(productIds);

        try
        {
            var key = "biling-prices" + string.Join(",", productIds);
            var result = _cache.Get<IDictionary<string, Dictionary<string, decimal>>>(key);
            if (result == null)
            {
                var client = GetBillingClient();
                result = client.GetProductPriceInfo(productIds);
                _cache.Insert(key, result, DateTime.Now.AddHours(1));
            }

            return result;
        }
        catch (Exception error)
        {
            LogError(error);

            return productIds
                .Select(p => new { ProductId = p, Prices = new Dictionary<string, decimal>() })
                .ToDictionary(e => e.ProductId, e => e.Prices);
        }
    }


    public string GetButton(int tariffId, string partnerId)
    {
        using var coreDbContext = _dbContextFactory.CreateDbContext();

        return coreDbContext.Buttons
            .Where(r => r.TariffId == tariffId && r.PartnerId == partnerId)
            .Select(r => r.ButtonUrl)
            .SingleOrDefault();
    }

    public void SaveButton(int tariffId, string partnerId, string buttonUrl)
    {
        var efButton = new DbButton()
        {
            TariffId = tariffId,
            PartnerId = partnerId,
            ButtonUrl = buttonUrl
        };

        using var coreDbContext = _dbContextFactory.CreateDbContext();
        coreDbContext.AddOrUpdate(r => r.Buttons, efButton);
        coreDbContext.SaveChanges();
    }


    private Tariff GetBillingInfo(int tenant)
    {
        using var coreDbContext = _dbContextFactory.CreateDbContext();
        var r = coreDbContext.Tariffs
            .Where(r => r.Tenant == tenant)
            .OrderByDescending(r => r.Id)
            .FirstOrDefault();

        if (r == null)
        {
            return Tariff.CreateDefault();
        }

        var tariff = Tariff.CreateDefault(true);
        tariff.DueDate = r.Stamp.Year < 9999 ? r.Stamp : DateTime.MaxValue;

        var tariffRows = coreDbContext.TariffRows
            .Where(row => row.Id == r.Tariff && row.Tenant == tenant);

        tariff.Quotas = tariffRows.Select(r => new Tuple<int, int>(r.Quota, r.Quantity)).ToList();

        return tariff;
    }

    private bool SaveBillingInfo(int tenant, Tariff tariffInfo, bool renewal = true)
    {
        var inserted = false;
        var currentTariff = GetBillingInfo(tenant);
        if (!tariffInfo.EqualsByParams(currentTariff))
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var strategy = dbContext.Database.CreateExecutionStrategy();

            strategy.Execute(() =>
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                using var tx = dbContext.Database.BeginTransaction();

                // last record is not the same
                var any = false;
                // think about tariffrows ?
                //dbContext.Tariffs
                //    .Any(r => r.Tenant == tenant && r.Stamp == tariffInfo.DueDate);

                if (tariffInfo.DueDate == DateTime.MaxValue || renewal || any)
                {
                    var tariffRowId = dbContext.TariffRows.Any() ? dbContext.TariffRows.Max(r => r.Id) + 1 : 1;

                    var tariffRows = tariffInfo.Quotas.Select(q => new DbTariffRow
                    {
                        Id = tariffRowId,
                        Quota = q.Item1,
                        Quantity = q.Item2,
                        Tenant = tenant
                    });
                    dbContext.TariffRows.AddRange(tariffRows);

                    var efTariff = new DbTariff
                    {
                        Tenant = tenant,
                        Tariff = tariffRowId,
                        Stamp = tariffInfo.DueDate,
                        CreateOn = DateTime.UtcNow
                    };

                    dbContext.Tariffs.Add(efTariff);
                    dbContext.SaveChanges();

                    _cache.Remove(GetTariffCacheKey(tenant));
                    inserted = true;
                }

                tx.Commit();
            });
        }

        if (inserted)
        {
            var t = _tenantService.GetTenant(tenant);
            if (t != null)
            {
                // update tenant.LastModified to flush cache in documents
                _tenantService.SaveTenant(_coreSettings, t);
            }
        }

        return inserted;
    }

    public void DeleteDefaultBillingInfo()
    {
        const int tenant = Tenant.DefaultTenant;

        using var coreDbContext = _dbContextFactory.CreateDbContext();
        var tariffs = coreDbContext.Tariffs.Where(r => r.Tenant == tenant).ToList();

        foreach (var t in tariffs)
        {
            t.Tenant = -2;
            t.CreateOn = DateTime.UtcNow;
        }

        coreDbContext.SaveChanges();

        ClearCache(tenant);
    }


    private Tariff CalculateTariff(int tenantId, Tariff tariff)
    {
        tariff.State = TariffState.Paid;

        if (tariff.Quotas.Count == 0)
        {
            tariff.Quotas.Add(new Tuple<int, int>(Tenant.DefaultTenant, 1));
        }

        var trial = tariff.Quotas.Exists(q => _quotaService.GetTenantQuota(q.Item1).Trial);

        var delay = 0;
        if (trial)
        {
            tariff.State = TariffState.Trial;
            if (tariff.DueDate == DateTime.MinValue || tariff.DueDate == DateTime.MaxValue)
            {
                var tenant = _tenantService.GetTenant(tenantId);
                if (tenant != null)
                {
                    var fromDate = tenant.CreationDateTime < tenant.VersionChanged ? tenant.VersionChanged : tenant.CreationDateTime;
                    var trialPeriod = GetPeriod("TrialPeriod", DefaultTrialPeriod);
                    if (fromDate == DateTime.MinValue)
                    {
                        fromDate = DateTime.UtcNow.Date;
                    }

                    tariff.DueDate = trialPeriod != default ? fromDate.Date.AddDays(trialPeriod) : DateTime.MaxValue;
                }
                else
                {
                    tariff.DueDate = DateTime.MaxValue;
                }
            }
        }
        else
        {
            delay = _paymentDelay;
        }

        if (tariff.DueDate != DateTime.MinValue && tariff.DueDate.Date < DateTime.Today && delay > 0)
        {
            tariff.State = TariffState.Delay;

            tariff.DelayDueDate = tariff.DueDate.Date.AddDays(delay);
        }

        if (tariff.DueDate == DateTime.MinValue ||
            tariff.DueDate != DateTime.MaxValue && tariff.DueDate.Date.AddDays(delay) < DateTime.Today)
        {
            tariff.State = TariffState.NotPaid;
        }

        return tariff;
    }

    private int GetPeriod(string key, int defaultValue)
    {
        var settings = _tenantService.GetTenantSettings(Tenant.DefaultTenant, key);

        return settings != null ? Convert.ToInt32(Encoding.UTF8.GetString(settings)) : defaultValue;
    }

    private BillingClient GetBillingClient()
    {
        try
        {
            return new BillingClient(_configuration, _httpClientFactory);
        }
        catch (InvalidOperationException ioe)
        {
            throw new BillingNotConfiguredException(ioe.Message, ioe);
        }
        catch (ReflectionTypeLoadException rtle)
        {
            _logger.ErrorLoaderExceptions(string.Join(Environment.NewLine, rtle.LoaderExceptions.Select(e => e.ToString())), rtle);
            throw;
        }
    }

    private string GetPortalId(int tenant)
    {
        return _coreSettings.GetKey(tenant);
    }

    private string GetAffiliateId(int tenant)
    {
        return _coreSettings.GetAffiliateId(tenant);
    }

    private string GetCampaign(int tenant)
    {
        return _coreSettings.GetCampaign(tenant);
    }

    private TimeSpan GetCacheExpiration()
    {
        if (_coreBaseSettings.Standalone && _cacheExpiration < _standaloneCacheExpiration)
        {
            _cacheExpiration = _cacheExpiration.Add(TimeSpan.FromSeconds(30));
        }
        return _cacheExpiration;
    }

    private void ResetCacheExpiration()
    {
        if (_coreBaseSettings.Standalone)
        {
            _cacheExpiration = _defaultCacheExpiration;
        }
    }

    private void LogError(Exception error, string tenantId = null)
    {
        if (error is BillingNotFoundException)
        {
            _logger.DebugPaymentTenant(tenantId, error.Message);
        }
        else if (error is BillingNotConfiguredException)
        {
            _logger.DebugBillingTenant(tenantId, error.Message);
        }
        else
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.ErrorBillingWithException(tenantId, error);
            }
            else
            {
                _logger.ErrorBilling(tenantId, error.Message);
            }
        }
    }
}
