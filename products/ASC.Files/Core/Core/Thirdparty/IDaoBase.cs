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

namespace ASC.Files.Core.Core.Thirdparty;

[Scope]
internal interface IDaoBase<TFile, TFolder, TItem>
    where TFile : class, TItem
    where TFolder : class, TItem
    where TItem : class
{
    void Init(string pathPrefix, IProviderInfo<TFile, TFolder, TItem> providerInfo);
    string GetName(TItem item);
    string GetId(TItem item);
    bool IsRoot(TFolder folder);
    string MakeThirdId(object entryId);
    string GetParentFolderId(TItem item);
    string MakeId(TItem item);
    string MakeId(string path = null);
    string MakeFolderTitle(TFolder folder);
    string MakeFileTitle(TFile file);
    Folder<string> ToFolder(TFolder folder);
    File<string> ToFile(TFile file);
    Task<Folder<string>> GetRootFolderAsync();
    Task<TFolder> GetFolderAsync(string folderId);
    Task<TFile> GetFileAsync(string fileId);
    Task<IEnumerable<string>> GetChildrenAsync(string folderId);
    Task<List<TItem>> GetItemsAsync(string parentId, bool? folder = null);
    Task<string> GetAvailableTitleAsync(string requestTitle, string parentFolderId, Func<string, string, Task<bool>> isExist);
    bool CheckInvalidFilter(FilterType filterType);
    Task<string> MappingIDAsync(string id, bool saveIfNotExist = false);
}
