(function () {
  function waitForSwaggerUi() {
    if (!window.ui) {
      setTimeout(waitForSwaggerUi, 300);
      return;
    }

    const currentUrl = new URL(window.location.href);
    const swaggerToken = currentUrl.searchParams.get("swaggerToken");
    if (swaggerToken) {
      window.ui.preauthorizeApiKey("Bearer", "Bearer " + swaggerToken);
      currentUrl.searchParams.delete("swaggerToken");
      window.history.replaceState({}, document.title, currentUrl.toString());
    }

    const attachHandler = () => {
      const authorizeButton = document.querySelector(".swagger-ui .auth-wrapper .authorize");
      if (!authorizeButton || authorizeButton.dataset.customLoginAttached === "true") {
        return;
      }

      authorizeButton.dataset.customLoginAttached = "true";
      authorizeButton.addEventListener("click", function (event) {
        event.preventDefault();
        event.stopPropagation();
        const loginUrl =
          "https://pharma-frenzy-fe.vercel.app/account/login?returnUrl=" +
          encodeURIComponent(window.location.href.split("#")[0]);
        window.location.href = loginUrl;
      }, true);
    };

    attachHandler();
    const observer = new MutationObserver(attachHandler);
    observer.observe(document.body, { childList: true, subtree: true });
  }

  waitForSwaggerUi();
})();
