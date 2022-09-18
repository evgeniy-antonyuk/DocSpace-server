import styled, { css } from "styled-components";
import { mobile, tablet } from "../utils/device";
import NoUserSelect from "../utils/commonStyles";

const isRootFolderStyles = css`
  .ec-image {
    height: 72px;
    margin-top: 21px;
  }

  .ec-header {
    padding-top: 16px;
    font-size: 16px;
    background: ${(props) => !!props.descriptionText && "red"};
  }

  .ec-desc {
    margin-top: -2px;
    line-height: 16px;
  }

  .empty-folder_container-links {
    align-items: center;
    margin: 17px 0 !important;
  }

  @media (min-width: 600px) {
    .ec-image {
      height: 100px;
    }

    .ec-header {
      padding-top: 21px;
    }
  }

  @media (min-width: 1024px) {
    .ec-desc {
      max-width: 618px;
    }
  }
`;

const EmptyContentBody = styled.div`
  margin: 0 auto;
  padding: 64px 0;
  grid-template-columns: 150px 1fr;

  @media ${tablet} {
    grid-template-columns: none;
  }

  display: grid;
  grid-template-areas:
    "img headerText"
    ${(props) => props.subheadingText && `"img subheadingText"`}
    ${(props) => props.descriptionText && `"img descriptionText"`}
    "img button";

  grid-column-gap: 16px;
  grid-row-gap: 10px;
  max-width: 800px;
  grid-template-rows: max-content;
  .ec-image {
    grid-area: img;
    margin: 16px 0 0 auto;
    ${NoUserSelect}
  }

  .ec-header {
    grid-area: headerText;
    padding-top: 16px;
    @media (max-width: 375px) {
      margin-top: 5px;
    }
  }

  .ec-subheading {
    grid-area: subheadingText;
    margin-top: -1px;
  }

  .ec-desc {
    grid-area: descriptionText;
    line-height: 18px;
    margin-top: 2px;
  }

  .ec-buttons {
    grid-area: button;
    margin-top: -1px;
  }

  @media (max-width: 600px) {
    ${(props) =>
      props.isRootStyles &&
      css`
        grid-template-areas:
          "img img img"
          "headerText headerText headerText"
          ${(props) =>
            props.subheadingText &&
            `"subheadingText subheadingText subheadingText"`}
          ${(props) =>
            props.descriptionText &&
            `"descriptionText descriptionText descriptionText"`}
          "button button button";

        .ec-header {
          padding-top: 0px;
        }
        .ec-header,
        .ec-subheading,
        .ec-desc,
        .ec-image,
        .ec-buttons {
          padding-left: 16px;
        }

        .ec-image {
          height: 75px;
          margin-left: 0;
        }
      `}
  }

  @media (orientation: portrait) {
    @media (max-width: 768px) {
      padding-top: 0px;
      max-width: 700px;

      .ec-image {
        max-height: 100px;
      }
    }

    @media (max-width: 428px) {
      grid-template-areas:
        "img img img"
        "headerText headerText headerText"
        ${(props) =>
          props.subheadingText &&
          `"subheadingText subheadingText subheadingText"`}
        ${(props) =>
          props.descriptionText &&
          `"descriptionText descriptionText descriptionText"`}
        "button button button";

      .ec-header {
        padding-top: 0px;
      }
      .ec-header,
      .ec-subheading,
      .ec-desc,
      .ec-image,
      .ec-buttons {
        padding-left: 16px;
      }

      .ec-image {
        height: 75px;
        margin-left: 0;
      }
    }
  }

  ${(props) => props.isRootStyles && `${isRootFolderStyles}`}
`;

const EmptyContentImage = styled.img.attrs((props) => ({
  src: props.imageSrc,
  alt: props.imageAlt,
}))`
  background: no-repeat 0 0 transparent;
`;

export { EmptyContentBody, EmptyContentImage };
