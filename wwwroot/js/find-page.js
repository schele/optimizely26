// Keeps the find page's address bar in step with the search, so results can be linked to and reloaded.
// Replaces the current history entry rather than adding one, so Back leaves the find page.
window.findPage = {
    replaceUrl: function (query, page) {
        const url = new URL(window.location.href);

        if (query) {
            url.searchParams.set('q', query);
        } else {
            url.searchParams.delete('q');
        }

        if (page > 1) {
            url.searchParams.set('page', page);
        } else {
            url.searchParams.delete('page');
        }

        history.replaceState(history.state, '', url);
    }
};
