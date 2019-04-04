(function () {
    'use strict';

    var LANG_KEY = 'dejavu-lang';

    var translations = {
        'zh-CN': {
            home: '首页',
            posts: '文章',
            tags: '标签',
            about: '关于',
            categories: '分类',
            recentPosts: '最近文章',
            language: '语言',
            searchPlaceholder: '搜索...',
            apiDocs: 'API 文档',
            guideDocs: '用户文档',
            modules: '模块',
            tableOfContents: '目录',
            previous: '上一节',
            next: '下一节',
            searchApi: '搜索 API...'
        },
        en: {
            home: 'Home',
            posts: 'Posts',
            tags: 'Tags',
            about: 'About',
            categories: 'Categories',
            recentPosts: 'Recent Posts',
            language: 'Language',
            searchPlaceholder: 'Search...',
            apiDocs: 'API Docs',
            guideDocs: 'Guide',
            modules: 'Modules',
            tableOfContents: 'Contents',
            previous: 'Previous',
            next: 'Next',
            searchApi: 'Search API...'
        }
    };

    function getCurrentLang() {
        return localStorage.getItem(LANG_KEY) || document.documentElement.lang || 'zh-CN';
    }

    function applyTranslations(lang) {
        var t = translations[lang] || translations['zh-CN'];
        var elements = document.querySelectorAll('[data-i18n]');
        for (var i = 0; i < elements.length; i++) {
            var key = elements[i].getAttribute('data-i18n');
            if (t[key]) {
                elements[i].textContent = t[key];
            }
        }

        document.documentElement.lang = lang;

        var options = document.querySelectorAll('.lang-option');
        for (var i = 0; i < options.length; i++) {
            var optionLang = options[i].getAttribute('data-lang');
            if (optionLang === lang) {
                options[i].classList.add('active');
            } else {
                options[i].classList.remove('active');
            }
        }
    }

    function initLangSwitcher() {
        var toggle = document.getElementById('langToggle');
        var dropdown = document.getElementById('langDropdown');

        if (!toggle || !dropdown) return;

        toggle.addEventListener('click', function (e) {
            e.stopPropagation();
            dropdown.classList.toggle('open');
        });

        document.addEventListener('click', function () {
            dropdown.classList.remove('open');
        });

        dropdown.addEventListener('click', function (e) {
            e.stopPropagation();
        });

        var options = dropdown.querySelectorAll('.lang-option');
        for (var i = 0; i < options.length; i++) {
            options[i].addEventListener('click', function (e) {
                e.preventDefault();
                var lang = this.getAttribute('data-lang');
                localStorage.setItem(LANG_KEY, lang);
                applyTranslations(lang);
                dropdown.classList.remove('open');
            });
        }

        var savedLang = getCurrentLang();
        applyTranslations(savedLang);
    }

    function initSidebarToggle() {
        var toggle = document.getElementById('sidebarToggle');
        var sidebar = document.getElementById('sidebar');

        if (!toggle || !sidebar) return;

        toggle.addEventListener('click', function () {
            sidebar.classList.toggle('open');
        });

        document.addEventListener('click', function (e) {
            if (!sidebar.contains(e.target) && !toggle.contains(e.target)) {
                sidebar.classList.remove('open');
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            initLangSwitcher();
            initSidebarToggle();
        });
    } else {
        initLangSwitcher();
        initSidebarToggle();
    }
})();
