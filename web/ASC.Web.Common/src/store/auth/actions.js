import { default as api } from "../../api";
import { isDesktopClient } from "./selectors";
import { checkPwd, regDesktop, logout as logoutDesktop } from "../../desktop/";

export const LOGIN_POST = "LOGIN_POST";
export const SET_CURRENT_USER = "SET_CURRENT_USER";
export const SET_MODULES = "SET_MODULES";
export const SET_SETTINGS = "SET_SETTINGS";
export const SET_IS_LOADED = "SET_IS_LOADED";
export const LOGOUT = "LOGOUT";
export const SET_PASSWORD_SETTINGS = "SET_PASSWORD_SETTINGS";
export const SET_NEW_EMAIL = "SET_NEW_EMAIL";
export const SET_PORTAL_CULTURES = "SET_PORTAL_CULTURES";
export const SET_PORTAL_LANGUAGE_AND_TIME = "SET_PORTAL_LANGUAGE_AND_TIME";
export const SET_TIMEZONES = "SET_TIMEZONES";
export const SET_CURRENT_PRODUCT_ID = "SET_CURRENT_PRODUCT_ID";
export const SET_CURRENT_PRODUCT_HOME_PAGE = "SET_CURRENT_PRODUCT_HOME_PAGE";
export const SET_GREETING_SETTINGS = "SET_GREETING_SETTINGS";
export const SET_CUSTOM_NAMES = "SET_CUSTOM_NAMES";
export const SET_WIZARD_COMPLETED = "SET_WIZARD_COMPLETED";
export const GET_ENCRYPTION_KEYS = "GET_ENCRYPTION_KEYS";
export const SET_IS_ENCRYPTION_SUPPORT = "SET_IS_ENCRYPTION_SUPPORT";

export function setCurrentUser(user) {
  return {
    type: SET_CURRENT_USER,
    user,
  };
}

export function setModules(modules) {
  return {
    type: SET_MODULES,
    modules,
  };
}

export function setSettings(settings) {
  return {
    type: SET_SETTINGS,
    settings,
  };
}

export function setIsLoaded(isLoaded) {
  return {
    type: SET_IS_LOADED,
    isLoaded,
  };
}

export function setLogout() {
  return {
    type: LOGOUT,
  };
}

export function setPasswordSettings(passwordSettings) {
  return {
    type: SET_PASSWORD_SETTINGS,
    passwordSettings,
  };
}

export function setNewEmail(email) {
  return {
    type: SET_NEW_EMAIL,
    email,
  };
}

export function setPortalCultures(cultures) {
  return {
    type: SET_PORTAL_CULTURES,
    cultures,
  };
}

export function setPortalLanguageAndTime(newSettings) {
  return {
    type: SET_PORTAL_LANGUAGE_AND_TIME,
    newSettings,
  };
}

export function setTimezones(timezones) {
  return {
    type: SET_TIMEZONES,
    timezones,
  };
}

export function setCurrentProductId(currentProductId) {
  return {
    type: SET_CURRENT_PRODUCT_ID,
    currentProductId,
  };
}

export function setCurrentProductHomePage(homepage) {
  return {
    type: SET_CURRENT_PRODUCT_HOME_PAGE,
    homepage,
  };
}

export function setGreetingSettings(title) {
  return {
    type: SET_GREETING_SETTINGS,
    title,
  };
}

export function setCustomNames(customNames) {
  return {
    type: SET_CUSTOM_NAMES,
    customNames,
  };
}

export function setWizardComplete() {
  return {
    type: SET_WIZARD_COMPLETED,
  };
}

export function receiveEncryptionKeys(keys) {
  return {
    type: GET_ENCRYPTION_KEYS,
    keys,
  };
}

export function setIsEncryptionSupport(isSupport) {
  return {
    type: SET_IS_ENCRYPTION_SUPPORT,
    isSupport,
  };
}

export function getUser(dispatch) {
  return api.people
    .getUser()
    .then((user) => {
      //window.AscDesktopEditor && regDesktop(user, true);
      dispatch(setCurrentUser(user));
    })
    .catch((err) => {
      console.error(err);
      dispatch(setCurrentUser({}));
    });
}

export function getPortalSettings(dispatch) {
  return api.settings.getSettings().then((settings) => {
    const { passwordHash: hashSettings, ...otherSettings } = settings;
    dispatch(
      setSettings(
        hashSettings ? { ...otherSettings, hashSettings } : otherSettings
      )
    );

    otherSettings.nameSchemaId &&
      getCurrentCustomSchema(dispatch, otherSettings.nameSchemaId);
  });
}
export function getCurrentCustomSchema(dispatch, id) {
  return api.settings
    .getCurrentCustomSchema(id)
    .then((customNames) => dispatch(setCustomNames(customNames)));
}

export function getModules(dispatch) {
  return api.modules
    .getModulesList()
    .then((modules) => dispatch(setModules(modules)));
}

export const loadInitInfo = (dispatch) => {
  return getPortalSettings(dispatch).then(() => getModules(dispatch));
};

export function getUserInfo(dispatch) {
  return getUser(dispatch).finally(() => loadInitInfo(dispatch));
}

export function login(user, hash) {
  return (dispatch) => {
    return api.user
      .login(user, hash)
      .then(() => {
        dispatch(setIsLoaded(false));
      })
      .then(() => {
        getUserInfo(dispatch);
        getEncryptionKeys(dispatch);
      });
  };
}

export function logout() {
  return (dispatch, getState) => {
    const state = getState();
    const isDesktop = isDesktopClient(state);
    return api.user
      .logout()
      .then(() => {
        isDesktop && logoutDesktop();
        dispatch(setLogout());
      })
      .then(() => dispatch(setIsLoaded(true)));
  };
}

export function getPortalCultures(dispatch = null) {
  return dispatch
    ? api.settings.getPortalCultures().then((cultures) => {
        dispatch(setPortalCultures(cultures));
      })
    : (dispatch) => {
        return api.settings.getPortalCultures().then((cultures) => {
          dispatch(setPortalCultures(cultures));
        });
      };
}

export function getPortalPasswordSettings(dispatch, confirmKey = null) {
  return api.settings.getPortalPasswordSettings(confirmKey).then((settings) => {
    dispatch(setPasswordSettings(settings));
  });
}

export const reloadPortalSettings = () => {
  return (dispatch) => getPortalSettings(dispatch);
};

export function setEncryptionKeys(keys) {
  return (dispatch) => {
    return api.files.setEncryptionKeys(keys);
  };
}

export function getEncryptionKeys(dispatch) {
  return api.files
    .getEncryptionKeys()
    .then((res) => {
      (res && dispatch(receiveEncryptionKeys(res))) ||
        dispatch(receiveEncryptionKeys({}));
    })
    .catch((err) => console.error(err));
}

export function getEncryptionAccess(fileId) {
  return (dispatch) => {
    return api.files.getEncryptionAccess(fileId);
  };
}

export function getEncryptionSupport(dispatch) {
  return api.files
    .getEncryptionSupport()
    .then((res) => dispatch(setIsEncryptionSupport(res)))
    .catch((err) => console.error(err));
}
