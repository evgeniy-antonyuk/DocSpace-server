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

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// </summary>
public class TfaRequestsDto
{
    /// <summary>TFA type (None, Sms, or App)</summary>
    /// <type>System.String, System</type>
    public TfaRequestsDtoType? Type { get; set; }

    /// <summary>User ID</summary>
    /// <type>System.Nullable{System.Guid}, System</type>
    /// <example>9924256A-739C-462b-AF15-E652A3B1B6EB</example>
    public Guid? Id { get; set; }

    /// <summary>List of trusted IP addresses</summary>
    /// <type>System.Collections.Generic.List{System.String}, System.Collections.Generic</type>
    public List<string> TrustedIps { get; set; }

    /// <summary>List of users who must use the TFA verification</summary>
    /// <type>System.Collections.Generic.List{System.Guid}, System.Collections.Generic</type>
    /// <example>9924256A-739C-462b-AF15-E652A3B1B6EB</example>
	/// <collection>list</collection>
    public List<Guid> MandatoryUsers { get; set; }

    /// <summary>List of groups who must use the TFA verification</summary>
    /// <type>System.Collections.Generic.List{System.Guid}, System.Collections.Generic</type>
    /// <example>9924256A-739C-462b-AF15-E652A3B1B6EB</example>
	/// <collection>list</collection>
    public List<Guid> MandatoryGroups { get; set; }
}

public enum TfaRequestsDtoType
{
    None = 0,
    Sms = 1,
    App = 2
}

/// <summary>
/// </summary>
public class TfaValidateRequestsDto
{
    /// <summary>TFA code</summary>
    /// <type>System.String, System</type>
    public string Code { get; set; }
}
