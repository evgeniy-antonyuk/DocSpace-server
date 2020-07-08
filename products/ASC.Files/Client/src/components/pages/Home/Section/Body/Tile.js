import { Checkbox, ContextMenuButton} from "asc-web-components";
import PropTypes from "prop-types";
import React from "react";
import isEqual from "lodash/isEqual";
import styled, {css} from "styled-components";
import BadgesFileTile from './BadgesFileTile';

const FlexBoxStyles = css`
    display: flex;
    flex-direction: row;
    flex-wrap: nowrap;

    justify-content: flex-start;
    align-items: center;
    align-content: center;
`

const StyledTile = styled.div`
  cursor: default;

  min-height: 55px;
  width: 100%;

  ${props => props.isFolder && FlexBoxStyles}

`;

const StyledFileTileTop = styled.div`
  ${FlexBoxStyles}
  justify-content: space-between;
  align-items: baseline;
  background-color:#F8F9F9;
  padding: 13px;
  height: 157px;
`;

const StyledFileTileBottom = styled.div`
  ${FlexBoxStyles}
  padding: 9px 13px;
  min-height: 56px;
  box-sizing: border-box;
`;

const StyledContent = styled.div`
  display: flex;
  flex-basis: 100%;
  min-width: 156px;

  a{
    display: block;
    display: -webkit-box;
    max-width: 400px;
    height: auto;
    margin: 0 auto;
    font-size: 15px;
    line-height: 19px;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: normal;
  }


  @media (max-width: 1024px) {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
`;

const StyledCheckbox = styled.div`
  flex: 0 0 16px;
`;

const StyledElement = styled.div`
  flex: 0 0 auto;
  display: flex;
  margin-right: 8px;
  user-select: none;
`;

const StyledOptionButton = styled.div`
  display: flex;
  width: ${props => props.spacerWidth && props.spacerWidth};

  .expandButton > div:first-child {
    padding-top: 8px;
    padding-bottom: 8px;
    padding-left: 16px;
  }
`;

class Tile extends React.Component {
  shouldComponentUpdate(nextProps) {
    if (this.props.needForUpdate) {
      return this.props.needForUpdate(this.props, nextProps);
    }
    return !isEqual(this.props, nextProps);
  }

  render() {
    //console.log("Row render");
    const {
      checked,
      children,
      contextButtonSpacerWidth,
      contextOptions,
      item,
      element,
      indeterminate,
      onSelect,
      isFolder
    } = this.props;

    const renderCheckbox = Object.prototype.hasOwnProperty.call(
      this.props,
      "checked"
    );

    const renderElement = Object.prototype.hasOwnProperty.call(
      this.props,
      "element"
    );

    const renderContext =
      Object.prototype.hasOwnProperty.call(this.props, "contextOptions") &&
      contextOptions.length > 0;

    const changeCheckbox = e => {
      onSelect && onSelect(e.target.checked, item);
    };

    const getOptions = () => contextOptions;

    return (
      <StyledTile {...this.props}>
        {isFolder 
          ? 
            <>
              {renderCheckbox && (
                <StyledCheckbox>
                  <Checkbox isChecked={checked} isIndeterminate={indeterminate} onChange={changeCheckbox} />
                </StyledCheckbox>
              )}
              {renderElement && !isFolder && <StyledElement>{element}</StyledElement>}
              <StyledContent isFolder={isFolder}>{children}</StyledContent>
              <StyledOptionButton spacerWidth={contextButtonSpacerWidth}>
                {renderContext
                  ? (<ContextMenuButton className="expandButton" directionX="right" getData={getOptions} />)
                  : (<div className="expandButton">{' '}</div>)}
              </StyledOptionButton>
            </>
          :
            <>
              <StyledFileTileTop>
                {renderCheckbox && (
                  <StyledCheckbox>
                    <Checkbox isChecked={checked} isIndeterminate={indeterminate} onChange={changeCheckbox} />
                  </StyledCheckbox>
                )}
                <BadgesFileTile item={item}/>
              </StyledFileTileTop>
              <StyledFileTileBottom>
                {renderElement && !isFolder && <StyledElement>{element}</StyledElement>}
                <StyledContent isFolder={isFolder}>{children}</StyledContent>
                <StyledOptionButton spacerWidth={contextButtonSpacerWidth}>
                  {renderContext
                    ? (<ContextMenuButton className="expandButton" directionX="right" getData={getOptions} />)
                    : (<div className="expandButton">{' '}</div>)}
                </StyledOptionButton>
              </StyledFileTileBottom>
            </>
          }
      </StyledTile>
    );
  }
}

Tile.propTypes = {
  checked: PropTypes.bool,
  children: PropTypes.element,
  className: PropTypes.string,
  contextButtonSpacerWidth: PropTypes.string,
  contextOptions: PropTypes.array,
  data: PropTypes.object,
  element: PropTypes.element,
  id: PropTypes.string,
  indeterminate: PropTypes.bool,
  needForUpdate: PropTypes.func,
  onSelect: PropTypes.func,
  style: PropTypes.oneOfType([PropTypes.object, PropTypes.array]),
  viewAs: PropTypes.string
};

Tile.defaultProps = {
  contextButtonSpacerWidth: '32px'
};

export default Tile;
