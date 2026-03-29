(function () {
    function initMenu() {
        var shell = document.querySelector("[data-menu-shell]");
        if (!shell) {
            return;
        }

        var openButtons = document.querySelectorAll("[data-menu-open]");
        var closeButtons = document.querySelectorAll("[data-menu-close]");

        function openMenu() {
            shell.classList.add("menu-open");
        }

        function closeMenu() {
            shell.classList.remove("menu-open");
        }

        openButtons.forEach(function (button) {
            button.addEventListener("click", openMenu);
        });

        closeButtons.forEach(function (button) {
            button.addEventListener("click", closeMenu);
        });
    }

    function initDeviceCatalog() {
        var catalogs = document.querySelectorAll("[data-device-catalog]");
        catalogs.forEach(function (catalog) {
            var items = Array.prototype.slice.call(catalog.querySelectorAll("[data-device-item]"));
            if (!items.length) {
                return;
            }

            var searchInput = catalog.querySelector("[data-device-search]");
            var statusFilter = catalog.querySelector("[data-device-filter='status']");
            var connectionFilter = catalog.querySelector("[data-device-filter='connection']");
            var onlineFilter = catalog.querySelector("[data-device-filter='online']");
            var pageSizeSelect = catalog.querySelector("[data-device-page-size]");
            var countLabel = catalog.querySelector("[data-device-count]");
            var pageLabel = catalog.querySelector("[data-device-page-label]");
            var prevButton = catalog.querySelector("[data-device-prev]");
            var nextButton = catalog.querySelector("[data-device-next]");
            var page = 1;

            function normalize(value) {
                return (value || "").toLowerCase().trim();
            }

            function getFilteredItems() {
                var query = normalize(searchInput && searchInput.value);
                var status = normalize(statusFilter && statusFilter.value) || "all";
                var connection = normalize(connectionFilter && connectionFilter.value) || "all";
                var online = normalize(onlineFilter && onlineFilter.value) || "all";

                return items.filter(function (item) {
                    var haystack = item.getAttribute("data-search") || "";
                    var itemStatus = normalize(item.getAttribute("data-status"));
                    var itemConnection = normalize(item.getAttribute("data-connection"));
                    var itemOnline = normalize(item.getAttribute("data-online"));

                    var matchesQuery = !query || haystack.indexOf(query) >= 0;
                    var matchesStatus = status === "all" || itemStatus === status;
                    var matchesConnection = connection === "all" || itemConnection === connection;
                    var matchesOnline = online === "all" || itemOnline === online;

                    return matchesQuery && matchesStatus && matchesConnection && matchesOnline;
                });
            }

            function render() {
                var filtered = getFilteredItems();
                var pageSize = parseInt(pageSizeSelect && pageSizeSelect.value || catalog.getAttribute("data-page-size") || "8", 10);
                var pageCount = Math.max(1, Math.ceil(filtered.length / pageSize));
                page = Math.min(page, pageCount);
                var start = (page - 1) * pageSize;
                var end = start + pageSize;

                items.forEach(function (item) {
                    item.classList.add("is-hidden");
                });

                filtered.slice(start, end).forEach(function (item) {
                    item.classList.remove("is-hidden");
                });

                if (countLabel) {
                    countLabel.textContent = filtered.length + " device(s)";
                }

                if (pageLabel) {
                    pageLabel.textContent = "Page " + page + " of " + pageCount;
                }

                if (prevButton) {
                    prevButton.disabled = page <= 1;
                }

                if (nextButton) {
                    nextButton.disabled = page >= pageCount;
                }
            }

            [searchInput, statusFilter, connectionFilter, onlineFilter, pageSizeSelect].forEach(function (element) {
                if (!element) {
                    return;
                }

                element.addEventListener("input", function () {
                    page = 1;
                    render();
                });

                element.addEventListener("change", function () {
                    page = 1;
                    render();
                });
            });

            if (prevButton) {
                prevButton.addEventListener("click", function () {
                    page = Math.max(1, page - 1);
                    render();
                });
            }

            if (nextButton) {
                nextButton.addEventListener("click", function () {
                    page += 1;
                    render();
                });
            }

            render();
        });
    }

    function initTabs() {
        var groups = document.querySelectorAll("[data-tab-group]");
        groups.forEach(function (group) {
            var buttons = Array.prototype.slice.call(group.querySelectorAll("[data-tab-button]"));
            var panels = Array.prototype.slice.call(group.querySelectorAll("[data-tab-panel]"));
            if (!buttons.length || !panels.length) {
                return;
            }

            function activate(name) {
                buttons.forEach(function (button) {
                    var isActive = button.getAttribute("data-tab-button") === name;
                    button.classList.toggle("active", isActive);
                    button.setAttribute("aria-selected", isActive ? "true" : "false");
                });

                panels.forEach(function (panel) {
                    var isActive = panel.getAttribute("data-tab-panel") === name;
                    panel.classList.toggle("is-hidden", !isActive);
                });
            }

            buttons.forEach(function (button) {
                button.addEventListener("click", function () {
                    activate(button.getAttribute("data-tab-button"));
                });
            });

            activate(buttons[0].getAttribute("data-tab-button"));
        });
    }

    initMenu();
    initDeviceCatalog();
    initTabs();
})();
