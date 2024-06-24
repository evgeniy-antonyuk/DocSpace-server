package com.asc.common.utilities;

import jakarta.servlet.http.HttpServletRequest;
import java.util.Optional;
import java.util.regex.Pattern;

/** Utility class for handling HTTP-related operations. */
public class HttpUtils {
  private static final String IP_PATTERN =
      "https?://([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})";
  private static final String DOMAIN_PATTERN = "https?://([a-zA-Z0-9.-]+\\.[a-zA-Z]{2,})";
  private static final String X_FORWARDED_HOST = "X-Forwarded-Host";
  private static final String X_FORWARDED_FOR = "X-Forwarded-For";
  private static final String HOST = "Host";
  private static final String[] IP_HEADERS = {
    "X-Forwarded-Host",
    "X-Forwarded-For",
    "Proxy-Client-IP",
    "WL-Proxy-Client-IP",
    "HTTP_X_FORWARDED_FOR",
    "HTTP_X_FORWARDED",
    "HTTP_X_CLUSTER_CLIENT_IP",
    "HTTP_CLIENT_IP",
    "HTTP_FORWARDED_FOR",
    "HTTP_FORWARDED",
    "HTTP_VIA",
    "REMOTE_ADDR"
  };

  private HttpUtils() {
    // Private constructor to prevent instantiation
  }

  /**
   * Retrieves the address from the specified header of the request.
   *
   * @param request HttpServletRequest object
   * @param header The header name to retrieve the address from
   * @return An Optional containing the address if found, otherwise an empty Optional
   */
  private static Optional<String> getRequestAddress(HttpServletRequest request, String header) {
    var address = request.getHeader(header);
    if (address == null || address.isBlank()) return Optional.empty();
    return Optional.of(String.format("%s://%s", request.getScheme(), address));
  }

  /**
   * Retrieves the host address from the 'X-Forwarded-Host' header of the request.
   *
   * @param request HttpServletRequest object
   * @return An Optional containing the host address if found, otherwise an empty Optional
   */
  public static Optional<String> getRequestHostAddress(HttpServletRequest request) {
    return getRequestAddress(request, X_FORWARDED_HOST);
  }

  /**
   * Retrieves the client address from the 'X-Forwarded-For' header of the request.
   *
   * @param request HttpServletRequest object
   * @return An Optional containing the client address if found, otherwise an empty Optional
   */
  public static Optional<String> getRequestClientAddress(HttpServletRequest request) {
    return getRequestAddress(request, X_FORWARDED_FOR);
  }

  /**
   * Retrieves the domain from the 'Host' header of the request.
   *
   * @param request HttpServletRequest object
   * @return An Optional containing the domain if found, otherwise an empty Optional
   */
  public static Optional<String> getRequestDomain(HttpServletRequest request) {
    var host = request.getHeader(HOST);
    if (host == null || host.isBlank()) {
      return Optional.empty();
    }

    return Optional.of(String.format("%s://%s", request.getScheme(), host));
  }

  /**
   * Retrieves the first IP address from the request headers.
   *
   * @param request HttpServletRequest object
   * @return The first IP address found in the request headers, or the remote address if none found
   */
  public static String getFirstRequestIP(HttpServletRequest request) {
    for (var header : IP_HEADERS) {
      var value = request.getHeader(header);
      if (value != null && !value.isEmpty()) return value.split("\\s*,\\s*")[0];
    }

    return request.getRemoteAddr();
  }

  /**
   * Determines the client's operating system from the User-Agent header.
   *
   * @param request HttpServletRequest object
   * @return Client's operating system
   */
  public static String getClientOS(HttpServletRequest request) {
    var userAgent = request.getHeader("User-Agent");
    var os = "Unknown";
    var osPattern = "";

    if (userAgent == null) return os;

    if (userAgent.toLowerCase().contains("windows")) osPattern = "Windows NT ([\\d.]+)";
    else if (userAgent.toLowerCase().contains("mac os x")) osPattern = "Mac OS X ([\\d_]+)";
    else if (userAgent.toLowerCase().contains("android")) osPattern = "Android ([\\d.]+)";
    else if (userAgent.toLowerCase().contains("iphone")) osPattern = "iPhone OS ([\\d_]+)";
    else if (userAgent.toLowerCase().contains("x11")) os = "Unix";

    if (!osPattern.isEmpty()) {
      var pattern = Pattern.compile(osPattern);
      var matcher = pattern.matcher(userAgent);
      if (matcher.find()) os = matcher.group().replace('_', '.');
    }

    return os;
  }

  /**
   * Determines the client's browser from the User-Agent header.
   *
   * @param request HttpServletRequest object
   * @return Client's browser
   */
  public static String getClientBrowser(HttpServletRequest request) {
    var browserDetails = request.getHeader("User-Agent");
    var user = browserDetails.toLowerCase();
    var browser = "";
    if (user.contains("msie")) {
      var substring = browserDetails.substring(browserDetails.indexOf("MSIE")).split(";")[0];
      browser = substring.split(" ")[0].replace("MSIE", "IE") + " " + substring.split(" ")[1];
    } else if (user.contains("safari") && user.contains("version")) {
      browser =
          (browserDetails.substring(browserDetails.indexOf("Safari")).split(" ")[0]).split("/")[0]
              + " "
              + (browserDetails.substring(browserDetails.indexOf("Version")).split(" ")[0])
                  .split("/")[1];
    } else if (user.contains("opr") || user.contains("opera")) {
      if (user.contains("opera"))
        browser =
            (browserDetails.substring(browserDetails.indexOf("Opera")).split(" ")[0]).split("/")[0]
                + "-"
                + (browserDetails.substring(browserDetails.indexOf("Version")).split(" ")[0])
                    .split("/")[1];
      else if (user.contains("opr"))
        browser =
            ((browserDetails.substring(browserDetails.indexOf("OPR")).split(" ")[0])
                    .replace("/", " "))
                .replace("OPR", "Opera");
    } else if (user.contains("chrome")) {
      browser =
          (browserDetails.substring(browserDetails.indexOf("Chrome")).split(" ")[0])
              .replace("/", "-")
              .replaceAll("\\.[0-9]+", "")
              .replaceAll("-", " ");
    } else if ((user.contains("mozilla/7.0"))
        || (user.contains("netscape6"))
        || (user.contains("mozilla/4.7"))
        || (user.contains("mozilla/4.78"))
        || (user.contains("mozilla/4.08"))
        || (user.contains("mozilla/3"))) {
      browser = "Netscape";
    } else if (user.contains("firefox")) {
      browser =
          (browserDetails.substring(browserDetails.indexOf("Firefox")).split(" ")[0])
              .replace("/", " ");
    } else if (user.contains("rv")) {
      browser = "IE";
    } else {
      browser = "Unknown";
    }

    return browser;
  }

  /**
   * Constructs the full URL of the current request.
   *
   * @param request HttpServletRequest object
   * @return Full URL of the request
   */
  public static String getFullURL(HttpServletRequest request) {
    var requestURL = request.getRequestURL();
    var queryString = request.getQueryString();
    return queryString == null
        ? requestURL.toString()
        : requestURL.append('?').append(queryString).toString();
  }

  /**
   * Extracts the host from the given URL.
   *
   * @param url The URL to extract the host from
   * @return The extracted host if found, otherwise the original URL
   */
  public static String extractHostFromUrl(String url) {
    return extractPattern(url, IP_PATTERN)
        .or(() -> extractPattern(url, DOMAIN_PATTERN))
        .orElse(url);
  }

  /**
   * Extracts a pattern from the given input string.
   *
   * @param input The input string
   * @param pattern The pattern to extract
   * @return An Optional containing the extracted pattern if found, otherwise an empty Optional
   */
  private static Optional<String> extractPattern(String input, String pattern) {
    var compiledPattern = Pattern.compile(pattern);
    var matcher = compiledPattern.matcher(input);
    if (matcher.find()) return Optional.of(matcher.group(1));
    return Optional.empty();
  }

  /**
   * Retrieves browser information from the User-Agent string.
   *
   * @param userAgent The User-Agent string
   * @param browser The browser name to look for
   * @param replacement The replacement string for the browser name
   * @return The formatted browser information
   */
  private static String getBrowserInfo(String userAgent, String browser, String replacement) {
    var substring = userAgent.substring(userAgent.indexOf(browser)).split(";")[0];
    return substring.split(" ")[0].replace(browser, replacement) + " " + substring.split(" ")[1];
  }

  /**
   * Retrieves the browser version from the User-Agent string.
   *
   * @param userAgent The User-Agent string
   * @param browser The browser name to look for
   * @return The browser version
   */
  private static String getBrowserVersion(String userAgent, String browser) {
    return userAgent.substring(userAgent.indexOf(browser)).split(" ")[0].split("/")[1];
  }
}
