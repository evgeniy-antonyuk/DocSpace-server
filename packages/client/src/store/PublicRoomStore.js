import { makeAutoObservable } from "mobx";
import api from "@docspace/common/api";
import { LinkType, ValidationResult } from "../helpers/constants";

class PublicRoomStore {
  externalLinks = [];
  roomTitle = null;
  roomId = null;
  roomStatus = null;
  roomType = null;
  publicRoomKey = null;

  isLoaded = false;
  isLoading = false;

  constructor() {
    makeAutoObservable(this);
  }

  setIsLoading = (isLoading) => {
    this.isLoading = isLoading;
  };

  setRoomData = (data) => {
    const { id, roomType, status, title } = data;

    this.roomTitle = title;
    this.roomId = id;
    this.roomStatus = status;
    this.roomType = roomType;

    if (status === ValidationResult.Ok) this.isLoaded = true;
  };

  getExternalLinks = async (roomId) => {
    const type = 1;
    const externalLinks = await api.rooms.getExternalLinks(roomId, type);

    this.externalLinks = externalLinks;
  };

  setExternalLink = (linkId, data) => {
    const linkIndex = this.externalLinks.findIndex(
      (l) => l.sharedTo.id === linkId
    );
    const dataLink = data.find((l) => l.sharedTo.id === linkId);
    this.externalLinks[linkIndex] = dataLink;
  };

  setExternalLinks = (links) => {
    const externalLinks = links.filter((t) => t.sharedTo.shareLink); // shareLink

    this.externalLinks = externalLinks;
  };

  editExternalLink = (roomId, link) => {
    const linkType = LinkType.External;

    const { id, title, expirationDate, password, disabled, denyDownload } =
      link.sharedTo;

    return api.rooms.editExternalLink(
      roomId,
      id,
      title,
      link.access,
      expirationDate,
      linkType,
      password,
      disabled,
      denyDownload
    );
  };

  validatePublicRoomKey = (key) => {
    this.setIsLoading(true);
    api.rooms
      .validatePublicRoomKey(key)
      .then((res) => {
        this.publicRoomKey = key;
        this.setRoomData(res);
      })
      .finally(() => this.setIsLoading(false));
  };

  validatePublicRoomPassword = (key, passwordHash) => {
    return api.rooms.validatePublicRoomPassword(key, passwordHash);
  };

  get isPublicRoom() {
    return this.isLoaded && window.location.pathname === "/rooms/share";
  }

  get roomLinks() {
    return this.externalLinks.filter(
      (l) =>
        l.sharedTo.shareLink &&
        !l.sharedTo.isTemplate &&
        l.sharedTo.linkType === LinkType.External
    );
  }
}

export default PublicRoomStore;
