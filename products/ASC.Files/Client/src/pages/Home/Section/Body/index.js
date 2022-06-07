import React, { useEffect } from "react";
import { withRouter } from "react-router";
import { withTranslation } from "react-i18next";
import { isMobile, isMobileOnly } from "react-device-detect";

import { observer, inject } from "mobx-react";
import FilesRowContainer from "./RowsView/FilesRowContainer";
import FilesTileContainer from "./TilesView/FilesTileContainer";
import EmptyContainer from "../../../../components/EmptyContainer";
import withLoader from "../../../../HOCs/withLoader";
import TableView from "./TableView/TableContainer";
import withHotkeys from "../../../../HOCs/withHotkeys";
import { Consumer } from "@appserver/components/utils/context";
import { isElementInViewport } from "@appserver/common/utils";

let currentDroppable = null;
let isDragActive = false;

const SectionBodyContent = (props) => {
  const {
    t,
    tReady,
    fileActionId,
    isEmptyFilesList,
    folderId,
    dragging,
    setDragging,
    startDrag,
    setStartDrag,
    setTooltipPosition,
    isRecycleBinFolder,
    moveDragItems,
    viewAs,
    setSelection,
    setBufferSelection,
    tooltipPageX,
    tooltipPageY,
    setHotkeyCaretStart,
    setHotkeyCaret,
    scrollToFolderId,
    setScrollToFolderId,
    filesList,
    fetchMoreFiles,
  } = props;

  useEffect(() => {
    const customScrollElm = document.querySelector(
      "#customScrollBar > .scroll-body"
    );

    if (isMobile) {
      customScrollElm && customScrollElm.scrollTo(0, 0);
    }

    window.addEventListener("mousedown", onMouseDown);
    startDrag && window.addEventListener("mouseup", onMouseUp);
    startDrag && document.addEventListener("mousemove", onMouseMove);

    document.addEventListener("dragover", onDragOver);
    document.addEventListener("dragleave", onDragLeaveDoc);
    document.addEventListener("drop", onDropEvent);

    const scroll = document.getElementById("sectionScroll").childNodes[0];

    scroll.addEventListener("scroll", onScroll);

    return () => {
      window.removeEventListener("mousedown", onMouseDown);
      window.removeEventListener("mouseup", onMouseUp);
      document.removeEventListener("mousemove", onMouseMove);

      document.removeEventListener("dragover", onDragOver);
      document.removeEventListener("dragleave", onDragLeaveDoc);
      document.removeEventListener("drop", onDropEvent);
      scroll.removeEventListener("scroll", onScroll);
    };
  }, [onMouseUp, onMouseMove, startDrag, folderId, viewAs]);

  useEffect(() => {
    if (scrollToFolderId) {
      const newFolder = document.querySelector(
        `div[value='folder_${scrollToFolderId}_draggable']`
      );

      let isInViewport = isElementInViewport(newFolder);

      if (!isInViewport || viewAs === "table") {
        const bodyScroll = isMobileOnly
          ? document.querySelector("#customScrollBar > div")
          : document.querySelector(".section-scroll");

        const rectNewFolder = newFolder.getBoundingClientRect();
        const count =
          viewAs === "table"
            ? filesList.findIndex((elem) => elem.id === scrollToFolderId) * 40
            : rectNewFolder.bottom - window.innerHeight + 300;

        bodyScroll.scrollTo(0, count);
      }
      setScrollToFolderId(null);
    }
  }, [scrollToFolderId]);

  const onScroll = (e) => {
    if (window.innerHeight + e.target.scrollTop + 1 > e.target.scrollHeight) {
      fetchMoreFiles();
    }
  };

  const onMouseDown = (e) => {
    if (
      (e.target.closest(".scroll-body") &&
        !e.target.closest(".files-item") &&
        !e.target.closest(".not-selectable") &&
        !e.target.closest(".info-panel") &&
        !e.target.closest(".table-container_group-menu")) ||
      e.target.closest(".files-main-button") ||
      e.target.closest(".add-button") ||
      e.target.closest(".search-input-block")
    ) {
      setSelection([]);
      setBufferSelection(null);
      setHotkeyCaretStart(null);
      setHotkeyCaret(null);
    }
  };

  const onMouseMove = (e) => {
    if (
      Math.abs(e.pageX - tooltipPageX) < 5 &&
      Math.abs(e.pageY - tooltipPageY) < 5
    ) {
      return false;
    }

    isDragActive = true;
    if (!dragging) {
      document.body.classList.add("drag-cursor");
      setDragging(true);
    }

    setTooltipPosition(e.pageX, e.pageY);
    const wrapperElement = document.elementFromPoint(e.clientX, e.clientY);
    if (!wrapperElement) {
      return;
    }

    const droppable = wrapperElement.closest(".droppable");
    if (currentDroppable !== droppable) {
      if (currentDroppable) {
        if (viewAs === "table") {
          const value = currentDroppable.getAttribute("value");
          const classElements = document.getElementsByClassName(value);

          for (let cl of classElements) {
            cl.classList.remove("droppable-hover");
          }
        } else {
          currentDroppable.classList.remove("droppable-hover");
        }
      }
      currentDroppable = droppable;

      if (currentDroppable) {
        if (viewAs === "table") {
          const value = currentDroppable.getAttribute("value");
          const classElements = document.getElementsByClassName(value);

          // add check for column with width = 0, because without it dark theme d`n`d have bug color
          // 30 - it`s column padding
          for (let cl of classElements) {
            if (cl.clientWidth - 30) {
              cl.classList.add("droppable-hover");
            }
          }
        } else {
          currentDroppable.classList.add("droppable-hover");
          currentDroppable = droppable;
        }
      }
    }
  };

  const onMouseUp = (e) => {
    document.body.classList.remove("drag-cursor");

    const treeElem = e.target.closest(".tree-drag");
    const treeDataValue = treeElem?.dataset?.value;
    const splitValue = treeDataValue && treeDataValue.split(" ");
    const isDragging = splitValue && splitValue.includes("dragging");
    const treeValue = isDragging ? splitValue[0] : null;
    const treeProvider = splitValue && splitValue[splitValue.length - 1];

    const elem = e.target.closest(".droppable");
    const title = elem && elem.dataset.title;
    const value = elem && elem.getAttribute("value");
    if ((!value && !treeValue) || isRecycleBinFolder || !isDragActive) {
      setDragging(false);
      setStartDrag(false);
      isDragActive = false;
      return;
    }

    const folderId = value ? value.split("_")[1] : treeValue;
    const providerKey = value ? value.split("_")[2].trim() : treeProvider;

    setStartDrag(false);
    setDragging(false);
    onMoveTo(folderId, title, providerKey);
    isDragActive = false;
    return;
  };

  const onMoveTo = (destFolderId, title, providerKey) => {
    const id = isNaN(+destFolderId) ? destFolderId : +destFolderId;
    moveDragItems(id, title, providerKey, {
      copy: t("Common:CopyOperation"),
      move: t("Translations:MoveToOperation"),
    }); //TODO: then catch
  };

  const onDropEvent = () => {
    setDragging(false);
  };

  const onDragOver = (e) => {
    e.preventDefault();
    if (
      e.dataTransfer.items.length > 0 &&
      e.dataTransfer.dropEffect !== "none"
    ) {
      setDragging(true);
    }
  };

  const onDragLeaveDoc = (e) => {
    e.preventDefault();
    if (!e.relatedTarget || !e.dataTransfer.items.length) {
      setDragging(false);
    }
  };

  //console.log("Files Home SectionBodyContent render", props);

  return (
    <Consumer>
      {(context) =>
        (!fileActionId && isEmptyFilesList) || null ? (
          <>
            <EmptyContainer />
          </>
        ) : viewAs === "tile" ? (
          <>
            <FilesTileContainer sectionWidth={context.sectionWidth} t={t} />
          </>
        ) : viewAs === "table" ? (
          <>
            <TableView sectionWidth={context.sectionWidth} tReady={tReady} />
          </>
        ) : (
          <>
            <FilesRowContainer
              sectionWidth={context.sectionWidth}
              tReady={tReady}
            />
          </>
        )
      }
    </Consumer>
  );
};

export default inject(
  ({
    filesStore,
    selectedFolderStore,
    treeFoldersStore,
    filesActionsStore,
  }) => {
    const {
      fileActionStore,
      isEmptyFilesList,
      dragging,
      setDragging,
      viewAs,
      setTooltipPosition,
      startDrag,
      setStartDrag,
      setSelection,
      tooltipPageX,
      tooltipPageY,
      setBufferSelection,
      setHotkeyCaretStart,
      setHotkeyCaret,
      scrollToFolderId,
      setScrollToFolderId,
      filesList,
      fetchMoreFiles,
    } = filesStore;

    return {
      dragging,
      startDrag,
      setStartDrag,
      fileActionId: fileActionStore.id,
      isEmptyFilesList,
      setDragging,
      folderId: selectedFolderStore.id,
      setTooltipPosition,
      isRecycleBinFolder: treeFoldersStore.isRecycleBinFolder,
      moveDragItems: filesActionsStore.moveDragItems,
      viewAs,
      setSelection,
      setBufferSelection,
      tooltipPageX,
      tooltipPageY,
      setHotkeyCaretStart,
      setHotkeyCaret,
      scrollToFolderId,
      setScrollToFolderId,
      filesList,
      fetchMoreFiles,
    };
  }
)(
  withRouter(
    withTranslation(["Home", "Common", "Translations"])(
      withLoader(withHotkeys(observer(SectionBodyContent)))()
    )
  )
);
