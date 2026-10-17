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
    } else {
        belowFold.forEach((element) => element.classList.add("visible"));
    }
})();
