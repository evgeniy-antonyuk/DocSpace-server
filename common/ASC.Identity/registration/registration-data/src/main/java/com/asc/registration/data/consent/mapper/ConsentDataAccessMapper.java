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
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.registration.data.consent.mapper;

import com.asc.common.core.domain.entity.Consent;
import com.asc.common.core.domain.value.ConsentId;
import com.asc.common.core.domain.value.enums.ConsentStatus;
import com.asc.common.data.consent.entity.ConsentEntity;
import com.asc.common.data.scope.entity.ScopeEntity;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.entity.ClientConsent;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;

/**
 * Mapper class to convert between {@link ConsentEntity} data access objects and {@link
 * ClientConsent} domain objects.
 */
@Component
@RequiredArgsConstructor
public class ConsentDataAccessMapper {

  /**
   * Converts a {@link ConsentEntity} and {@link Client} to a {@link ClientConsent} domain object.
   *
   * @param entity the data access object to convert
   * @param client the client domain object associated with the consent
   * @return the converted domain object
   */
  public ClientConsent toClientConsent(ConsentEntity entity, Client client) {
    return new ClientConsent(
        client,
        Consent.Builder.builder()
            .id(new ConsentId(entity.getRegisteredClientId(), entity.getPrincipalId()))
            .scopes(
                entity.getScopes().stream().map(ScopeEntity::getName).collect(Collectors.toSet()))
            .modifiedOn(entity.getModifiedAt())
            .status(entity.isInvalidated() ? ConsentStatus.INVALIDATED : ConsentStatus.ACTIVE)
            .build());
  }
}
