// Sitenin istemci tarafı davranışları: tema, mobil menü, kaydırma efektleri.
(() => {
    const root = document.documentElement;

    // --- Tema (açık / karanlık) ---
    document.querySelectorAll("[data-theme-toggle]").forEach((button) => {
        button.addEventListener("click", () => {
            const next = root.dataset.theme === "dark" ? "light" : "dark";
            root.dataset.theme = next;
            try { localStorage.setItem("theme", next); } catch { /* gizli sekme vb. */ }
        });
    });

    // --- Mobil menü ---
    const nav = document.querySelector("[data-nav]");
    const navToggle = document.querySelector("[data-nav-toggle]");
    if (nav && navToggle) {
        const setOpen = (open) => {
            nav.classList.toggle("open", open);
            navToggle.setAttribute("aria-expanded", String(open));
            navToggle.setAttribute("aria-label", open ? "Menüyü kapat" : "Menüyü aç");
        };
        navToggle.addEventListener("click", () => setOpen(!nav.classList.contains("open")));
        nav.querySelectorAll("a").forEach((link) => link.addEventListener("click", () => setOpen(false)));
        document.addEventListener("keydown", (event) => { if (event.key === "Escape") setOpen(false); });
    }

    // --- Kaydırınca üst bilginin arka planı ---
    const header = document.querySelector("[data-header]");
    if (header) {
        const onScroll = () => header.classList.toggle("scrolled", window.scrollY > 8);
        onScroll();
        window.addEventListener("scroll", onScroll, { passive: true });
    }

    // --- Görünür olunca beliren öğeler ---
    // İlk ekrandaki öğeler gözlemciyi beklemeden hemen açılır; içerik hiçbir koşulda
    // JavaScript'e bağlı olarak gizli kalmasın.
    const revealables = [...document.querySelectorAll(".reveal")];
    const belowFold = revealables.filter((element, index) => {
        if (element.getBoundingClientRect().top < window.innerHeight) {
            element.style.transitionDelay = `${Math.min(index, 6) * 60}ms`;
            element.classList.add("visible");
            return false;
        }
        return true;
    });

    if ("IntersectionObserver" in window) {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (entry.isIntersecting) {
                    entry.target.classList.add("visible");
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.12 });

        belowFold.forEach((element) => observer.observe(element));

        // Yedek: çapalı bağlantı (/#deneyim) ya da yumuşak kaydırma sonrası gözlemci gecikirse,
        // kaydırma durduğunda görünür alandakileri doğrudan aç. Hepsi açılınca dinleyici kaldırılır.
        let pending = belowFold;
        let timer = 0;
        const revealInView = () => {
            pending = pending.filter((element) => {
                const rect = element.getBoundingClientRect();
                const inView = rect.top < window.innerHeight && rect.bottom > 0;
                if (inView) element.classList.add("visible");
                return !inView && !element.classList.contains("visible");
            });
            if (!pending.length) window.removeEventListener("scroll", onScrollEnd);
        };
        const onScrollEnd = () => {
            clearTimeout(timer);
            timer = setTimeout(revealInView, 150);
        };
        window.addEventListener("scroll", onScrollEnd, { passive: true });
        window.addEventListener("load", onScrollEnd);
    } else {
        belowFold.forEach((element) => element.classList.add("visible"));
    }

    // --- İletişim formu: çift tıklamayla iki kez gönderilmesin ---
    document.querySelectorAll("[data-contact-form]").forEach((form) => {
        form.addEventListener("submit", () => {
            const button = form.querySelector("[data-submit]");
            if (!button) return;
            button.disabled = true;
            button.querySelector("span").textContent = "Gönderiliyor…";
        });
    });

    // --- İçindekiler: okunmakta olan başlığı vurgula ---
    const toc = document.querySelector("[data-toc]");
    if (toc && "IntersectionObserver" in window) {
        const links = new Map([...toc.querySelectorAll("a")].map((a) => [decodeURIComponent(a.hash.slice(1)), a]));
        const headings = [...links.keys()].map((id) => document.getElementById(id)).filter(Boolean);
        const setActive = (id) => links.forEach((a, key) => a.classList.toggle("active", key === id));

        const spy = new IntersectionObserver((entries) => {
            const visible = entries.filter((e) => e.isIntersecting).sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top);
            if (visible.length) setActive(visible[0].target.id);
        }, { rootMargin: "-80px 0px -65% 0px" });

        headings.forEach((h) => spy.observe(h));
    }
})();
