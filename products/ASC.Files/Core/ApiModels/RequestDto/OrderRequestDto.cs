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

namespace ASC.Files.Core.ApiModels.RequestDto;

/// <summary>
/// Order request parameters
/// </summary>
public class OrderRequestDto
{
    /// <summary>
    /// Order
    /// </summary>
    [Range(1, int.MaxValue)]
    [JsonConverter(typeof(OrderRequestDtoConverter))]
    public int Order { get; set; }
}

public class OrdersItemRequestDto<T> : OrderRequestDto
{
    public T EntryId { get; set; }
    public FileEntryType EntryType { get; set; }
}

public class OrdersRequestDto<T>
{
    public IEnumerable<OrdersItemRequestDto<T>> Items { get; set; }
}

public class OrderRequestDtoConverter : System.Text.Json.Serialization.JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var orderString = reader.GetString();
        if (!string.IsNullOrEmpty(orderString))
        {
            var path = orderString.Split('.');
            if (int.TryParse(path.Last(), out var pathOrder))
            {
                return pathOrder;
            }
        }

        throw new ArgumentException("order");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// 
/// </summary>
public class OrderFileRequestDto<T>
{
    /// <summary>
    /// File ID
    /// </summary>
    [FromRoute(Name = "fileId")]
    public T FileId { get; set; }

    /// <summary>
    /// Order
    /// </summary>
    [FromBody]
    public OrderRequestDto Order { get; set; }
}

/// <summary>
/// 
/// </summary>
public class OrderFolderRequestDto<T>
{
    /// <summary>
    /// Folder ID
    /// </summary>
    [FromRoute(Name = "folderId")]
    public T FolderId { get; set; }

    /// <summary>
    /// Order
    /// </summary>
    [FromBody]
    public OrderRequestDto Order { get; set; }
}