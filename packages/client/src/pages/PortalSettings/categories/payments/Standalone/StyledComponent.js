import styled from "styled-components";

import { tablet, hugeMobile } from "@docspace/components/utils/device";

const StyledComponent = styled.div`
  .payments_file-input {
    max-width: 350px;
    margin: 16px 0;
  }
  .payments_license-description {
    margin-top: 12px;
  }
`;
const StyledButtonComponent = styled.div`
  margin: 16px 0;
  button {
    @media ${tablet} {
      max-width: 100%;

      height: 40px;
    }
    @media ${hugeMobile} {
      width: 100%;
    }
  }
`;
const StyledContactComponent = styled.div`
  margin-top: 20px;
  max-width: 504px;
  .payments_contact {
    display: flex;
    width: 100%;
    p {
      margin-right: 4px;
    }
  }
`;

const StyledEnterpriseComponent = styled.div`
  margin-bottom: 35px;
  margin-top: 12px;

  .payments_subscription {
    margin-top: 12px;
  }
  .payments_renew-subscription,
  .payments_subscription {
    max-width: 660px;
  }
  .payments_renew-subscription {
    margin-top: 12px;
  }
  .payments_support {
    max-width: 503px;
  }

  .payments_subscription-expired {
    max-width: fit-content;
    border: 1px solid
      ${(props) => props.theme.client.settings.payment.warningColor};
    border-radius: 3px;
    color: ${(props) => props.theme.client.settings.payment.warningColor};
    padding: 5px 8px;
    margin-top: 8px;
  }
`;

const StyledBenefitsBody = styled.div`
  margin: 20px 0;
  border-radius: 12px;
  border: ${(props) => props.theme.client.settings.payment.border};
  max-width: 660px;

  padding: 24px;

  background: ${(props) =>
    props.theme.client.settings.payment.backgroundBenefitsColor};

  .benefits-title {
    margin-bottom: 20px;
  }
  .payments-benefits {
    display: grid;
    grid-template-columns: 24px 1fr;
    gap: 10px;
    .benefits-svg {
      display: flex;
      align-items: flex-start;
      padding-top: 4px;
    }
    .benefits-description {
      p:first-child {
        margin-bottom: 2px;
      }
    }
  }
  .payments-benefits ~ .payments-benefits {
    margin-top: 12px;
  }
`;
export {
  StyledComponent,
  StyledContactComponent,
  StyledEnterpriseComponent,
  StyledButtonComponent,
  StyledBenefitsBody,
};
