// Yönetim paneli etkileşimleri. Her özellik ilgili data-* özniteliği varsa devreye girer;
// JavaScript kapalıyken formlar yine çalışır (aşamalı geliştirme).
(() => {
    // --- Silme onayı: data-confirm taşıyan formlar önce bir diyalog gösterir ---
    const dialog = document.querySelector("[data-confirm-dialog]");
    if (dialog && typeof dialog.showModal === "function") {
        const message = dialog.querySelector("[data-confirm-message]");
        let pendingForm = null;

        document.addEventListener("submit", (event) => {
            const form = event.target;
            if (!(form instanceof HTMLFormElement) || !form.dataset.confirm || form.dataset.confirmed) return;

            event.preventDefault();
            pendingForm = form;
            message.textContent = form.dataset.confirm;
            dialog.showModal();
        }, true);

        dialog.addEventListener("close", () => {
            if (dialog.returnValue === "ok" && pendingForm) {
                pendingForm.dataset.confirmed = "true";
                pendingForm.requestSubmit();
            }
            pendingForm = null;
        });

        // Diyaloğun dışına tıklayınca kapat.
        dialog.addEventListener("click", (event) => {
            if (event.target === dialog) dialog.close("cancel");
        });
    }

    // --- Bildirimler: kapatma düğmesi ve 5 sn sonra otomatik gizleme ---
    document.querySelectorAll("[data-flash]").forEach((flash) => {
        const hide = () => flash.classList.add("hiding");
        flash.querySelector("[data-flash-close]")?.addEventListener("click", hide);
        flash.addEventListener("transitionend", () => flash.remove());
        if (flash.classList.contains("flash-success")) setTimeout(hide, 5000);
    });

    // --- Slug önizlemesi: sunucudaki Slug.Generate ile aynı kurallar ---
    const slugify = (text) => text
        .replace(/[ıİ]/g, "i").replace(/[ğĞ]/g, "g").replace(/[şŞ]/g, "s")
        .normalize("NFD").replace(/[̀-ͯ]/g, "")
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, "-")
        .replace(/^-+|-+$/g, "")
        .slice(0, 120);

    const slugSource = document.querySelector("[data-slug-source]");
    const slugTarget = document.querySelector("[data-slug-target]");
    if (slugSource && slugTarget) {
        const update = () => { slugTarget.placeholder = slugify(slugSource.value) || "otomatik oluşturulur"; };
        slugSource.addEventListener("input", update);
        slugTarget.addEventListener("blur", () => { slugTarget.value = slugify(slugTarget.value); });
        update();
    }

    // --- Karakter sayacı ---
    document.querySelectorAll("[data-counter][maxlength]").forEach((field) => {
        const counter = document.createElement("span");
        counter.className = "counter";
        field.insertAdjacentElement("afterend", counter);
        const max = Number(field.getAttribute("maxlength"));
        const update = () => {
            counter.textContent = `${field.value.length} / ${max}`;
            counter.classList.toggle("near-limit", field.value.length > max * 0.9);
        };
        field.addEventListener("input", update);
        update();
    });

    // --- Kapak görseli: önizleme ve sürükle-bırak ---
    document.querySelectorAll("[data-dropzone]").forEach((zone) => {
        const input = zone.querySelector("[data-file-input]");
        const preview = zone.querySelector("[data-preview]");
        const empty = zone.querySelector("[data-preview-empty]");
        const remove = document.querySelector("[data-remove-cover]");
        let objectUrl = null;

        const show = (file) => {
            if (!file || !file.type.startsWith("image/")) return;
            if (objectUrl) URL.revokeObjectURL(objectUrl);
            objectUrl = URL.createObjectURL(file);
            preview.src = objectUrl;
            preview.hidden = false;
            empty.hidden = true;
            if (remove) remove.checked = false;
        };

        input.addEventListener("change", () => show(input.files[0]));
        ["dragenter", "dragover"].forEach((type) => zone.addEventListener(type, (event) => {
            event.preventDefault();
            zone.classList.add("dragging");
        }));
        ["dragleave", "drop"].forEach((type) => zone.addEventListener(type, () => zone.classList.remove("dragging")));
        zone.addEventListener("drop", (event) => {
            event.preventDefault();
            if (event.dataTransfer.files.length) {
                input.files = event.dataTransfer.files;
                show(input.files[0]);
            }
        });

        remove?.addEventListener("change", () => zone.classList.toggle("removing", remove.checked));
    });

    // --- "Hâlâ devam ediyor" kutusu bitiş tarihini temizler ve kilitler ---
    const ongoing = document.querySelector("[data-ongoing]");
    const endDate = document.querySelector("[data-end-date]");
    if (ongoing && endDate) {
        const sync = () => {
            if (ongoing.checked) endDate.value = "";
            endDate.disabled = ongoing.checked;
        };
        ongoing.addEventListener("change", sync);
        sync();
    }

    // --- Markdown alanı: "Yaz | Önizle" sekmeleri ---
    // Önizleme, sitede kullanılan aynı işleyiciden (sanitize edilmiş) HTML olarak gelir.
    document.querySelectorAll("[data-markdown-field]").forEach((field) => {
        const input = field.querySelector("[data-md-input]");
        const preview = field.querySelector("[data-md-preview]");
        const tabs = field.querySelectorAll("[data-md-tab]");
        const token = field.closest("form")?.querySelector("input[name=__RequestVerificationToken]")?.value;
        let lastRendered = null;

        const show = async (mode) => {
            tabs.forEach((tab) => {
                const active = tab.dataset.mdTab === mode;
                tab.classList.toggle("active", active);
                tab.setAttribute("aria-selected", String(active));
            });
            input.hidden = mode === "preview";
            preview.hidden = mode !== "preview";
            if (mode !== "preview" || lastRendered === input.value) return;

            preview.classList.add("loading");
            try {
                const body = new URLSearchParams({ markdown: input.value, __RequestVerificationToken: token ?? "" });
                const url = field.querySelector('[data-md-tab="preview"]').dataset.previewUrl;
                const response = await fetch(url, { method: "POST", body });
                preview.innerHTML = response.ok ? await response.text() : '<p class="field-error">Önizleme alınamadı.</p>';
                lastRendered = input.value;
            } catch {
                preview.innerHTML = '<p class="field-error">Bağlantı hatası; önizleme alınamadı.</p>';
            } finally {
                preview.classList.remove("loading");
            }
        };

        tabs.forEach((tab) => tab.addEventListener("click", () => show(tab.dataset.mdTab)));
    });

    // --- Seçim değişince filtre formunu gönder ---
    document.querySelectorAll("[data-autosubmit]").forEach((select) =>
        select.addEventListener("change", () => select.form?.requestSubmit()));
})();
