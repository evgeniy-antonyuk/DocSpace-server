import React from "react";
import { withRouter } from "react-router";
import { inject, observer } from "mobx-react";
import { Trans } from "react-i18next";

import Text from "@docspace/components/text";
import Button from "@docspace/components/button";
import { ColorTheme, ThemeType } from "@docspace/common/components/ColorTheme";

import {
  StyledEnterpriseComponent,
  StyledButtonComponent,
} from "./StyledComponent";
import BenefitsContainer from "./sub-components/BenefitsContainer";

const EnterpriseContainer = (props) => {
  const { buyUrl, salesEmail, t, isSubscriptionExpired, theme } = props;
  const onClickBuy = () => {
    window.open(buyUrl, "_blank");
  };

  const subscriptionDescription = isSubscriptionExpired ? (
    <Text className="payments_subscription-expired" isBold fontSize="14px">
      {t("EnterpriseSubscriptionExpired")}
    </Text>
  ) : (
    <Text as="span" fontWeight={400} fontSize="14px">
      {t("EnterpriseSubscriptionExpiresDate", {
        finalDate: "Tuesday, December 19, 2023.",
      })}
    </Text>
  );

  return (
    <StyledEnterpriseComponent theme={theme}>
      <Text fontWeight={700} fontSize={"16px"}>
        {t("EnterpriseRenewSubscription")}
      </Text>
      <div className="payments_subscription">
        <Text isBold fontSize="14px" as="span">
          {t("EnterpriseEdition")}{" "}
        </Text>
        {subscriptionDescription}
      </div>
      {isSubscriptionExpired && <BenefitsContainer t={t} />}
      <Text
        fontWeight={400}
        fontSize="14px"
        className="payments_renew-subscription"
      >
        {isSubscriptionExpired ? t("BuyLicense") : t("EnterpriseRenewal")}
      </Text>
      <StyledButtonComponent>
        <Button
          label={t("BuyNow")}
          size={"small"}
          primary
          onClick={onClickBuy}
        />
      </StyledButtonComponent>

      <div className="payments_support">
        <Text>
          <Trans i18nKey="EnterprisePersonalRenewal" ns="Payments" t={t}>
            To get your personal renewal terms, contact your dedicated manager
            or write us at
            <ColorTheme
              fontWeight="600"
              target="_blank"
              tag="a"
              href={`mailto:${salesEmail}`}
              themeId={ThemeType.Link}
            >
              {{ email: salesEmail }}
            </ColorTheme>
          </Trans>
        </Text>
      </div>
    </StyledEnterpriseComponent>
  );
};

export default inject(({ auth, payments }) => {
  const { buyUrl, salesEmail } = payments;
  const { settingsStore } = auth;
  const { theme } = settingsStore;
  const isSubscriptionExpired = true;

  return { theme, buyUrl, salesEmail, isSubscriptionExpired };
})(withRouter(observer(EnterpriseContainer)));
