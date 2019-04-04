(function () {
    'use strict';

    function initPrism() {
        if (typeof Prism === 'undefined') return;

        if (Prism.plugins && Prism.plugins.autoloader) {
            Prism.plugins.autoloader.languages_path =
                'https://cdn.jsdelivr.net/npm/prismjs@1.29.0/components/';
        }

        Prism.highlightAll();
    }

    function initKatex() {
        if (typeof renderMathInElement === 'undefined') return;

        renderMathInElement(document.body, {
            delimiters: [
                { left: '$$', right: '$$', display: true },
                { left: '$', right: '$', display: false },
                { left: '\\(', right: '\\)', display: false },
                { left: '\\[', right: '\\]', display: true }
            ],
            throwOnError: false,
            output: 'html'
        });
    }

    function initMermaid() {
        if (typeof mermaid === 'undefined') return;

        mermaid.initialize({
            startOnLoad: false,
            theme: 'default',
            securityLevel: 'loose',
            fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Noto Sans SC", sans-serif'
        });

        var codeBlocks = document.querySelectorAll('pre code.language-mermaid, pre code.lang-mermaid, div.language-mermaid, div.lang-mermaid');
        if (codeBlocks.length === 0) return;

        var ids = [];
        for (var i = 0; i < codeBlocks.length; i++) {
            var block = codeBlocks[i];
            var id = 'mermaid-' + i;
            block.id = id;
            ids.push(id);

            var parent = block.parentElement;
            if (parent && parent.tagName === 'PRE') {
                var container = document.createElement('div');
                container.className = 'mermaid-container';
                container.id = id;
                container.textContent = block.textContent;
                parent.replaceWith(container);
            }
        }

        mermaid.run({ querySelector: '.mermaid-container' });
    }

    function initAll() {
        initMermaid();
        initKatex();
        initPrism();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }
})();
