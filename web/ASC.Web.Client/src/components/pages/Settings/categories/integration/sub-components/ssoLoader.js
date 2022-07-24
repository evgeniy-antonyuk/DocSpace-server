import React from "react";
import styled from "styled-components";
import Loaders from "@appserver/common/components/Loaders";

const StyledLoader = styled.div`
  .submenu {
    display: flex;
    gap: 20px;
    margin-bottom: 20px;
    .item {
      width: 72px;
    }
  }

  .description {
    max-width: 700px;
    margin-bottom: 20px;
  }

  .category {
    margin-top: 24px;
    width: 238px;
  }

  hr {
    margin-top: 24px;
    border: none;
    border-top: 1px solid #eceef1;
  }
`;

const SSOLoader = () => {
  return (
    <StyledLoader>
      <div className="submenu">
        <Loaders.Rectangle className="item" height="28px" />
        <Loaders.Rectangle className="item" height="28px" />
      </div>
      <Loaders.Rectangle className="description" height="60px" />
      <Loaders.Rectangle height="64px" />

      <Loaders.Rectangle className="category" height="22px" />
      <hr />
      <Loaders.Rectangle className="category" height="22px" />
    </StyledLoader>
  );
};

export default SSOLoader;
