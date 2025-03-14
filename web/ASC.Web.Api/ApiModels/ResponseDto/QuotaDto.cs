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

namespace ASC.Web.Api.ApiModels.ResponseDto;

public class QuotaDto
{
    /// <summary>
    /// ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Price
    /// </summary>
    public PriceDto Price { get; set; }

    /// <summary>
    /// Specifies if the quota is nonprofit or not
    /// </summary>
    public bool NonProfit { get; set; }

    /// <summary>
    /// Specifies if the quota is free or not
    /// </summary>
    public bool Free { get; set; }

    /// <summary>
    /// Specifies if the quota is trial or not
    /// </summary>
    public bool Trial { get; set; }

    /// <summary>
    /// List of quota features
    /// </summary>
    public IEnumerable<TenantQuotaFeatureDto> Features { get; set; }

    /// <summary>
    /// User quota
    /// </summary>
    public TenantEntityQuotaSettings UsersQuota {  get; set; }

    /// <summary>
    /// Room quota
    /// </summary>
    public TenantEntityQuotaSettings RoomsQuota {  get; set; }

    /// <summary>
    /// Tenant custom quota
    /// </summary>
    public TenantQuotaSettings TenantCustomQuota { get; set; }
}

public class TenantQuotaFeatureDto : IEquatable<TenantQuotaFeatureDto>
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Image URL
    /// </summary>
    public string Image { get; set; }

    /// <summary>
    /// Value
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Used feature parameters
    /// </summary>
    public FeatureUsedDto Used { get; set; }

    /// <summary>
    /// Price title
    /// </summary>
    public string PriceTitle { get; set; }

    public bool Equals(TenantQuotaFeatureDto other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id;
    }

    public override bool Equals(object obj) => Equals(obj as TenantQuotaFeatureDto);
    public override int GetHashCode() => Id.GetHashCode();
}

public class PriceDto
{
    /// <summary>
    /// Value
    /// </summary>
    [SwaggerSchemaCustom(Example = 10.0)]
    public decimal? Value { get; set; }

    /// <summary>
    /// Currency symbol
    /// </summary>
    public string CurrencySymbol { get; set; }
}

public class FeatureUsedDto
{
    /// <summary>
    /// Value
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; }
}