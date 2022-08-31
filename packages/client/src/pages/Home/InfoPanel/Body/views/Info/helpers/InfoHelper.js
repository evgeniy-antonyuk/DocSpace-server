import React from "react";

import { LANGUAGE } from "@docspace/common/constants";

import getCorrectDate from "@docspace/components/utils/getCorrectDate";

import Link from "@docspace/components/link";
import Text from "@docspace/components/text";
import Tag from "@docspace/components/tag";

import {
  connectedCloudsTypeTitleTranslation,
  getDefaultRoomName,
  getFileTypeName,
} from "@docspace/client/src/helpers/filesUtils";

// Property Content Components

const styledText = (text) => (
  <Text truncate className="property-content">
    {text}
  </Text>
);

const styledLink = (text, href) => (
  <Link
    isTextOverflow
    className="property-content"
    href={href}
    isHovered={true}
  >
    {text}
  </Link>
);

const styledTagList = (tags) => (
  <div className="property-tag_list">
    {tags.map((tag) => (
      <Tag className="property-tag" label={tag} />
    ))}
  </div>
);

// Functional Helpers

const decodeString = (str) => {
  const regex = /&#([0-9]{1,4});/gi;
  return str
    ? str.replace(regex, (match, numStr) => String.fromCharCode(+numStr))
    : "...";
};

const parseAndFormatDate = (date, personal, culture) => {
  const locale = personal ? localStorage.getItem(LANGUAGE) : culture;
  const correctDate = getCorrectDate(locale, date);
  return correctDate;
};

class InfoHelper {
  constructor(t, item, personal, culture) {
    this.t = t;
    this.item = item;
    this.personal = personal;
    this.culture = culture;
  }

  getNeededProperties = () => {
    return this.item.isRoom
      ? [
          "Owner",
          "Storage Type",
          "Storage account",
          "Type",
          "Content",
          "Date modified",
          "Last modified by",
          "Creation date",
          "Tags",
        ]
      : this.item.isFolder
      ? [
          "Owner",
          "Location",
          "Type",
          "Content",
          "Date modified",
          "Last modified by",
          "Creation date",
        ]
      : [
          "Owner",
          "Location",
          "Type",
          "File extension",
          "Size",
          "Date modified",
          "Last modified by",
          "Creation date",
        ];
  };

  getPropertyContent = (propertyId) => {
    switch (propertyId) {
      case "Owner":
        return getItemOwner();
      case "Location":
        return getItemLocation();

      case "Type":
        return getItemType();
      case "Storage Type":
        return getItemStorageType();
      case "Storage account":
        return getItemStorageAccount();

      case "File extension":
        return getItemFileExtention();

      case "Content":
        return getItemContent();
      case "Size":
        return getItemSize();
      case "Tags":
        return getItemTags();

      case "Date modified":
        return getItemDateModified();
      case "Last modified by":
        return getItemLastModifiedBy();
      case "Creation date":
        return getItemCreationDate();
    }
  };

  getItemOwner = () => {
    return this.personal
      ? styledText(decodeString(selectedItem.createdBy?.displayName))
      : styledLink(
          decodeString(selectedItem.createdBy?.displayName),
          selectedItem.createdBy?.profileUrl
        );
  };

  getItemLocation = () => {
    return styledText("...");
  };

  getItemType = () => {
    if (this.item.isRoom)
      return styledText(getDefaultRoomName(this.item.roomType, this.t));
    return styledText(getFileTypeName(this.item.fileType, this.t));
  };

  getItemFileExtention = () => {
    return styledText(
      this.item.fileExst ? this.item.fileExst.split(".")[1].toUpperCase() : "-"
    );
  };

  getItemStorageType = () => {
    return styledText(
      connectedCloudsTypeTitleTranslation(this.item.providerKey, this.t)
    );
  };

  getItemStorageAccount = () => {
    return styledText("...");
  };

  getItemContent = () => {
    return styledText(
      `${this.t("Translations:Folders")}: ${this.item.foldersCount} | ${this.t(
        "Translations:Files"
      )}: ${this.item.filesCount}`
    );
  };

  getItemSize = () => {
    return styledText(this.item.contentLength);
  };

  getItemTags = () => {
    return styledTagList(selectedItem.tags);
  };

  getItemDateModified = () => {
    return styledText(
      parseAndFormatDate(selectedItem.updated, this.personal, this.culture)
    );
  };

  getItemLastModifiedBy = () => {
    return personal
      ? styledText(decodeString(selectedItem.updatedBy?.displayName))
      : styledLink(
          decodeString(selectedItem.updatedBy?.displayName),
          selectedItem.updatedBy?.profileUrl
        );
  };

  getItemCreationDate = () => {
    return styledText(
      parseAndFormatDate(selectedItem.created, this.personal, this.culture)
    );
  };
}

export default InfoHelper;
