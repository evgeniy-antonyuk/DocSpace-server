import React, { useEffect } from "react";
import {
  BrowserRouter as Router,
  Routes,
  Route,
  Navigate,
} from "react-router-dom";
import { inject, observer } from "mobx-react";
import NavMenu from "./components/NavMenu";
import Main from "./components/Main";
import PrivateRoute from "@docspace/common/components/PrivateRoute";
import PublicRoute from "@docspace/common/components/PublicRoute";
import ErrorBoundary from "@docspace/common/components/ErrorBoundary";
import Layout from "./components/Layout";
import ScrollToTop from "./components/Layout/ScrollToTop";
import Toast from "@docspace/components/toast";
import toastr from "@docspace/components/toast/toastr";
import { getLogoFromPath, updateTempContent } from "@docspace/common/utils";
import { Provider as MobxProvider } from "mobx-react";
import ThemeProvider from "@docspace/components/theme-provider";
import store from "client/store";

import config from "PACKAGE_FILE";
import { I18nextProvider, useTranslation } from "react-i18next";
import i18n from "./i18n";
import AppLoader from "@docspace/common/components/AppLoader";
import Snackbar from "@docspace/components/snackbar";
import moment from "moment";
import ReactSmartBanner from "./components/SmartBanner";
import { useThemeDetector } from "@docspace/common/utils/useThemeDetector";
import { isMobileOnly, isMobile, isIOS, isFirefox } from "react-device-detect";
import IndicatorLoader from "./components/IndicatorLoader";
import DialogsWrapper from "./components/dialogs/DialogsWrapper";
import MainBar from "./components/MainBar";
import { Portal } from "@docspace/components";

const Error404 = React.lazy(() => import("client/Error404"));
const Error401 = React.lazy(() => import("client/Error401"));
const Files = React.lazy(() => import("./pages/Files")); //import("./components/pages/Home"));

const About = React.lazy(() => import("./pages/About"));
const Wizard = React.lazy(() => import("./pages/Wizard"));
const PortalSettings = React.lazy(() => import("./pages/PortalSettings"));

const Confirm = !IS_PERSONAL && React.lazy(() => import("./pages/Confirm"));

const PreparationPortal = React.lazy(() => import("./pages/PreparationPortal"));
const PortalUnavailable = React.lazy(() => import("./pages/PortalUnavailable"));
const FormGallery = React.lazy(() => import("./pages/FormGallery"));

const ErrorUnavailable = React.lazy(() => import("./pages/Errors/Unavailable"));

const PortalSettingsRoute = (props) => {
  return (
    <React.Suspense fallback={<AppLoader />}>
      <ErrorBoundary>
        <PortalSettings {...props} />
      </ErrorBoundary>
    </React.Suspense>
  );
};

const Error404Route = (props) => (
  <React.Suspense fallback={<AppLoader />}>
    <ErrorBoundary>
      <Error404 {...props} />
    </ErrorBoundary>
  </React.Suspense>
);

const Error401Route = (props) => (
  <React.Suspense fallback={<AppLoader />}>
    <ErrorBoundary>
      <Error401 {...props} />
    </ErrorBoundary>
  </React.Suspense>
);
const FilesRoute = (props) => (
  <React.Suspense fallback={<AppLoader />}>
    <ErrorBoundary>
      <Files {...props} />
    </ErrorBoundary>
  </React.Suspense>
);

const ConfirmRoute =
  !IS_PERSONAL &&
  ((props) => (
    <React.Suspense fallback={<AppLoader />}>
      <ErrorBoundary>
        <Confirm {...props} />
      </ErrorBoundary>
    </React.Suspense>
  ));

const PreparationPortalRoute = (props) => (
  <React.Suspense fallback={<AppLoader />}>
    <ErrorBoundary>
      <PreparationPortal {...props} />
    </ErrorBoundary>
  </React.Suspense>
);

const PortalUnavailableRoute = (props) => (
  <React.Suspense fallback={<AppLoader />}>
    <ErrorBoundary>
      <PortalUnavailable {...props} />
    </ErrorBoundary>
  </React.Suspense>
);

const AboutRoute = (props) => (
  <React.Suspense fallback={<AppLoader />}>
    <ErrorBoundary>
      <About {...props} />
    </ErrorBoundary>
  </React.Suspense>
);

const WizardRoute = (props) => (
  <React.Suspense fallback={<AppLoader />}>
    <ErrorBoundary>
      <Wizard {...props} />
    </ErrorBoundary>
  </React.Suspense>
);

const FormGalleryRoute = (props) => (
  <React.Suspense fallback={<AppLoader />}>
    <ErrorBoundary>
      <FormGallery {...props} />
    </ErrorBoundary>
  </React.Suspense>
);

const Shell = ({ items = [], page = "home", ...rest }) => {
  const {
    isLoaded,
    loadBaseInfo,

    isDesktop,
    language,
    FirebaseHelper,
    // personal,
    setCheckedMaintenance,
    socketHelper,
    setPreparationPortalDialogVisible,
    isBase,
    setTheme,
    setMaintenanceExist,
    roomsMode,
    setSnackbarExist,
    userTheme,
    //user,
    whiteLabelLogoUrls,
  } = rest;

  useEffect(() => {
    try {
      loadBaseInfo();
    } catch (err) {
      toastr.error(err);
    }
  }, []);

  useEffect(() => {
    if (!whiteLabelLogoUrls) return;
    const favicon = getLogoFromPath(whiteLabelLogoUrls[2]?.path?.light);

    if (!favicon) return;

    const link = document.querySelector("#favicon-icon");
    link.href = favicon;

    const shortcutIconLink = document.querySelector("#favicon");
    shortcutIconLink.href = favicon;

    const appleIconLink = document.querySelector(
      "link[rel~='apple-touch-icon']"
    );

    if (appleIconLink) appleIconLink.href = favicon;

    const androidIconLink = document.querySelector(
      "link[rel~='android-touch-icon']"
    );
    if (androidIconLink) androidIconLink.href = favicon;
  }, [whiteLabelLogoUrls]);

  useEffect(() => {
    socketHelper.emit({
      command: "subscribe",
      data: { roomParts: "backup-restore" },
    });
    socketHelper.on("restore-backup", () => {
      setPreparationPortalDialogVisible(true);
    });
  }, [socketHelper]);

  const { t, ready } = useTranslation(["Common", "SmartBanner"]);

  let snackTimer = null;
  let fbInterval = null;
  let lastCampaignStr = null;
  const LS_CAMPAIGN_DATE = "maintenance_to_date";
  const DATE_FORMAT = "YYYY-MM-DD";
  const SNACKBAR_TIMEOUT = 10000;

  const setSnackBarTimer = (campaign) => {
    snackTimer = setTimeout(() => showSnackBar(campaign), SNACKBAR_TIMEOUT);
  };

  const clearSnackBarTimer = () => {
    if (!snackTimer) return;

    clearTimeout(snackTimer);
    snackTimer = null;
  };

  const showSnackBar = (campaign) => {
    clearSnackBarTimer();

    let skipMaintenance;

    const { fromDate, toDate, desktop } = campaign;

    console.log(
      `FB: 'bar/maintenance' desktop=${desktop} fromDate=${fromDate} toDate=${toDate}`
    );

    if (!campaign || !fromDate || !toDate) {
      console.log("Skip snackBar by empty campaign params");
      skipMaintenance = true;
    }

    const to = moment(toDate).local();

    const watchedCampaignDateStr = localStorage.getItem(LS_CAMPAIGN_DATE);

    const campaignDateStr = to.format(DATE_FORMAT);
    if (campaignDateStr == watchedCampaignDateStr) {
      console.log("Skip snackBar by already watched");
      skipMaintenance = true;
    }

    const from = moment(fromDate).local();
    const now = moment();

    if (now.isBefore(from)) {
      setSnackBarTimer(campaign);

      Snackbar.close();
      console.log(`Show snackBar has been delayed for 1 minute`, now);
      skipMaintenance = true;
    }

    if (now.isAfter(to)) {
      console.log("Skip snackBar by current date", now);
      Snackbar.close();
      skipMaintenance = true;
    }

    if (isDesktop && !desktop) {
      console.log("Skip snackBar by desktop", desktop);
      Snackbar.close();
      skipMaintenance = true;
    }

    if (skipMaintenance) {
      setCheckedMaintenance(true);
      return;
    }

    setSnackBarTimer(campaign);

    if (!document.getElementById("main-bar")) return;

    const campaignStr = JSON.stringify(campaign);
    // let skipRender = lastCampaignStr === campaignStr;

    const hasChild = document.getElementById("main-bar").hasChildNodes();

    if (hasChild) return;

    lastCampaignStr = campaignStr;

    const targetDate = to.locale(language).format("LL");

    const barConfig = {
      parentElementId: "main-bar",
      headerText: t("Attention"),
      text: `${t("BarMaintenanceDescription", {
        targetDate: targetDate,
        productName: "ONLYOFFICE Personal",
      })} ${t("BarMaintenanceDisclaimer")}`,
      isMaintenance: true,
      clickAction: () => {
        setMaintenanceExist(false);
        setSnackbarExist(false);
        Snackbar.close();
        localStorage.setItem(LS_CAMPAIGN_DATE, to.format(DATE_FORMAT));
      },
      opacity: 1,
      onLoad: () => {
        setCheckedMaintenance(true);
        setSnackbarExist(true);
        setMaintenanceExist(true);
      },
    };

    Snackbar.show(barConfig);
  };

  const fetchMaintenance = () => {
    try {
      if (!FirebaseHelper.isEnabled) return;

      FirebaseHelper.checkMaintenance()
        .then((campaign) => {
          console.log("checkMaintenance", campaign);
          if (!campaign) {
            setCheckedMaintenance(true);
            clearSnackBarTimer();
            Snackbar.close();
            return;
          }

          setTimeout(() => showSnackBar(campaign), 1000);
        })
        .catch((err) => {
          console.error(err);
        });
    } catch (e) {
      console.log(e);
    }
  };

  const fetchBanners = () => {
    if (!FirebaseHelper.isEnabled) return;

    FirebaseHelper.checkBar()
      .then((bar) => {
        localStorage.setItem("bar", bar);
      })
      .catch((err) => {
        console.log(err);
      });

    FirebaseHelper.checkCampaigns()
      .then((campaigns) => {
        localStorage.setItem("campaigns", campaigns);
      })
      .catch((err) => {
        console.error(err);
      });
  };

  useEffect(() => {
    if (!isLoaded) return;

    updateTempContent();

    if (!FirebaseHelper.isEnabled) {
      setCheckedMaintenance(true);
      localStorage.setItem("campaigns", "");
      return;
    }

    fetchMaintenance();
    fetchBanners();
    fbInterval = setInterval(fetchMaintenance, 60000);
    const bannerInterval = setInterval(fetchBanners, 60000 * 720); // get every 12 hours

    return () => {
      if (fbInterval) {
        clearInterval(fbInterval);
      }
      clearInterval(bannerInterval);
      clearSnackBarTimer();
    };
  }, [isLoaded]);

  // fix night mode for IOS firefox
  useEffect(() => {
    if (isIOS && isMobile && isFirefox) {
      Array.from(document.querySelectorAll("style")).forEach((sheet) => {
        if (
          sheet?.textContent?.includes(
            "-webkit-filter: hue-rotate(180deg) invert(100%) !important;"
          )
        ) {
          sheet.parentNode?.removeChild(sheet);
        }
      });
    }
  }, []);

  useEffect(() => {
    console.log("Current page ", page);
  }, [page]);

  useEffect(() => {
    if (userTheme) setTheme(userTheme);
  }, [userTheme]);

  const pathname = window.location.pathname.toLowerCase();
  const isEditor = pathname.indexOf("doceditor") !== -1;

  const currentTheme = isBase ? "Base" : "Dark";
  const systemTheme = useThemeDetector();
  useEffect(() => {
    if (userTheme === "System" && currentTheme !== systemTheme)
      setTheme(systemTheme);
  }, [systemTheme]);

  const rootElement = document.getElementById("root");

  const toast = isMobileOnly ? (
    <Portal element={<Toast />} appendTo={rootElement} visible={true} />
  ) : (
    <Toast />
  );

  return (
    <Router>
      <Layout>
        {toast}
        <ReactSmartBanner t={t} ready={ready} />
        {isEditor || !isMobileOnly ? <></> : <NavMenu />}
        {isMobileOnly && <MainBar />}
        <IndicatorLoader />
        <ScrollToTop />
        <DialogsWrapper t={t} />
        <Main isDesktop={isDesktop}>
          {!isMobileOnly && <MainBar />}
          <div className="main-container">
            <Routes>
              <Route
                caseSensitive
                path="/Products/Files/"
                element={<Navigate to="/rooms/shared" replace />}
              />

              <Route
                path="/*"
                element={
                  <PrivateRoute>
                    <FilesRoute />
                  </PrivateRoute>
                }
              />

              <Route
                path={"/form-gallery/:folderId"}
                element={
                  <PrivateRoute>
                    <FormGalleryRoute />
                  </PrivateRoute>
                }
              />

              <Route
                path={"/about"}
                element={
                  <PrivateRoute>
                    <AboutRoute />
                  </PrivateRoute>
                }
              />

              <Route path={"/confirm/*"} element={<ConfirmRoute />} />

              <Route
                path={"/portal-settings/*"}
                element={
                  <PrivateRoute restricted>
                    <PortalSettingsRoute />
                  </PrivateRoute>
                }
              />

              <Route
                path={"/portal-unavailable"}
                element={
                  <PrivateRoute>
                    <PortalUnavailableRoute />
                  </PrivateRoute>
                }
              />

              <Route
                path={"/wizard"}
                element={
                  <PublicRoute>
                    <WizardRoute />
                  </PublicRoute>
                }
              />

              <Route
                path={"/preparation-portal"}
                element={
                  <PublicRoute>
                    <PreparationPortalRoute />
                  </PublicRoute>
                }
              />

              <Route
                path={"/error401"}
                element={
                  <PrivateRoute>
                    <Error401Route />
                  </PrivateRoute>
                }
              />

              <Route
                element={
                  <PrivateRoute>
                    <Error404Route />
                  </PrivateRoute>
                }
              />
            </Routes>
          </div>
        </Main>
      </Layout>
    </Router>
  );
};

const ShellWrapper = inject(({ auth, backup }) => {
  const { init, isLoaded, settingsStore, setProductVersion, language } = auth;

  const {
    personal,
    roomsMode,
    isDesktopClient,
    firebaseHelper,
    setModuleInfo,
    setCheckedMaintenance,
    setMaintenanceExist,
    setSnackbarExist,
    socketHelper,
    setTheme,
    whiteLabelLogoUrls,
  } = settingsStore;
  const isBase = settingsStore.theme.isBase;
  const { setPreparationPortalDialogVisible } = backup;

  return {
    loadBaseInfo: async () => {
      await init();

      setModuleInfo(config.homepage, "home");
      setProductVersion(config.version);

      if (isDesktopClient) {
        document.body.classList.add("desktop");
      }
    },
    language,
    isLoaded,

    isDesktop: isDesktopClient,
    FirebaseHelper: firebaseHelper,
    personal,
    setCheckedMaintenance,
    setMaintenanceExist,
    socketHelper,
    setPreparationPortalDialogVisible,
    isBase,
    setTheme,
    roomsMode,
    setSnackbarExist,
    userTheme: isDesktopClient
      ? window.RendererProcessVariable?.theme?.type === "dark"
        ? "Dark"
        : "Base"
      : auth?.userStore?.user?.theme,
    whiteLabelLogoUrls,
  };
})(observer(Shell));

const ThemeProviderWrapper = inject(({ auth }) => {
  const { settingsStore } = auth;

  return { theme: settingsStore.theme };
})(observer(ThemeProvider));

export default () => (
  <MobxProvider {...store}>
    <I18nextProvider i18n={i18n}>
      <ThemeProviderWrapper>
        <ShellWrapper />
      </ThemeProviderWrapper>
    </I18nextProvider>
  </MobxProvider>
);
