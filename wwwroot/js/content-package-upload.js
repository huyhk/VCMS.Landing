(() => {
    const form = document.querySelector("#content-package-upload");
    if (!form) return;
    const panel = form.querySelector(".package-progress");
    const bar = panel.querySelector("progress");
    const percent = panel.querySelector("[data-package-percent]");
    const status = panel.querySelector("[data-package-status]");
    const button = form.querySelector('button[type="submit"]');

    form.addEventListener("submit", event => {
        event.preventDefault();
        panel.hidden = false;
        button.disabled = true;
        const request = new XMLHttpRequest();
        request.open(form.method, form.action);
        request.responseType = "document";
        request.upload.addEventListener("progress", uploadEvent => {
            if (!uploadEvent.lengthComputable) return;
            const value = Math.min(100, Math.round(uploadEvent.loaded * 100 / uploadEvent.total));
            bar.value = value;
            percent.textContent = `${value}%`;
        });
        request.upload.addEventListener("load", () => {
            bar.removeAttribute("value");
            percent.textContent = "100%";
            status.textContent = "Đang kiểm tra package…";
        });
        request.addEventListener("load", () => {
            if (request.status >= 200 && request.status < 400 && request.response) {
                document.open();
                document.write(request.response.documentElement.outerHTML);
                document.close();
                history.replaceState(null, "", request.responseURL);
                return;
            }
            status.textContent = `Không thể tải package (HTTP ${request.status}).`;
            button.disabled = false;
        });
        request.addEventListener("error", () => {
            status.textContent = "Mất kết nối trong lúc tải package.";
            button.disabled = false;
        });
        request.send(new FormData(form));
    });
})();
