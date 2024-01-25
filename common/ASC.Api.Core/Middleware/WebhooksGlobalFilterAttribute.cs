﻿// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Api.Core.Middleware;

[Scope]
public class WebhooksGlobalFilterAttribute(IWebhookPublisher webhookPublisher,
        ILogger<WebhooksGlobalFilterAttribute> logger,
        SettingsManager settingsManager,
        DbWorker dbWorker)
    : ResultFilterAttribute, IDisposable
{
    private readonly MemoryStream _stream = new();
    private Stream _bodyStream;

    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var skip = await SkipAsync(context.HttpContext);

        if (!skip)
        {
            _bodyStream = context.HttpContext.Response.Body;
            context.HttpContext.Response.Body = _stream;
        }

        await base.OnResultExecutionAsync(context, next);

        if (context.Cancel || skip)
        {
            return;
        }

        if (_stream is { CanRead: true })
        {
            _stream.Position = 0;
            await _stream.CopyToAsync(_bodyStream);
            context.HttpContext.Response.Body = _bodyStream;

            try
            {
                var (method, routePattern, _) = GetData(context.HttpContext);

                var resultContent = Encoding.UTF8.GetString(_stream.ToArray());

                var webhook = await dbWorker.GetWebhookAsync(method, routePattern);

                await webhookPublisher.PublishAsync(webhook.Id, resultContent);
            }
            catch (Exception e)
            {
                logger.ErrorWithException(e);
            }
        }
    }

    public void Dispose()
    {
        _stream?.Dispose();
    }

    private (string, string, bool) GetData(HttpContext context)
    {
        var method = context.Request.Method;
        var endpoint = (RouteEndpoint)context.GetEndpoint();
        var routePattern = endpoint?.RoutePattern.RawText;
        var disabled = endpoint?.Metadata.OfType<WebhookDisableAttribute>().FirstOrDefault();
        return (method, routePattern, disabled != null);
    }

    private async Task<bool> SkipAsync(HttpContext context)
    {
        var (method, routePattern, disabled) = GetData(context);

        if (routePattern == null)
        {
            return true;
        }
        
        if (disabled)
        {
            return true;
        }
        
        if (!DbWorker.MethodList.Contains(method))
        {
            return true;
        }

        var webhook = await dbWorker.GetWebhookAsync(method, routePattern);
        if (webhook == null || (await settingsManager.LoadAsync<WebHooksSettings>()).Ids.Contains(webhook.Id))
        {
            return true;
        }

        return false;
    }
}