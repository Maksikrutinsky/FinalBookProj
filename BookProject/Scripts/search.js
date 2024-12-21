$(document).ready(function() {
    console.log('Search script loaded'); // להוספת לוג לבדיקה

    var searchTimeout;
    var searchResults = $('#searchResults');

    $('#searchInput').on('input', function() {
        console.log('Input detected'); // להוספת לוג לבדיקה

        var query = $(this).val();

        clearTimeout(searchTimeout);

        if (query.length < 2) {
            searchResults.hide();
            return;
        }

        searchTimeout = setTimeout(function() {
            console.log('Searching for:', query); // להוספת לוג לבדיקה

            $.get('/Books/Search', { query: query })
                .done(function(data) {
                    console.log('Search results:', data); // להוספת לוג לבדיקה

                    searchResults.empty();

                    if (data.length > 0) {
                        data.forEach(function(book) {
                            var resultItem = $('<div class="search-result-item">')
                                .html(`
                                    <div class="d-flex align-items-center">
                                        <img src="${book.CoverImageUrl}" alt="${book.Title}" style="width: 50px; height: 70px; object-fit: cover; margin-right: 10px;">
                                        <div>
                                            <div class="font-weight-bold">${book.Title}</div>
                                            <div class="text-muted">${book.Author}</div>
                                            <div class="text-primary">₪${book.BuyPrice}</div>
                                        </div>
                                    </div>
                                `)
                                .click(function() {
                                    window.location.href = '/Books/Details/' + book.BookId;
                                });

                            searchResults.append(resultItem);
                        });

                        searchResults.show();
                    } else {
                        searchResults.html('<div class="search-result-item">No results found</div>').show();
                    }
                })
                .fail(function(error) {
                    console.error('Search failed:', error); // להוספת לוג לבדיקה
                });
        }, 300);
    });

    $(document).click(function(e) {
        if (!$(e.target).closest('.search-box').length) {
            searchResults.hide();
        }
    });
});