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

using System.Security;

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Files.ThumbnailBuilder;

[Singleton(Additional = typeof(FileConverterQueueExtension))]
internal class FileConverterService<T>(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<FileConverterService<T>> logger)
    : BackgroundService
    {
    private readonly int _timerDelay = 1000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.DebugFileConverterServiceRuning();

        stoppingToken.Register(logger.DebugFileConverterServiceStopping);

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var serviceScope = serviceScopeFactory.CreateAsyncScope();

            var registerInstanceService = serviceScope.ServiceProvider.GetService<IRegisterInstanceManager<FileConverterService<T>>>();

            if (!await registerInstanceService.IsActive(RegisterInstanceWorkerService<FileConverterService<T>>.InstanceId))
            {
                await Task.Delay(1000, stoppingToken);

                continue;
            }

            await ExecuteCheckFileConverterStatusAsync(serviceScope);

            await Task.Delay(_timerDelay, stoppingToken);
        }
    }

    private async Task ExecuteCheckFileConverterStatusAsync(IServiceScope scope)
    {
        var logger = scope.ServiceProvider.GetService<ILogger<FileConverterQueue>>();

        try
        {
            var fileConverterQueue = scope.ServiceProvider.GetService<FileConverterQueue>();

            var conversionQueue = fileConverterQueue.GetAllTask<T>().ToList();

            if (conversionQueue.Count > 0)
            {
                logger.DebugRunCheckConvertFilesStatus(conversionQueue.Count);
            }

            var filesIsConverting = conversionQueue
                                    .Where(x => string.IsNullOrEmpty(x.Processed))
                                    .ToList();

            foreach (var converter in filesIsConverting)
            {
                converter.Processed = "1";

                var parsed = JsonDocument.Parse(converter.Source).RootElement;
                var fileId = parsed.GetProperty("id").Deserialize<T>();
                var fileVersion = parsed.GetProperty("version").Deserialize<int>();
                var updateIfExist = parsed.GetProperty("updateIfExist").Deserialize<bool>();

                int operationResultProgress;
                var password = converter.Password;

                var commonLinkUtility = scope.ServiceProvider.GetService<CommonLinkUtility>();
                commonLinkUtility.ServerUri = converter.ServerRootPath;

                var scopeClass = scope.ServiceProvider.GetService<FileConverterQueueScope>();
                var (tenantManager, userManager, securityContext, daoFactory, fileSecurity, pathProvider, setupInfo, fileUtility, documentServiceHelper, documentServiceConnector, entryManager, fileConverter) = scopeClass;

                await tenantManager.SetCurrentTenantAsync(converter.TenantId);

                await securityContext.AuthenticateMeWithoutCookieAsync(converter.Account);

                var file = await daoFactory.GetFileDao<T>().GetFileAsync(fileId, fileVersion);
                var fileUri = file.Id.ToString();

                string convertedFileUrl;
                string convertedFileType;

                try
                {
                    var externalShare = scope.ServiceProvider.GetRequiredService<ExternalShare>();

                    if (!string.IsNullOrEmpty(converter.ExternalShareData))
                    {
                        externalShare.SetCurrentShareData(JsonSerializer.Deserialize<ExternalShareData>(converter.ExternalShareData));
                    }

                    var user = await userManager.GetUsersAsync(converter.Account);

                    var culture = string.IsNullOrEmpty(user.CultureName) ? (await tenantManager.GetCurrentTenantAsync()).GetCulture() : CultureInfo.GetCultureInfo(user.CultureName);

                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;

                    if (!await fileSecurity.CanReadAsync(file) && file.RootFolderType != FolderType.BUNCH)
                    {
                        //No rights in CRM after upload before attach
                        throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException_ReadFile);
                    }

                    if (file.ContentLength > setupInfo.AvailableFileSize)
                    {
                        throw new Exception(string.Format(FilesCommonResource.ErrorMassage_FileSizeConvert, FileSizeComment.FilesSizeToString(setupInfo.AvailableFileSize)));
                    }

                    fileUri = await pathProvider.GetFileStreamUrlAsync(file);

                    var toExtension = fileUtility.GetInternalConvertExtension(file.Title);
                    var fileExtension = file.ConvertedExtension;
                    var docKey = await documentServiceHelper.GetDocKeyAsync(file);

                    fileUri = await documentServiceConnector.ReplaceCommunityAdressAsync(fileUri);
                    (operationResultProgress, convertedFileUrl, convertedFileType) = await documentServiceConnector.GetConvertedUriAsync(fileUri, fileExtension, toExtension, docKey, password, CultureInfo.CurrentUICulture.Name, null, null, null, true);
                }
                catch (Exception exception)
                {
                    var password1 = exception.InnerException is DocumentServiceException { Code: DocumentServiceException.ErrorCode.ConvertPassword };

                    logger.ErrorConvertFileWithUrl(file.Id.ToString(), fileUri, exception);

                    if (converter.Delete)
                    {
                        conversionQueue.Remove(converter);
                    }
                    else
                    {
                        converter.Progress = 100;
                        converter.StopDateTime = DateTime.UtcNow;
                        converter.Error = exception.Message;

                        if (password1)
                        {
                            converter.Result = "password";
                        }
                    }

                    continue;
                }

                operationResultProgress = Math.Min(operationResultProgress, 100);

                if (operationResultProgress < 100)
                {
                    if (DateTime.UtcNow - converter.StartDateTime > TimeSpan.FromMinutes(10))
                    {
                        converter.StopDateTime = DateTime.UtcNow;
                        converter.Error = FilesCommonResource.ErrorMassage_ConvertTimeout;

                        logger.ErrorCheckConvertFilesStatus(file.Id.ToString(), file.ContentLength);
                    }
                    else
                    {
                        converter.Processed = "";
                    }

                    converter.Progress = operationResultProgress;

                    logger.DebugCheckConvertFilesStatusIterationContinue();

                    continue;
                }

                File<T> newFile = null;

                var operationResultError = string.Empty;

                try
                {
                    newFile = await fileConverter.SaveConvertedFileAsync(file, convertedFileUrl, convertedFileType, updateIfExist);
                }
                catch (Exception e)
                {
                    operationResultError = e.Message;

                    logger.ErrorOperation(operationResultError, convertedFileUrl, fileUri, convertedFileType, e);

                    continue;
                }
                finally
                {
                    if (converter.Delete)
                    {
                        conversionQueue.Remove(converter);
                    }
                    else
                    {
                        if (newFile != null)
                        {
                            var folderDao = daoFactory.GetFolderDao<T>();
                            var folder = await folderDao.GetFolderAsync(newFile.ParentId);
                            var folderTitle = await fileSecurity.CanReadAsync(folder) ? folder.Title : null;

                            converter.Result = fileConverterQueue.FileJsonSerializerAsync(entryManager, newFile, folderTitle).Result;
                        }

                        converter.Progress = 100;
                        converter.StopDateTime = DateTime.UtcNow;
                        converter.Processed = "1";

                        if (!string.IsNullOrEmpty(operationResultError))
                        {
                            converter.Error = operationResultError;
                        }
                    }
                }

                logger.DebugCheckConvertFilesStatusIterationEnd();
            }

            fileConverterQueue.SetAllTask<T>(conversionQueue);

        }
        catch (Exception exception)
        {
            logger.ErrorWithException(exception);
        }
    }
}

public static class FileConverterQueueExtension
{
    public static void Register(DIHelper services)
    {
        services.TryAdd<FileConverterQueueScope>();
    }
}

[Scope]
public record FileConverterQueueScope(
    TenantManager TenantManager,
    UserManager UserManager,
    SecurityContext SecurityContext,
    IDaoFactory DaoFactory,
    FileSecurity FileSecurity,
    PathProvider PathProvider,
    SetupInfo SetupInfo,
    FileUtility FileUtility,
    DocumentServiceHelper DocumentServiceHelper,
    DocumentServiceConnector DocumentServiceConnector,
    EntryStatusManager EntryManager,
    FileConverter FileConverter);
