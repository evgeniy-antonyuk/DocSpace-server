﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Files.AutoCleanUp;

[Singleton]
public class CleanupLifetimeExpiredEntriesWorker(ILogger<CleanupLifetimeExpiredEntriesWorker> logger, IServiceScopeFactory serviceScopeFactory)
{
    public async Task DeleteLifetimeExpiredEntries(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        List<LifetimeEnabledRoom> lifetimeEnabledRooms;

        await using (var scope = serviceScopeFactory.CreateAsyncScope())
        {
            await using var dbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<FilesDbContext>>().CreateDbContextAsync(cancellationToken);

            lifetimeEnabledRooms = await GetLifetimeEnabledRoomsAsync(dbContext);

            if (lifetimeEnabledRooms.Count == 0)
            {
                return;
            }

            foreach (var room in lifetimeEnabledRooms)
            {
                var expiration = DateTime.UtcNow;
                switch (room.Lifetime.Period)
                {
                    case RoomDataLifetimePeriod.Day:
                        expiration = expiration.AddDays(-room.Lifetime.Value);
                        break;
                    case RoomDataLifetimePeriod.Month:
                        expiration = expiration.AddMonths(-room.Lifetime.Value);
                        break;
                    case RoomDataLifetimePeriod.Year:
                        expiration = expiration.AddYears(-room.Lifetime.Value);
                        break;
                    default:
                        return;
                }

                room.ExipiredFiles = await GetExpiredFilesAsync(dbContext, room.TenantId, room.RoomId, expiration);

                logger.InfoCleanupLifetimeExpiredEntriesFound(room.TenantId, room.RoomId, room.ExipiredFiles.Count);
            }
        }

        await Parallel.ForEachAsync(lifetimeEnabledRooms.Where(x => x.ExipiredFiles.Count > 0),
                                    new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = cancellationToken }, //System.Environment.ProcessorCount
                                    DeleteExpiredFiles);
    }

    private async ValueTask DeleteExpiredFiles(LifetimeEnabledRoom data, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();
            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            var fileOperationsManager = scope.ServiceProvider.GetRequiredService<FileOperationsManager>();

            var tenant = await tenantManager.SetCurrentTenantAsync(data.TenantId);

            var userAccount = await authManager.GetAccountByIDAsync(tenant.Id, data.UserId);
            if (Equals(userAccount, ASC.Core.Configuration.Constants.Guest))
            {
                userAccount = await authManager.GetAccountByIDAsync(tenant.Id, tenant.OwnerId);
            }

            await securityContext.AuthenticateMeWithoutCookieAsync(userAccount);

            logger.InfoCleanupLifetimeExpiredEntriesStart(data.TenantId, data.RoomId, userAccount.ID, string.Join(',', data.ExipiredFiles));

            await fileOperationsManager.PublishDelete([], data.ExipiredFiles, true, true, data.Lifetime.DeletePermanently);

            logger.InfoCleanupLifetimeExpiredEntriesWait(data.TenantId, data.RoomId, userAccount.ID);

            while (true)
            {
                var statuses = await fileOperationsManager.GetOperationResults();

                if (statuses.TrueForAll(r => r.OperationType != FileOperationType.Delete || r.Finished))
                {
                    break;
                }

                await Task.Delay(100, cancellationToken);
            }

            logger.InfoCleanupLifetimeExpiredEntriesFinish(data.TenantId, data.RoomId, userAccount.ID);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }
    }

    private async Task<List<LifetimeEnabledRoom>> GetLifetimeEnabledRoomsAsync(FilesDbContext dbContext)
    {
        return await Queries.LifetimeEnabledRoomsAsync(dbContext).ToListAsync();
    }

    private async Task<List<int>> GetExpiredFilesAsync(FilesDbContext dbContext, int tenantId, int roomId, DateTime expiration)
    {
        return await Queries.ExpiredFilesAsync(dbContext, tenantId, roomId, expiration).ToListAsync();
    }
}

static file class Queries
{
    public static readonly Func<FilesDbContext, IAsyncEnumerable<LifetimeEnabledRoom>>
        LifetimeEnabledRoomsAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx) =>
                ctx.RoomSettings
                    .Join(ctx.Folders, a => a.RoomId, b => b.Id,
                        (settings, room) => new { settings, room })
                    .Where(x => !string.IsNullOrEmpty(x.settings.Lifetime))
                    .Select(r => new LifetimeEnabledRoom
                    {
                        TenantId = r.settings.TenantId,
                        RoomId = r.settings.RoomId,
                        UserId = r.room.CreateBy,
                        Lifetime = RoomDataLifetime.Deserialize(r.settings.Lifetime)
                    }));

    public static readonly Func<FilesDbContext, int, int, DateTime, IAsyncEnumerable<int>>
        ExpiredFilesAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int roomId, DateTime expiration) =>
                ctx.Tree
                    .Join(ctx.Files, a => a.FolderId, b => b.ParentId, (tree, file) => new { tree, file })
                    .Where(x => x.tree.ParentId == roomId && x.file.TenantId == tenantId && x.file.ModifiedOn < expiration)
                    .Select(r => r.file.Id));
}