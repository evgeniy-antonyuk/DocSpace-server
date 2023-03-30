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

namespace ASC.Notify;

[Singletone]
public class DbWorker : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly NotifyServiceCfg _notifyServiceCfg;

    public DbWorker(IServiceScopeFactory serviceScopeFactory, IOptions<NotifyServiceCfg> notifyServiceCfg)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _notifyServiceCfg = notifyServiceCfg.Value;
    }

    public async Task SaveMessageAsync(NotifyMessage m)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var _mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        using var dbContext = scope.ServiceProvider.GetService<IDbContextFactory<NotifyDbContext>>().CreateDbContext();

        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            var notifyQueue = _mapper.Map<NotifyMessage, NotifyQueue>(m);

            notifyQueue = (await dbContext.NotifyQueue.AddAsync(notifyQueue)).Entity;

            var id = notifyQueue.NotifyId;

            var info = new NotifyInfo
            {
                NotifyId = id,
                State = 0,
                Attempts = 0,
                ModifyDate = DateTime.UtcNow,
                Priority = m.Priority
            };

            await dbContext.NotifyInfo.AddAsync(info);
            await dbContext.SaveChangesAsync();

            await tx.CommitAsync();
        });
    }

    public async Task<IDictionary<int, NotifyMessage>> GetMessagesAsync(int count)
    {
        try
        {
            await _semaphore.WaitAsync();
            using var scope = _serviceScopeFactory.CreateScope();

            var _mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

            using var dbContext = scope.ServiceProvider.GetService<IDbContextFactory<NotifyDbContext>>().CreateDbContext();

            var q = dbContext.NotifyQueue
                .Join(dbContext.NotifyInfo, r => r.NotifyId, r => r.NotifyId, (queue, info) => new { queue, info })
                .Where(r => r.info.State == (int)MailSendingState.NotSended || r.info.State == (int)MailSendingState.Error && r.info.ModifyDate < DateTime.UtcNow - TimeSpan.Parse(_notifyServiceCfg.Process.AttemptsInterval))
                .OrderBy(i => i.info.Priority)
                .ThenBy(i => i.info.NotifyId)
                .Take(count);


            var messages = await q
                .ToDictionaryAsync(
                    r => r.queue.NotifyId,
                    r =>
                    {
                        var res = _mapper.Map<NotifyQueue, NotifyMessage>(r.queue);

                        try
                        {
                            res.Attachments = JsonConvert.DeserializeObject<NotifyMessageAttachment[]>(r.queue.Attachments);
                        }
                        catch (Exception)
                        {

                        }

                        return res;
                    });


            await dbContext.NotifyInfo.Where(r => messages.Keys.Any(a => a == r.NotifyId)).ExecuteUpdateAsync(q=> q.SetProperty(p => p.State, (int)MailSendingState.Sending));

            return messages;
        }
        catch
        {
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ResetStatesAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetService<IDbContextFactory<NotifyDbContext>>().CreateDbContext();

        await dbContext.NotifyInfo.Where(r => r.State == 1).ExecuteUpdateAsync(q => q.SetProperty(p => p.State, 0));
    }

    public async Task SetStateAsync(int id, MailSendingState result)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var dbContext = scope.ServiceProvider.GetService<IDbContextFactory<NotifyDbContext>>().CreateDbContext();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            using var tx = await dbContext.Database.BeginTransactionAsync();
            if (result == MailSendingState.Sended)
            {
                var d = await dbContext.NotifyInfo.Where(r => r.NotifyId == id).FirstOrDefaultAsync();
                dbContext.NotifyInfo.Remove(d);
                await dbContext.SaveChangesAsync();
            }
            else
            {
                if (result == MailSendingState.Error)
                {
                    var attempts = await dbContext.NotifyInfo.Where(r => r.NotifyId == id).Select(r => r.Attempts).FirstOrDefaultAsync();
                    if (_notifyServiceCfg.Process.MaxAttempts <= attempts + 1)
                    {
                        result = MailSendingState.FatalError;
                    }
                }

                await dbContext.NotifyInfo.Where(r => r.NotifyId == id)
                .ExecuteUpdateAsync(q =>
                    q.SetProperty(p => p.State, (int)result)
                    .SetProperty(p => p.Attempts, p => p.Attempts + 1)
                    .SetProperty(p => p.ModifyDate, DateTime.UtcNow));
            }

            await tx.CommitAsync();
        });
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
